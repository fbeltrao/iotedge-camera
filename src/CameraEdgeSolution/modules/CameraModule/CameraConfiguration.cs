using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
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
        public bool WebServerEnabled { get; set; }

        // Get/sets the camera rotation
        public int CameraRotation { get; set; }

        public int? CameraPhotoResolutionWidth { get; set; }
        public int? CameraPhotoResolutionHeight { get; set; }

        HashSet<Action> subscribers = new HashSet<Action>();

        // Subscribe to changes in the configuration
        public void Subscribe(Action action) => subscribers.Add(action);

        public static CameraConfiguration CreateFromEnvironmentVariables()
        {
            var configuration = new CameraConfiguration();
            configuration.InitializeFromEnvironmentVariables();
            return configuration;
        }

        internal void InitializeFromEnvironmentVariables()
        {
            var storageAccountEnv = Environment.GetEnvironmentVariable("storageaccount");
            if (!string.IsNullOrEmpty(storageAccountEnv))
            {
                this.StorageAccount = storageAccountEnv;
                Logger.Log($"Using storage account {this.StorageAccount}");
            }

            var storageKeyEnv = Environment.GetEnvironmentVariable("storagekey");
            if (!string.IsNullOrEmpty(storageKeyEnv))
            {
                this.StorageKey = storageKeyEnv;
            }

            this.DeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            if (string.IsNullOrEmpty(this.DeviceId))
            {
                this.DeviceId = Environment.MachineName;
            }

            this.ModuleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            if (string.IsNullOrEmpty(this.ModuleId))
            {
                this.ModuleId = "camera";
            }
            Logger.Log($"Module: {this.ModuleId}");

            var outputEventsEnv = Environment.GetEnvironmentVariable("outputevents");
            if (bool.TryParse(outputEventsEnv, out var parsedOutputEventsEnv))
            {
                this.OutputEvents = parsedOutputEventsEnv;
                Logger.Log($"Outputting events: {this.OutputEvents}");
            }

            var webserverEnv = Environment.GetEnvironmentVariable("webserver");
            if (bool.TryParse(webserverEnv, out var parsedWebServerEnv))
            {
                this.WebServerEnabled = parsedWebServerEnv;
                Logger.Log($"Web server: {this.WebServerEnabled}");
            }
        }

        // Connects the configuration to an IoT Edge module twin
        internal async Task ConnectToModuleAsync(ModuleClient moduleClient)
        {
            var twin = await moduleClient.GetTwinAsync();
            await UpdateFromTwin(twin.Properties?.Desired, moduleClient);

            await moduleClient.SetDesiredPropertyUpdateCallbackAsync(UpdateFromTwin, moduleClient);

            Logger.Log("Twin changes callback is set");
        }

        internal Task UpdateFromTwin(TwinCollection desired, object userContext)
        {
            if (desired == null)
                return Task.FromResult(0);

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
                        this.CameraRotation = rotation;
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
                            this.CameraPhotoResolutionHeight = height;
                            this.CameraPhotoResolutionWidth = width;
                            resolutionSet = true;
                        }
                    }
                }

                if (!resolutionSet)
                {
                    Logger.Log($"Invalid photo resolution: {photoResolutionValue}. Should be WidthxHeight (i.e. 640x480)");
                }
            }

            foreach (var subscriber in this.subscribers)
            {
                try
                {
                    subscriber();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to updated configuration changes subscriber");
                }
                
            }

            return Task.FromResult(0);
        }

        // Gets if a Azure storage information was provided
        internal bool HasStorageInformation() => !string.IsNullOrEmpty(this.StorageAccount) && !string.IsNullOrEmpty(this.StorageKey);
    
    
          // Gets the output directory
        internal string  GetOuputDirectory()
        {
            if (Directory.Exists("/cameraoutput"))
                return "/cameraoutput";

            return "./cameraoutput";
        }

        internal string EnsureOutputDirectoryExists()
        {
            var directory = GetOuputDirectory();
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
                throw new Exception($"Failed to create '{directory}' folder", ex);
            }
        }
    }
}
