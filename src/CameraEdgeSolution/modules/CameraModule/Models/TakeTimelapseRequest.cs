using System;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class TakeTimelapseRequest
    {
        [JsonProperty("interval")]
        // Interval in which pictures should be taken
        public int Interval { get; set; }

        [JsonProperty("duration")]
        // How long the timelapse lasts (in seconds)
        public int Duration { get; set; }

        public string OutputDirectory { get; set; }

        public TakeTimelapseRequest()
        {
            this.Interval = 10;
            this.Duration = 60 * 10;
        }
    }
}
