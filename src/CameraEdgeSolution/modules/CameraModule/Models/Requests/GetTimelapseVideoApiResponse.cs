using System.IO;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class GetTimelapseVideoApiResponse
    {   
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        public Stream Stream { get; set; }


        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}