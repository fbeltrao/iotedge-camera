using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TimelapseTakenNotification : INotification
    {
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }

    public class TimelapseTakenNotificationSignalRHandler : INotificationHandler<TimelapseTakenNotification>
    {
        private readonly IHubContext<CameraHub> cameraHub;

        public TimelapseTakenNotificationSignalRHandler(IHubContext<CameraHub> cameraHub)
        {
            this.cameraHub = cameraHub;
        }

        public async Task Handle(TimelapseTakenNotification notification, CancellationToken cancellationToken)
        {
            await this.cameraHub.Clients.All.SendCoreAsync("onnewtimelapse", new object[] { notification });            
        }
    }
}