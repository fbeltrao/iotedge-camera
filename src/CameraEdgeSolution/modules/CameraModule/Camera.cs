using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.WindowsAzure.Storage;
using MMALSharp;
using MMALSharp.Components;
using MMALSharp.Handlers;
using MMALSharp.Native;
using Newtonsoft.Json;

namespace CameraModule
{
    public class Camera : IDisposable
    {
        const string directory = "./cameraoutput";

        private readonly CameraConfiguration configuration;

        MMALCamera camera;

        SemaphoreSlim cameraInUse = new SemaphoreSlim(1);

        string currentTimelapseId = null;
        CancellationTokenSource timelapseCts;


        public Camera(CameraConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool Initialize()
        {
            try
            {
                camera = MMALCamera.Instance;
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
                };
            }

            var cameraUsed = false;

            try
            {
                var path = EnsureLocalDirectoryExists();
                currentTimelapseId = Guid.NewGuid().ToString();
                var pathForImages = Path.Combine(path, currentTimelapseId);
                
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
                Logger.Log($"Prepare timelapse: {captureImageTask.IsCompletedSuccessfully}, {captureImageTask.IsFaulted}");

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

                    var targetDirectory = Path.Combine(path, "out");
                    var targetFilePath = Path.Combine(targetDirectory, string.Concat(currentTimelapseId, ".avi"));
                    var targetDirectoryInfo = Directory.CreateDirectory(targetDirectory);

                    var fps = 2;
                    var args = $"-framerate {fps} -f image2 -pattern_type glob -y -i {path + "/*." + extension} {targetFilePath}";
                    Logger.Log($"Starting ffmpeg with args: {args}");
                    process.StartInfo.Arguments = args;
                    process.Start();
                    process.WaitForExit();

                    Logger.Log($"Timelapse video for {currentTimelapseId} created");

                    await UploadFileAsync("timelapse", targetFilePath);
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

        public StopTimelapseResponse StopTimelapse(StopTimelapseRequest req)
        {
            if (!IsTakingTimelapse())
            {
                return new StopTimelapseResponse()
                {
                    ErrorMessage = "No timelapse being taken"
                };
            }

            this.timelapseCts.Cancel();
            Logger.Log($"Timelapse {currentTimelapseId ?? string.Empty} stopped");
            return new StopTimelapseResponse();
        }

        string EnsureLocalDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    return Directory.CreateDirectory(directory).FullName;
                }
                else
                {
                    return Path.GetFullPath(directory);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create '/cameraOutput' folder", ex);
            }
        }

        public async Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest takePhotoRequest)
        {
            var cameraWasUsed = false;


            if (IsTakingTimelapse())
            {
                return new TakePhotoResponse()
                {
                    ErrorMessage = $"Timelapse {currentTimelapseId?.ToString()} is being taken",
                };
            }

            try
            {
                var path = EnsureLocalDirectoryExists();

                await cameraInUse.WaitAsync();
                cameraWasUsed = true;

                using (var imgCaptureHandler = new ImageStreamCaptureHandler(path, takePhotoRequest.ImageType))        
                {            
                    var stopwatch = Stopwatch.StartNew();

                    if (takePhotoRequest.QuickMode)
                        await camera.TakeRawPicture(imgCaptureHandler);
                    else
                        await camera.TakePicture(imgCaptureHandler, takePhotoRequest.GetImageEncoding(), takePhotoRequest.GetPixelFormatEncoding());                    
                    stopwatch.Stop();
                    
                    var localFilePath = imgCaptureHandler.GetFilepath();
                    var blobName = await UploadFileAsync("photos", localFilePath);

                    var fi = new FileInfo(localFilePath);
                    Logger.Log($"New photo: {fi.Length} bytes, in {stopwatch.ElapsedMilliseconds}ms");

                    if (takePhotoRequest.DeleteLocalFile)
                    {
                        File.Delete(localFilePath);
                    }

                    return new TakePhotoResponse()
                    {
                        BlobName = blobName,
                        LocalFilePath = localFilePath,
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



        async Task<string> UploadFileAsync(string folder, string localFilePath)
        {
            if (CloudStorageAccount.TryParse($"DefaultEndpointsProtocol=https;AccountName={configuration.StorageAccount};AccountKey={configuration.StorageKey};EndpointSuffix=core.windows.net", out var cloudStorageAccount))
            {
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();
                var containerReference = blobClient.GetContainerReference(configuration.ModuleId);
                
                if (await containerReference.CreateIfNotExistsAsync())
                {
                    Logger.Log($"Container {configuration.ModuleId} created");
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
        #endregion
    }
}
