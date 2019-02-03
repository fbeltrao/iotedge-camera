using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CameraModule.Models
{
    public class TimelapseTakenNotificationSignalRHandler : INotificationHandler<TimelapseTakenNotification>
    {
        private readonly IHubContext<CameraHub> cameraHub;

        public TimelapseTakenNotificationSignalRHandler(IHubContext<CameraHub> cameraHub)
        {
            this.cameraHub = cameraHub; 
        }

        public async Task Handle(TimelapseTakenNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await this.cameraHub.Clients.All.SendCoreAsync("onnewtimelapse", new object[] { notification });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed sending signalr message onewtimelapse");
            }         
        }
    }
}