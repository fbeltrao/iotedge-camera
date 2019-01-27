using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TakePhotoApiRequest : IRequest<TakePhotoResponse>
    {
    }


    public class PhotoTakenNotification : INotification
    {
        [JsonProperty("details")]
        public TakePhotoResponse Details { get; set; }

        [JsonProperty("details")]
        public string Timelapse { get; set; }

        [JsonProperty("isTimelapsePhoto")]
        public bool IsTimelapsePhoto { get; set; }
    }

    public class PhotoTakenNotificationSignalRHandler : INotificationHandler<PhotoTakenNotification>
    {
        private readonly IHubContext<CameraHub> cameraHub;

        public PhotoTakenNotificationSignalRHandler(IHubContext<CameraHub> cameraHub)
        {
            this.cameraHub = cameraHub;
        }
        public async Task Handle(PhotoTakenNotification notification, CancellationToken cancellationToken)
        {
            await this.cameraHub.Clients.All.SendCoreAsync("onnewphoto", new object[] { notification }, cancellationToken);            
        }
    }

    public class TakePhotoApiRequestHandler : IRequestHandler<TakePhotoApiRequest, TakePhotoResponse>
    {
        private readonly ICamera camera;
        private readonly IMediator mediator;
        private readonly CameraConfiguration configuration;

        public TakePhotoApiRequestHandler(ICamera camera, IMediator mediator, CameraConfiguration configuration)
        {
            this.camera = camera;
            this.mediator = mediator;
            this.configuration = configuration;
        }

        public async Task<TakePhotoResponse> Handle(TakePhotoApiRequest request, CancellationToken cancellationToken)
        {
            var photoRequest = new TakePhotoRequest()
            {
                OutputDirectory = configuration.EnsureOutputDirectoryExists(Constants.PhotosSubFolderName),
            };

            try
            {
                var response = await camera.TakePhotoAsync(photoRequest);
                if (response.Succeded)
                {
                    await this.mediator.Publish(new PhotoTakenNotification()
                    {
                        Details = response,
                    });
                }
                    
                return response;
            }
            catch (TimelapseInProgressException)
            {
                return new TakePhotoResponse()
                {
                    IsTakingTimelapse = true,
                };
            }
        }
    }

    public class PhotoTakenNotificationModuleEventHandler : INotificationHandler<PhotoTakenNotification>
    {
        private readonly CameraConfiguration configuration;
        private readonly ModuleClient moduleClient;

        public PhotoTakenNotificationModuleEventHandler(CameraConfiguration configuration, ModuleClient moduleClient)
        {
            this.configuration = configuration;
            this.moduleClient = moduleClient;
        }

        public async Task Handle(PhotoTakenNotification notification, CancellationToken cancellationToken)
        {
            if (configuration.OutputEvents)
            {
                var msg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notification)));
                msg.ContentEncoding = "utf-8";
                msg.ContentType = "application/json";
                await this.moduleClient.SendEventAsync(msg);
            }
        }
    }
}