using MediatR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TimelapseTakenNotification : INotification
    {
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }
}