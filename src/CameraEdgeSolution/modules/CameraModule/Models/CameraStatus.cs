using System.Collections.Generic;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class CameraStatus
    {
        [JsonProperty("isTakingTimelapse")]
        public bool IsTakingTimelapse { get; set; }
        
    }
}
