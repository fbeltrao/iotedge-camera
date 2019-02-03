using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace CameraModule.Models
{
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