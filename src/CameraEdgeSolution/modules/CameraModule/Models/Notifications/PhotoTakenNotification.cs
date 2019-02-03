using MediatR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class PhotoTakenNotification : INotification
    {
        [JsonProperty("details")]
        public TakePhotoResponse Details { get; set; }

        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }

        [JsonProperty("isTimelapsePhoto")]
        public bool IsTimelapsePhoto { get; set; }
    }
}