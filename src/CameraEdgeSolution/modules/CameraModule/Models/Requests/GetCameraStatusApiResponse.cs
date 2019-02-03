using System.Collections.Generic;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class GetCameraStatusApiResponse
    {
        [JsonProperty("isTakingTimelapse")]
        public bool IsTakingTimelapse { get; set; }        

        [JsonProperty("photos")]
        public IReadOnlyList<string> Photos { get; set; }

        [JsonProperty("timelapses")]
        public IReadOnlyList<string> Timelapses { get; set; }
    }
}