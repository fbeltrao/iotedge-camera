using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CameraModule
{
    public class IoTHubModuleConnector : IHostedService
    {
        private readonly ILogger logger;
        private readonly ICamera camera;
        private readonly CameraConfiguration configuration;
        private ModuleClient moduleClient;

        public IoTHubModuleConnector(ILogger<IoTHubModuleConnector> logger, ICamera camera, CameraConfiguration configuration)
        {
            this.logger = logger;
            this.camera = camera;
            this.configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!this.camera.Initialize())
            {
                Environment.Exit(1);
            }

            var transportSettings = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { transportSettings };

            // Open a connection to the Edge runtime
            this.moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await moduleClient.OpenAsync();
            Logger.Log("Initializing Pi camera module");

            await this.configuration.ConnectToModuleAsync(moduleClient);

            await moduleClient.SetMethodDefaultHandlerAsync(ModuleMethodHandlerAsync, moduleClient);
            Logger.Log("Default method handler initialized");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        // Helper that converts an object to json and returns the byte[] with string value
        byte[] GetJsonBytes(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        
       
        private async Task<MethodResponse> ModuleMethodHandlerAsync(MethodRequest methodRequest, object userContext)
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

        private MethodResponse StopTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
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

        private async Task<MethodResponse> StartTimelapseAsync(MethodRequest methodRequest, ModuleClient moduleClient)
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

        private async Task<MethodResponse> TakePhotoAsync(MethodRequest methodRequest, ModuleClient moduleClient)
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

        private async Task SendEventAsync(ModuleClient moduleClient, byte[] result)
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
