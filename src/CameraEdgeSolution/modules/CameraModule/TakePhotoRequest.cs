namespace CameraModule
{
    using System;
    using MMALSharp.Native;
    using Newtonsoft.Json;

    public class TakePhotoRequest
    {
        string imageType = "jpeg";

        [JsonProperty("imageType")]
        public string ImageType 
        { 
            get => imageType;
            set => imageType = string.IsNullOrEmpty(value) ? "jpeg" : value;
        }

        string pixelFormat = "I420";

        [JsonProperty("pixelFormat")]
        public string PixelFormat 
        { 
            get => pixelFormat;
            set => pixelFormat = string.IsNullOrEmpty(value) ? "I420" : value;
        }

        [JsonProperty("deleteLocalFile")]
        public bool DeleteLocalFile { get; set; }

        [JsonProperty("quickMode")]
        public bool QuickMode { get; set; }

        public MMALEncoding GetImageEncoding()
        {
            switch (this.ImageType.ToLowerInvariant())
            {
                case "png":
                    return MMALEncoding.PNG;

                case "bmp":
                    return MMALEncoding.BMP;

                 case "gif":
                    return MMALEncoding.GIF;

                default:
                    return MMALEncoding.JPEG;
            }
        }

        public MMALEncoding GetPixelFormatEncoding()
        {
            switch (this.PixelFormat.ToLowerInvariant())
            {
                case "i422slice":
                case "i422_slice":
                    return MMALEncoding.I422_SLICE;

                case "i422":
                    return MMALEncoding.I422;

                case "i420slice":
                case "i420_slice":
                    return MMALEncoding.I420_SLICE;

                default:
                    return MMALEncoding.I420;
            }
        }

        public TakePhotoRequest()
        {
         
        }

        public static TakePhotoRequest FromJson(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<TakePhotoRequest>(json);
                    if (result != null) 
                        return result;
                }
                catch
                {

                }
            }
            
            return new TakePhotoRequest();
        }
    }   
}
