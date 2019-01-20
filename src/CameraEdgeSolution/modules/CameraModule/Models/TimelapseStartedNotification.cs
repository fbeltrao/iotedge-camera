using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TimelapseStartedNotification : INotification
    {
        public TimelapseStartedNotification()
        {
        }

        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }


    public class TimelapseStartedNotificationSignalRHandler : INotificationHandler<TimelapseStartedNotification>
    {
        private readonly IHubContext<CameraHub> cameraHub;

        public TimelapseStartedNotificationSignalRHandler(IHubContext<CameraHub> cameraHub)
        {
            this.cameraHub = cameraHub;
        }

        public async Task Handle(TimelapseStartedNotification notification, CancellationToken cancellationToken)
        {
            await this.cameraHub.Clients.All.SendCoreAsync("ontimelapsestarted", new object[] { notification });            
        }
    }
}