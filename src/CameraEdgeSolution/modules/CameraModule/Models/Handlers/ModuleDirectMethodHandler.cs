using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    // Handles module direct methods
    public class ModuleDirectMethodHandler : IRequestHandler<ModuleDirectMethodRequest, MethodResponse>
    {
        private readonly IMediator mediator;

        public ModuleDirectMethodHandler(IMediator mediator)
        {
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
        public Task<MethodResponse> Handle(ModuleDirectMethodRequest request, CancellationToken cancellationToken)
        {
            switch (request.MethodRequest.Name.ToLowerInvariant())
            {
                case "photo":
                case "takephoto":
                    return TakePhotoAsync(request.MethodRequest);

                case "timelapse":
                case "taketimelapse":
                case "starttimelapse":
                    return StartTimelapseAsync(request.MethodRequest);

                case "stoptimelapse":
                    return StopTimelapseAsync(request.MethodRequest);
            }

            // Unkown method name
            Logger.Log($"Unkown method call received: {request.MethodRequest.Name}");
            return Task.FromResult(new MethodResponse((int)HttpStatusCode.NotFound));
        }

        byte[] GetJsonBytes(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

        private async Task<MethodResponse> StopTimelapseAsync(MethodRequest methodRequest)
        {
            try
            {
                var req = IsEmptyMethodRequestJson(methodRequest) ?
                    new StopTimelapseApiRequest() :
                    JsonConvert.DeserializeObject<StopTimelapseApiRequest>(methodRequest.DataAsJson);

                var response = await this.mediator.Send(req);

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

        private async Task<MethodResponse> StartTimelapseAsync(MethodRequest methodRequest)
        {
            try
            {
                var req = IsEmptyMethodRequestJson(methodRequest) ?
                    new StartTimelapseApiRequest() :
                    JsonConvert.DeserializeObject<StartTimelapseApiRequest>(methodRequest.DataAsJson);

                var response = await this.mediator.Send(req);

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

        private async Task<MethodResponse> TakePhotoAsync(MethodRequest methodRequest)
        {   
            try
            {
                //Logger.Log($"Take photo input json: '{methodRequest.DataAsJson}'");
                var req = IsEmptyMethodRequestJson(methodRequest)
                    ? new TakePhotoApiRequest()
                    : JsonConvert.DeserializeObject<TakePhotoApiRequest>(methodRequest.DataAsJson);
                
                var response = await mediator.Send(req);
            
                var responseAsBytes = GetJsonBytes(response);
                
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

        private bool IsEmptyMethodRequestJson(MethodRequest methodRequest)
        {
            return string.IsNullOrEmpty(methodRequest.DataAsJson) || methodRequest.DataAsJson == "\"\"";
        }
    }
}
