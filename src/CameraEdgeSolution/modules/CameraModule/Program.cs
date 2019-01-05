namespace CameraModule
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.WindowsAzure.Storage;
    using MMALSharp;
    using MMALSharp.Handlers;
    using MMALSharp.Native;
    using MMALSharp.FFmpeg;
    using Newtonsoft.Json;

    class Program
    {
        const string directory = "./cameraoutput";

        static string storageAccount = "";
        static string storageKey = "";
        static string deviceId;
        static string moduleId;

        // indicates if events (new photo, new video, etc.) should be outputted
        static bool outputEvents = false;

        static MMALCamera camera;
        static SemaphoreSlim cameraInUse = new SemaphoreSlim(1);

        static async Task<int> Main(string[] args)
        {
            var initResult = await Init();
            if (initResult != 0)
            {
                return initResult;
            }

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            await WhenCancelled(cts.Token);

            // Clean up camera resources
            Log("Shutting down...");
            camera?.Cleanup();

            return 0;

        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task<int> Init()
        {

            try
            {
                camera = MMALCamera.Instance;
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to obtain camera. Ensure module is running with createOptions: \"HostConfig\": { \"Privileged\": true }");
                return 1;
            }

            if (camera == null)
            {
                Log("Failed to obtain camera. Ensure module is running with createOptions: \"HostConfig\": { \"Privileged\": true }");
                return 1;
            }

            var transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { transportSettings };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Log("Initializing Pi camera module");

            var storageAccountEnv = Environment.GetEnvironmentVariable("storageaccount");
            if (!string.IsNullOrEmpty(storageAccountEnv))
            {
                storageAccount = storageAccountEnv;
                Log($"Using storage account {storageAccount}");
            }

            var storageKeyEnv = Environment.GetEnvironmentVariable("storagekey");
            if (!string.IsNullOrEmpty(storageKeyEnv))
            {
                storageKey = storageKeyEnv;
            }

            deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Environment.MachineName;
            }

            moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            if (string.IsNullOrEmpty(moduleId))
            {
                moduleId = "camera";
            }

            var outputEventsEnv = Environment.GetEnvironmentVariable("outputevents");
            if (bool.TryParse(outputEventsEnv, out var parsedOutputEventsEnv))
            {
                outputEvents = parsedOutputEventsEnv;
                Log($"Outputting events: {outputEvents}");
            }


            var twin = await ioTHubModuleClient.GetTwinAsync();
            HandleTwinChanges(twin.Properties.Desired);

            await ioTHubModuleClient.SetMethodDefaultHandlerAsync(ModuleMethodHandlerAsync, ioTHubModuleClient);
            Log("Default method handler initialized");

            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync((updatedTwin, _) => {
                HandleTwinChanges(updatedTwin);
                return Task.FromResult(0);
            }, null);

            Log("Twin changes callback is set");
            Log("Ready!");
            return 0;

        }

        private static void HandleTwinChanges(TwinCollection desired)
        {
            if (desired == null)
                return;

            if (desired.Contains("storageaccount"))
            {
                storageAccount = desired["storageaccount"];
                Log($"Using storage account {storageAccount}");
            }
            
            if (desired.Contains("storagekey"))
                storageKey = desired["storagekey"];      

            if (desired.Contains("outputevents"))
            {
                outputEvents = desired["outputevents"];
                Log($"Outputting events: {outputEvents}");
            }
        }

        static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}] {text}");
        }

        static void LogError(Exception ex, string text)
        {
            Log(string.Concat(text, System.Environment.NewLine, ex.ToString()));
        }

        private static async Task<MethodResponse> ModuleMethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            var moduleClient = (ModuleClient)userContext;
            switch (methodRequest.Name.ToLowerInvariant())
            {
                case "photo":
                case "takephoto":
                    return await TakePhotoAsync(methodRequest, moduleClient);

                case "timelapse":
                case "taketimelapse":
                case "starttimelapse":
                    return await StartTimelapseAsync(methodRequest, moduleClient);

                case "stoptimelapse":
                    return await StopTimelapseAsync(methodRequest, moduleClient);
                
            }

            // Unkown method name
            Log($"Unkown method call received: {methodRequest.Name}");
            return new MethodResponse((int)HttpStatusCode.NotFound);
        }

        private static Task<MethodResponse> StopTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {
            throw new NotImplementedException();
        }


        static string currentTimelapseId = null;

        private static async Task<MethodResponse> StartTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {
            if (currentTimelapseId != null)
            {
                return new MethodResponse(
                    Encoding.UTF8.GetBytes($"Timelapse {currentTimelapseId.ToString()} is being taken"),
                    (int)HttpStatusCode.Conflict);
            }

            var cameraUsed = false;

            try
            {
                var path = EnsureLocalDirectoryExists();
                currentTimelapseId = Guid.NewGuid().ToString();
                var pathForImages = Path.Combine(path, currentTimelapseId);
                var pathForVideo = Path.Combine(path, currentTimelapseId, "video");

                await cameraInUse.WaitAsync();
                cameraUsed = true;

                // This example will take an image every 10 seconds for 4 hours
                var imgCaptureHandler = new ImageStreamCaptureHandler(pathForImages, "jpg");
                var duration = TimeSpan.FromMinutes(1);
                var interval = 10;
                var cts = new CancellationTokenSource(duration);
                var tl = new Timelapse { Mode = TimelapseMode.Second, CancellationToken = cts.Token, Value = interval };

                Log($"Starting timelapse {currentTimelapseId}");
                _ = camera.TakePictureTimelapse(imgCaptureHandler, MMALEncoding.JPEG, MMALEncoding.I420, tl)
                    .ContinueWith((t) => PrepareTimelapseVideo(t, imgCaptureHandler, pathForVideo));
 
                return new MethodResponse(
                    GetJsonBytes(new TakeTimelapseResponse()
                    {
                        Id = currentTimelapseId,
                        Duration = duration,
                        Interval = interval,
                    }), 
                    (int)HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                if (cameraUsed)
                    cameraInUse.Release();

                LogError(ex, "Failed to take timelapse");

                return new MethodResponse(Encoding.UTF8.GetBytes($"Error taking timelapse: {ex.Message}"), (int)HttpStatusCode.InternalServerError);
            }
        }
         
        static void PrepareTimelapseVideo(Task captureImageTask, ImageStreamCaptureHandler imgCaptureHandler, string path)
        {
            try
            {
                if (captureImageTask.IsCompletedSuccessfully)
                {
                    Log($"Will create timelapse {currentTimelapseId} video from {path}");

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                            
                    // Process all images captured into a video at 2fps.
                    imgCaptureHandler.ImagesToVideo(path, 2);

                    Log($"Timelapse video for {currentTimelapseId} created");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to create timelapse video");
            }
            finally
            {
                currentTimelapseId = null;
                cameraInUse.Release();

                imgCaptureHandler?.Dispose();
            }
        }
    

        static string EnsureLocalDirectoryExists()
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

        private static async Task<MethodResponse> TakePhotoAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {
            var cameraWasUsed = false;

            try
            {
                var takePhotoRequest = TakePhotoRequest.FromJson(methodRequest.DataAsJson);

                var path = EnsureLocalDirectoryExists();

                await cameraInUse.WaitAsync();
                cameraWasUsed = true;

                using (var imgCaptureHandler = new ImageStreamCaptureHandler(path, takePhotoRequest.ImageType))        
                {            
                    var stopwatch = Stopwatch.StartNew();
                    await camera.TakePicture(imgCaptureHandler, takePhotoRequest.GetImageEncoding(), takePhotoRequest.GetPixelFormatEncoding());                    
                    stopwatch.Stop();
                    
                    var localFilePath = imgCaptureHandler.GetFilepath();
                    var blobName = await UploadPhotoAsync(localFilePath);

                    var fi = new FileInfo(localFilePath);
                    Log($"New photo: {fi.Length} bytes, in {stopwatch.ElapsedMilliseconds}ms");

                    if (takePhotoRequest.DeleteLocalFile)
                    {
                        File.Delete(localFilePath);
                    }

                    var result = new TakePhotoResponse()
                    {
                        BlobName = blobName,
                        LocalFilePath = localFilePath,
                        DeleteLocalFile = takePhotoRequest.DeleteLocalFile,
                        PixelFormat = takePhotoRequest.PixelFormat,
                        ImageType = takePhotoRequest.ImageType,
                    };

                    var resultInBytes = GetJsonBytes(result);

                    if (outputEvents)
                    {
                        await moduleClient.SendEventAsync("cameraOutput", new Message(resultInBytes));
                    }

                    return new MethodResponse(
                        resultInBytes,
                        (int)HttpStatusCode.OK
                    );
                }           
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to take new photo");

                return new MethodResponse(Encoding.UTF8.GetBytes($"Error taking new photo: {ex.Message}"), (int)HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (cameraWasUsed)
                    cameraInUse.Release();

            }
        }

        // Helper that converts an object to json and returns the byte[] with string value
        static byte[] GetJsonBytes(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        

        private static async Task<string> UploadPhotoAsync(string localFilePath)
        {
            if (CloudStorageAccount.TryParse($"DefaultEndpointsProtocol=https;AccountName={storageAccount};AccountKey={storageKey};EndpointSuffix=core.windows.net", out var cloudStorageAccount))
            {
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();
                var containerReference = blobClient.GetContainerReference(moduleId);
                
                if (await containerReference.CreateIfNotExistsAsync())
                {
                    Log($"Container {moduleId} created");
                }

                var filename = Path.GetFileName(localFilePath);

                var blobName = $"photos/{deviceId}/{filename}";
                var appendBlob = containerReference.GetAppendBlobReference(blobName);

                await appendBlob.UploadFromFileAsync(localFilePath);
                Log($"File {localFilePath} copied to blob {blobName}");

                return appendBlob.Uri.ToString();
            }
            else
            {
                Log("Failed to create cloud storage from connection string");
                return string.Empty;
            }
        }
    }
}
