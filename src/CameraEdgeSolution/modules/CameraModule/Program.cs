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
    using Newtonsoft.Json;
    using System.Linq;

    class Program
    {

        static Camera camera;
        static CameraConfiguration configuration;

        static async Task<int> Main(string[] args)
        {
            var initResult = await InitAsync();
            if (initResult != 0)
                return initResult;

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            await WhenCancelled(cts.Token);

            // Clean up camera resources
            Logger.Log("Shutting down...");
            camera?.Dispose();

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
        static async Task<int> InitAsync()
        {
            configuration = CameraConfiguration.CreateFromEnvironmentVariables();

            camera = new Camera(configuration);
            if (!camera.Initialize())
                return -1;

            var transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { transportSettings };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Logger.Log("Initializing Pi camera module");

            var twin = await ioTHubModuleClient.GetTwinAsync();
            HandleTwinChanges(twin.Properties.Desired);

            await ioTHubModuleClient.SetMethodDefaultHandlerAsync(ModuleMethodHandlerAsync, ioTHubModuleClient);
            Logger.Log("Default method handler initialized");

            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync((updatedTwin, _) => {
                HandleTwinChanges(updatedTwin);
                return Task.FromResult(0);
            }, null);

            Logger.Log("Twin changes callback is set");
            Logger.Log("Ready!");
            return 0;

        }

        private static void HandleTwinChanges(TwinCollection desired)
        {
            if (desired == null)
                return;

            configuration.UpdateFromTwin(desired);

        }

        // Helper that converts an object to json and returns the byte[] with string value
        static byte[] GetJsonBytes(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        
       
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
                    return StopTimelapseAsync(methodRequest, moduleClient);
            }

            // Unkown method name
            Logger.Log($"Unkown method call received: {methodRequest.Name}");
            return new MethodResponse((int)HttpStatusCode.NotFound);
        }

        private static MethodResponse StopTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {
            try
            {
                var req = StopTimelapseRequest.FromJson(methodRequest.DataAsJson);
                var response = camera.StopTimelapse(req);
                
                var responseAsBytes = GetJsonBytes(response);
                return new MethodResponse(
                    responseAsBytes,
                    response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start new timelapse");

                return new MethodResponse(
                    Encoding.UTF8.GetBytes($"Error starting timelapse: {ex.Message}"), 
                    (int)HttpStatusCode.InternalServerError);
            }   
        }

        private static async Task<MethodResponse> StartTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {
            try
            {
                var req = TakeTimelapseRequest.FromJson(methodRequest.DataAsJson);
                var response = await camera.StartTimelapseAsync(req);
                
                var responseAsBytes = GetJsonBytes(response);
                return new MethodResponse(
                    responseAsBytes,
                    response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start new timelapse");

                return new MethodResponse(
                    Encoding.UTF8.GetBytes($"Error starting timelapse: {ex.Message}"), 
                    (int)HttpStatusCode.InternalServerError);
            }        
        }

        private static async Task<MethodResponse> TakePhotoAsync(MethodRequest methodRequest, ModuleClient moduleClient)
        {   
            try
            {
                var req = TakePhotoRequest.FromJson(methodRequest.DataAsJson);
                var response = await camera.TakePhotoAsync(req);
                
                var responseAsBytes = GetJsonBytes(response);
                if (configuration.OutputEvents)
                {
                    await SendEventAsync(moduleClient, responseAsBytes);
                }

                return new MethodResponse(
                    responseAsBytes,
                    response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to take new photo");
                return new MethodResponse(
                    Encoding.UTF8.GetBytes($"Error taking new photo: {ex.Message}"), 
                    (int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task SendEventAsync(ModuleClient moduleClient, byte[] result)
        {
            try
            {
                await moduleClient.SendEventAsync("cameraOutput", new Message(result));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending module event");
            }
        }
    }
}
