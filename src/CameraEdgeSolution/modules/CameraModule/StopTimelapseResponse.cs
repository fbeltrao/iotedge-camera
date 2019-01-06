using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace CameraModule
{
    public class StopTimelapseResponse
    {
        public StopTimelapseResponse()
        {          
        }

        [JsonProperty("id")]
        // Timelapse identifier
        public string Id { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get;  set; }

        [JsonProperty("suceeded")]
        // Request succeded
        public bool Succeded => (string.IsNullOrEmpty(ErrorMessage));
    }
}
