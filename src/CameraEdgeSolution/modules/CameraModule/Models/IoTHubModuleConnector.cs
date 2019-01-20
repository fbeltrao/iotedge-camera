// using System;
// using System.Net;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using MediatR;
// using Microsoft.Azure.Devices.Client;
// using Microsoft.Azure.Devices.Shared;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Newtonsoft.Json;

// namespace CameraModule.Models
// {
//     public class IoTHubModuleConnector : IHostedService
//     {
//         private readonly ILogger logger;
//         private readonly IMediator mediator;
//         private readonly ICamera camera;
//         private readonly CameraConfiguration configuration;
//         private ModuleClient moduleClient;

//         public IoTHubModuleConnector(ILogger<IoTHubModuleConnector> logger, ModuleClient moduleClient, IMediator mediator, ICamera camera, CameraConfiguration configuration)
//         {
//             this.logger = logger;
//             this.moduleClient = moduleClient;
//             this.mediator = mediator;
//             this.camera = camera;
//             this.configuration = configuration;
//         }

//         public async Task StartAsync(CancellationToken cancellationToken)
//         {
//             if (!this.camera.Initialize())
//             {
//                 Environment.Exit(1);
//             }

//             await moduleClient.OpenAsync();
//             Logger.Log("Initializing Pi camera module");

//             await this.configuration.ConnectToModuleAsync(moduleClient);

//             await moduleClient.SetMethodDefaultHandlerAsync(ModuleMethodHandlerAsync, moduleClient);
//             Logger.Log("Default method handler initialized");
//         }

//         public Task StopAsync(CancellationToken cancellationToken) => Task.FromResult(0);

//         // Helper that converts an object to json and returns the byte[] with string value
//         byte[] GetJsonBytes(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        
//         private async Task<MethodResponse> ModuleMethodHandlerAsync(MethodRequest methodRequest, object userContext)
//         {
//             var moduleClient = (ModuleClient)userContext;

//             switch (methodRequest.Name.ToLowerInvariant())
//             {
//                 case "photo":
//                 case "takephoto":
//                     return await TakePhotoAsync(methodRequest);

//                 case "timelapse":
//                 case "taketimelapse":
//                 case "starttimelapse":
//                     return await StartTimelapseAsync(methodRequest);

//                 case "stoptimelapse":
//                     return await StopTimelapseAsync(methodRequest);
//             }

//             // Unkown method name
//             Logger.Log($"Unkown method call received: {methodRequest.Name}");
//             return new MethodResponse((int)HttpStatusCode.NotFound);
//         }

//         private async Task<MethodResponse> StopTimelapseAsync(MethodRequest methodRequest)
//         {
//             try
//             {
//                 var req = string.IsNullOrEmpty(methodRequest.DataAsJson) ?
//                     new StopTimelapseApiRequest() :
//                     JsonConvert.DeserializeObject<StopTimelapseApiRequest>(methodRequest.DataAsJson);

//                 var response = await this.mediator.Send(req);

//                 var responseAsBytes = GetJsonBytes(response);
//                 return new MethodResponse(
//                     responseAsBytes,
//                     response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
//                 );
//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "Failed to start new timelapse");

//                 return new MethodResponse(
//                     Encoding.UTF8.GetBytes($"Error starting timelapse: {ex.Message}"), 
//                     (int)HttpStatusCode.InternalServerError);
//             }   
//         }

//         private async Task<MethodResponse> StartTimelapseAsync(MethodRequest methodRequest)
//         {
//             try
//             {
//                 var req = string.IsNullOrEmpty(methodRequest.DataAsJson) ?
//                     new StartTimelapseApiRequest() :
//                     JsonConvert.DeserializeObject<StartTimelapseApiRequest>(methodRequest.DataAsJson);

//                 var response = await this.mediator.Send(req);

//                 var responseAsBytes = GetJsonBytes(response);
//                 return new MethodResponse(
//                     responseAsBytes,
//                     response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
//                 );
//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "Failed to start new timelapse");

//                 return new MethodResponse(
//                     Encoding.UTF8.GetBytes($"Error starting timelapse: {ex.Message}"), 
//                     (int)HttpStatusCode.InternalServerError);
//             }        
//         }

//         private async Task<MethodResponse> TakePhotoAsync(MethodRequest methodRequest)
//         {   
//             try
//             {
//                 var req = string.IsNullOrEmpty(methodRequest.DataAsJson) 
//                     ? new TakePhotoApiRequest()
//                     : JsonConvert.DeserializeObject<TakePhotoApiRequest>(methodRequest.DataAsJson);
                
//                 var response = await mediator.Send(req);
            
//                 var responseAsBytes = GetJsonBytes(response);
                
//                 return new MethodResponse(
//                     responseAsBytes,
//                     response.Succeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Conflict
//                 );
//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "Failed to take new photo");
//                 return new MethodResponse(
//                     Encoding.UTF8.GetBytes($"Error taking new photo: {ex.Message}"), 
//                     (int)HttpStatusCode.InternalServerError);
//             }
//         }

//         private async Task SendEventAsync(ModuleClient moduleClient, byte[] result)
//         {
//             try
//             {
//                 await moduleClient.SendEventAsync("cameraOutput", new Message(result));
//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "Error sending module event");
//             }
//         }
//     }
// }
