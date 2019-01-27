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
using MMALSharp.Common.Utility;
using MMALSharp.Components;
using MMALSharp.Config;
using MMALSharp.Handlers;
using MMALSharp.Native;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class PiCamera : ICamera, IDisposable
    {      
        private readonly CameraConfiguration configuration;
        private readonly IMediator mediator;
        MMALCamera camera;
        Resolution? stillResolution;
        Resolution? timelapseResolution;
        SemaphoreSlim cameraInUse = new SemaphoreSlim(1);

        PiCameraTimelapse currentTimelapse;

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
                this.stillResolution = new Resolution(this.configuration.CameraPhotoResolutionWidth.Value, this.configuration.CameraPhotoResolutionHeight.Value);
                Logger.Log($"Camera photo resolution: {this.configuration.CameraPhotoResolutionWidth.Value}x{this.configuration.CameraPhotoResolutionHeight.Value}");
            }

            if (this.configuration.CameraTimelapseResolutionWidth.HasValue &&
                this.configuration.CameraTimelapseResolutionHeight.HasValue &&
                this.configuration.CameraTimelapseResolutionHeight.Value > 0 &&
                this.configuration.CameraTimelapseResolutionWidth.Value > 0)
            {
                this.timelapseResolution = new Resolution(this.configuration.CameraTimelapseResolutionWidth.Value, this.configuration.CameraTimelapseResolutionHeight.Value);
                Logger.Log($"Camera timelapse resolution: {this.configuration.CameraTimelapseResolutionWidth.Value}x{this.configuration.CameraTimelapseResolutionHeight.Value}");
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

        bool IsTakingTimelapse() => this.currentTimelapse != null;

        public Task<CameraTimelapseBase> CreateTimelapseAsync(TakeTimelapseRequest req)
        {
            if (IsTakingTimelapse())
            {
               throw new TimelapseInProgressException();
            }

            var duration = TimeSpan.FromMinutes(10);
            var interval = TimeSpan.FromSeconds(10);
            this.currentTimelapse = new PiCameraTimelapse(interval, duration, this.configuration, this);
            return Task.FromResult<CameraTimelapseBase>(this.currentTimelapse);
        }      

        

        public Task<StopTimelapseResponse> StopTimelapseAsync(StopTimelapseRequest req)
        {
            if (!IsTakingTimelapse())
            {
                return Task.FromResult(new StopTimelapseResponse()
                {
                    ErrorMessage = "No timelapse being taken"
                });
            }

            var timelapseId = this.currentTimelapse?.ID;
            this.currentTimelapse.Stop();
            this.currentTimelapse = null;
            
            Logger.Log($"Timelapse {timelapseId ?? string.Empty} stopped");

            return Task.FromResult(new StopTimelapseResponse() 
            {
                Id = timelapseId,
            });
        }


        internal async Task<TakePhotoResponse> TakeTimelapsePhotoAsync(string timelapse)
        {
            return await this.InternalTakePhotoAsync(Path.Combine(Constants.TimelapsesSubFolderName, timelapse), new TakePhotoRequest(), this.timelapseResolution);
        }

        async Task<TakePhotoResponse> InternalTakePhotoAsync(string subfolder, TakePhotoRequest takePhotoRequest, Resolution? resolution = null)
        {
            var cameraWasUsed = false;

            try
            {
                await cameraInUse.WaitAsync();
                cameraWasUsed = true;

                if (resolution.HasValue)
                    MMALCameraConfig.StillResolution = resolution.Value;

                var path = configuration.EnsureOutputDirectoryExists(subfolder);
                using (var imgCaptureHandler = new ImageStreamCaptureHandler(path, takePhotoRequest.ImageType))        
                {            
                    var stopwatch = Stopwatch.StartNew();

                    await camera.TakePicture(imgCaptureHandler, takePhotoRequest.GetImageEncoding(), takePhotoRequest.GetPixelFormatEncoding());                    
                    stopwatch.Stop();
                    
                    var localFilePath = imgCaptureHandler.GetFilepath();

                    // rename it according to our rules
                    localFilePath = FixFilename(localFilePath);

                    if (configuration.HasStorageInformation())
                    {
                        await IOUtils.UploadFileAsync(subfolder, localFilePath, this.configuration);
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

        public async Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest takePhotoRequest)
        {
            if (IsTakingTimelapse())
            {
               throw new TimelapseInProgressException();
            }

            return await InternalTakePhotoAsync(Constants.PhotosSubFolderName, takePhotoRequest, this.stillResolution);
        }

        private string FixFilename(string localFilePath)
        {
            var directory = Path.GetDirectoryName(localFilePath);
            var extension = Path.GetExtension(localFilePath);
            var now = DateTime.UtcNow.ToString(Constants.TimestampFormat);
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
                IsTakingTimelapse = this.currentTimelapse != null,
            };
        }

        #endregion
    }
}
