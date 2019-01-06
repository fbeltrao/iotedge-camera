using System;
using Newtonsoft.Json;

namespace CameraModule
{
    public class TakeTimelapseRequest
    {
        [JsonProperty("interval")]
        // Interval in which pictures should be taken
        public int Interval { get; set; }

        [JsonProperty("duration")]
        // How long the timelapse lasts
        public int Duration { get; set; }

        public TakeTimelapseRequest()
        {
            this.Interval = 10;
            this.Duration = 60;
        }

        public static TakeTimelapseRequest FromJson(string json)
        {
             if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<TakeTimelapseRequest>(json);
                    if (result != null) 
                        return result;
                }
                catch
                {

                }
            }
            
            return new TakeTimelapseRequest();
        }
    }
}
