using System;
using System.Linq;
using Microsoft.Azure.Devices.Shared;
using MMALSharp;

namespace CameraModule
{
    public class CameraConfiguration
    {
        public bool OutputEvents { get; set; }
        public string StorageAccount { get; set; }
        public string StorageKey { get; set; }
        public string DeviceId { get; set; }
        public string ModuleId { get; set; }

        public static CameraConfiguration CreateFromEnvironmentVariables()
        {
            var result = new CameraConfiguration();

            var storageAccountEnv = Environment.GetEnvironmentVariable("storageaccount");
            if (!string.IsNullOrEmpty(storageAccountEnv))
            {
                result.StorageAccount = storageAccountEnv;
                Logger.Log($"Using storage account {result.StorageAccount}");
            }

            var storageKeyEnv = Environment.GetEnvironmentVariable("storagekey");
            if (!string.IsNullOrEmpty(storageKeyEnv))
            {
                result.StorageKey = storageKeyEnv;
            }

            result.DeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            if (string.IsNullOrEmpty(result.DeviceId))
            {
                result.DeviceId = Environment.MachineName;
            }

            result.ModuleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            if (string.IsNullOrEmpty(result.ModuleId))
            {
                result.ModuleId = "camera";
            }

            var outputEventsEnv = Environment.GetEnvironmentVariable("outputevents");
            if (bool.TryParse(outputEventsEnv, out var parsedOutputEventsEnv))
            {
                result.OutputEvents = parsedOutputEventsEnv;
                Logger.Log($"Outputting events: {result.OutputEvents}");
            }


            return result;
        }

        internal void UpdateFromTwin(TwinCollection desired)
        {
            if (desired.Contains("storageaccount"))
            {
                this.StorageAccount = desired["storageaccount"];
                Logger.Log($"Using storage account {StorageAccount}");
            }
            
            if (desired.Contains("storagekey"))
                this.StorageKey = desired["storagekey"];      

            if (desired.Contains("outputevents"))
            {
                this.OutputEvents = desired["outputevents"];
                Logger.Log($"Outputting events: {OutputEvents}");
            }

            if (desired.Contains("rotation"))
            {
                var cameraRotationValidValues = new int[] { 0, 90, 180, 270 };
                if (int.TryParse(desired["rotation"].ToString(), out int rotation))
                {
                    if (cameraRotationValidValues.Contains(rotation))
                    {
                        MMALCameraConfig.Rotation = rotation;
                        Logger.Log($"Camera rotation: {rotation}");
                    }
                }
                else
                {
                    Logger.Log($"Invalid camera rotation value: {desired["rotation"].ToString()}. Allowed values are: {string.Join(',', cameraRotationValidValues)}");
                }
            }

            if (desired.Contains("photoresolution"))
            {
                var photoResolutionValue = (string)desired["photoresolution"];
                var wh = photoResolutionValue.Split('x');
                var resolutionSet = false;
                if (wh.Length == 2)
                {
                    if (int.TryParse(wh[0], out var width))
                    {
                        if (int.TryParse(wh[1], out var height))
                        {
                            MMALCameraConfig.StillResolution = new Resolution(width, height);
                            Logger.Log($"Camera photo resolution: {photoResolutionValue}");
                            resolutionSet = true;
                        }
                    }
                }

                if (!resolutionSet)
                {
                    Logger.Log($"Invalid photo resolution: {photoResolutionValue}. Should be WidthxHeight (i.e. 640x480)");
                }
            }
        }
    }
}
