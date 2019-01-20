using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using MMALSharp;
using MMALSharp.Components;
using MMALSharp.Handlers;
using MMALSharp.Native;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class PiCamera : ICamera, IDisposable
    {      
        const string TimestampFormat = "yyyy-MM-dd-HH-mm-ss";

        private readonly CameraConfiguration configuration;
        private readonly IMediator mediator;
        MMALCamera camera;

        SemaphoreSlim cameraInUse = new SemaphoreSlim(1);

        string currentTimelapseId = null;
        private string currentTimelapseOutputDirectory;
        CancellationTokenSource timelapseCts;

        public PiCamera(CameraConfiguration configuration, IMediator mediator)
        {
            this.configuration = configuration;
            this.mediator = mediator;
            this.configuration.Subscribe(this.ApplyConfiguration);
        }

        void ApplyConfiguration()
        {
            MMALCameraConfig.Rotation = this.configuration.CameraRotation;
            Logger.Log($"Camera rotation: {this.configuration.CameraRotation}");

            if (this.configuration.CameraPhotoResolutionWidth.HasValue &&
                this.configuration.CameraPhotoResolutionHeight.HasValue &&
                this.configuration.CameraPhotoResolutionHeight.Value > 0 &&
                this.configuration.CameraPhotoResolutionWidth.Value > 0)
            {
                MMALCameraConfig.StillResolution = new Resolution(this.configuration.CameraPhotoResolutionWidth.Value, this.configuration.CameraPhotoResolutionHeight.Value);
                Logger.Log($"Camera photo resolution: {this.configuration.CameraPhotoResolutionWidth.Value}x{this.configuration.CameraPhotoResolutionHeight.Value}");
            }
        }

        public bool Initialize()
        {
            try
            {
                camera = MMALCamera.Instance;

                ApplyConfiguration();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to obtain camera. Ensure module is running with createOptions: \"HostConfig\": { \"Privileged\": true }");
                return false;
            }

            if (camera == null)
            {
                Logger.Log("Failed to obtain camera. Ensure module is running with createOptions: \"HostConfig\": { \"Privileged\": true }");
                return false;
            }

            return true;
        }

        bool IsTakingTimelapse() => currentTimelapseId != null;


        public async Task<TakeTimelapseResponse> StartTimelapseAsync(TakeTimelapseRequest req)
        {
            if (IsTakingTimelapse())
            {
                return new TakeTimelapseResponse()
                {
                    ErrorMessage = $"Timelapse {currentTimelapseId?.ToString()} is being taken",
                    IsTakingTimelapse = true,
                };
            }

            var cameraUsed = false;

            try
            {
                currentTimelapseId = DateTime.UtcNow.ToString(TimestampFormat);
                currentTimelapseOutputDirectory = req.OutputDirectory; 
                var pathForImages = Path.Combine(req.OutputDirectory, currentTimelapseId);
                
                await cameraInUse.WaitAsync();
                cameraUsed = true;

                // This example will take an image every 10 seconds for 4 hours
                var imgCaptureHandler = new ImageStreamCaptureHandler(pathForImages, "jpg");
                timelapseCts = new CancellationTokenSource(TimeSpan.FromSeconds(req.Duration));
                var tl = new Timelapse { Mode = TimelapseMode.Second, CancellationToken = timelapseCts.Token, Value = req.Interval };

                Logger.Log($"Starting timelapse {currentTimelapseId}");
                _ = camera.TakePictureTimelapse(imgCaptureHandler, MMALEncoding.JPEG, MMALEncoding.I420, tl)
                    .ContinueWith(async (t) => await PrepareTimelapseVideoAsync(t, imgCaptureHandler, pathForImages));

                return new TakeTimelapseResponse()
                {
                    Id = currentTimelapseId,
                    Duration = req.Duration,
                    Interval = req.Interval,
                };
            }
            catch
            {
                if (cameraUsed)
                    cameraInUse.Release();

                throw;
            }
        }

        async Task PrepareTimelapseVideoAsync(Task captureImageTask, ImageStreamCaptureHandler imgCaptureHandler, string path)
        {
            try
            {
                //Logger.Log($"Prepare timelapse: {captureImageTask.IsCompletedSuccessfully}, {captureImageTask.IsFaulted}");

                if (captureImageTask.IsCompletedSuccessfully)
                {
                    Logger.Log($"Will create timelapse {currentTimelapseId} video from {path}");

                    if (imgCaptureHandler.ProcessedFiles.Count == 0)
                        return;

                    var process = new Process
                    {
                        StartInfo =
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            FileName = "ffmpeg"
                        }
                    };

                    var extension = imgCaptureHandler.ProcessedFiles.First().Extension;

                    var targetDirectory = configuration.EnsureOutputDirectoryExists(this.currentTimelapseOutputDirectory);
                    var targetFilePath = Path.Combine(targetDirectory, string.Concat(currentTimelapseId, ".mp4"));
                    var targetDirectoryInfo = Directory.CreateDirectory(targetDirectory);

                    var fps = 2;
                    //var args = $"-framerate {fps} -f image2 -pattern_type glob -y -i {path + "/*." + extension} {targetFilePath}";
                    var args = $"-framerate {fps} -f image2 -pattern_type glob -y -i {path + "/*." + extension} -c:v libx264 -pix_fmt yuv420p {targetFilePath}";
                    Logger.Log($"Starting ffmpeg with args: {args}");
                    process.StartInfo.Arguments = args;
                    process.Start();
                    process.WaitForExit();

                    Logger.Log($"Timelapse video for {currentTimelapseId} created");
                    if (configuration.HasStorageInformation())
                    {
                        await UploadFileAsync("timelapse", targetFilePath);
                    }

                    // clean up temporary folder
                    TryRemoveFolder(imgCaptureHandler.ProcessedFiles.First().Directory);

                    // notify
                    await this.mediator.Publish(new TimelapseTakenNotification()
                    {
                        Timelapse = currentTimelapseId,
                    });

                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create timelapse video");
            }
            finally
            {
                currentTimelapseId = null;
                imgCaptureHandler?.Dispose();
                timelapseCts?.Dispose();
                timelapseCts = null;

                cameraInUse.Release();                
            }
        }

        private bool TryRemoveFolder(string directory)
        {
            try
            {
                foreach (var f in Directory.GetFiles(directory))
                {
                    File.Delete(f);
                }

                Directory.Delete(directory);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error clearing folder {directory}");
            }

            return false;
        }

        public StopTimelapseResponse StopTimelapse(StopTimelapseRequest req)
        {
            if (!IsTakingTimelapse())
            {
                return new StopTimelapseResponse()
                {
                    ErrorMessage = "No timelapse being taken"
                };
            }

            var timelapseId = this.currentTimelapseId;
            this.timelapseCts.Cancel();
            Logger.Log($"Timelapse {currentTimelapseId ?? string.Empty} stopped");

            return new StopTimelapseResponse() 
            {
                Id = timelapseId,
            };
        }

        public async Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest takePhotoRequest)
        {
            var cameraWasUsed = false;


            if (IsTakingTimelapse())
            {
                return new TakePhotoResponse()
                {
                    ErrorMessage = $"Timelapse {currentTimelapseId?.ToString()} is being taken",
                    IsTakingTimelapse = true,
                };
            }

            try
            {
                await cameraInUse.WaitAsync();
                cameraWasUsed = true;

                using (var imgCaptureHandler = new ImageStreamCaptureHandler(takePhotoRequest.OutputDirectory, takePhotoRequest.ImageType))        
                {            
                    var stopwatch = Stopwatch.StartNew();

                    if (takePhotoRequest.QuickMode)
                        await camera.TakeRawPicture(imgCaptureHandler);
                    else
                        await camera.TakePicture(imgCaptureHandler, takePhotoRequest.GetImageEncoding(), takePhotoRequest.GetPixelFormatEncoding());                    
                    stopwatch.Stop();
                    
                    var localFilePath = imgCaptureHandler.GetFilepath();

                    // rename it according to our rules
                    localFilePath = FixFilename(localFilePath);

                    if (configuration.HasStorageInformation())
                    {
                        await UploadFileAsync("photos", localFilePath);
                    }

                    var fi = new FileInfo(localFilePath);
                    Logger.Log($"New photo: {fi.Length} bytes, in {stopwatch.ElapsedMilliseconds}ms");

                    if (takePhotoRequest.DeleteLocalFile)
                    {
                        File.Delete(localFilePath);
                    }

                    return new TakePhotoResponse()
                    {
                        Filename = Path.GetFileName(localFilePath),
                        DeleteLocalFile = takePhotoRequest.DeleteLocalFile,
                        PixelFormat = takePhotoRequest.PixelFormat,
                        ImageType = takePhotoRequest.ImageType,
                        QuickMode = takePhotoRequest.QuickMode,
                    };
                }           
            }
            finally
            {
                if (cameraWasUsed)
                    cameraInUse.Release();

            }
        }

        private string FixFilename(string localFilePath)
        {
            var directory = Path.GetDirectoryName(localFilePath);
            var extension = Path.GetExtension(localFilePath);
            var now = DateTime.UtcNow.ToString(TimestampFormat);
            for (var i=0; i < 5; i++)
            {
                try
                {
                    var newFileName = (i == 0) ? now : string.Concat(now, "_" + i.ToString());
                    var newFilePath = Path.Combine(directory, string.Concat(newFileName, extension));
                    File.Move(localFilePath, newFilePath);
                    return newFilePath;
                }
                catch (IOException)
                {

                }
            }

            // if we couldn't fix the name, return the original
            return localFilePath;
        }

        async Task<string> UploadFileAsync(string folder, string localFilePath)
        {
            if (CloudStorageAccount.TryParse($"DefaultEndpointsProtocol=https;AccountName={configuration.StorageAccount};AccountKey={configuration.StorageKey};EndpointSuffix=core.windows.net", out var cloudStorageAccount))
            {
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();

                var containerName = configuration.ModuleId;
                if (string.IsNullOrEmpty(containerName))
                    containerName = "camera";
                Logger.Log($"Using container {containerName}");
                var containerReference = blobClient.GetContainerReference(containerName);
                
                if (await containerReference.CreateIfNotExistsAsync())
                {
                    Logger.Log($"Container {containerName} created");
                }

                var filename = Path.GetFileName(localFilePath);

                var blobName = $"{configuration.DeviceId}/{folder}/{filename}";
                var appendBlob = containerReference.GetAppendBlobReference(blobName);

                await appendBlob.UploadFromFileAsync(localFilePath);
                Logger.Log($"File {localFilePath} copied to blob {blobName}");

                return appendBlob.Uri.ToString();
            }
            else
            {
                Logger.Log("Failed to create cloud storage from connection string");
                return string.Empty;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.camera?.Cleanup();
                    this.camera = null;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            
            GC.SuppressFinalize(this);
        }

        public CameraStatus GetCameraStatus()
        {
            return new CameraStatus()
            {
                IsTakingTimelapse = !string.IsNullOrEmpty(this.currentTimelapseId),
            };
        }

        #endregion
    }
}
