using System;
using Newtonsoft.Json;

namespace CameraModule
{
    public class StopTimelapseRequest
    {
        

        public StopTimelapseRequest()
        {

        }

        public static StopTimelapseRequest FromJson(string json)
        {
             if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<StopTimelapseRequest>(json);
                    if (result != null) 
                        return result;
                }
                catch
                {

                }
            }
            
            return new StopTimelapseRequest();
        }
    }
}
