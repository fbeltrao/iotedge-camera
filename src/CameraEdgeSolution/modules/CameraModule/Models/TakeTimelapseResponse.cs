using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TakeTimelapseResponse
    {
        public TakeTimelapseResponse()
        {          
        }

        [JsonProperty("id")]
        // Timelapse identifier
        public string Id { get; set; }
        
        
        [JsonProperty("interval")]
        // Interval in which pictures should be taken
        public int Interval { get; set; }

        [JsonProperty("duration")]
        // How long the timelapse lasts
        public int Duration { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get;  set; }

        [JsonProperty("suceeded")]
        // Request succeded
        public bool Succeded => (string.IsNullOrEmpty(ErrorMessage));

        [JsonProperty("isTakingTimelapse")]
        public bool IsTakingTimelapse { get;  set; }
    }
}
