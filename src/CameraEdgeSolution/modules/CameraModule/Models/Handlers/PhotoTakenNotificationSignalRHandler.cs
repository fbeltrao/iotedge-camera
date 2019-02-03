using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;

namespace CameraModule.Models
{
    public class PhotoTakenNotificationSignalRHandler : INotificationHandler<PhotoTakenNotification>
    {
        private readonly IHubContext<CameraHub> cameraHub;

        public PhotoTakenNotificationSignalRHandler(IHubContext<CameraHub> cameraHub)
        {
            this.cameraHub = cameraHub;
        }
        public async Task Handle(PhotoTakenNotification notification, CancellationToken cancellationToken)
        {
            Logger.Log($"Publishing in signalR, onnewphoto -> {JsonConvert.SerializeObject(notification)}");
            
            try
            {
                await this.cameraHub.Clients.All.SendCoreAsync("onnewphoto", new object[] { notification }, cancellationToken);            
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed sending signalr message onnewphoto");
            } 
        }
    }
}