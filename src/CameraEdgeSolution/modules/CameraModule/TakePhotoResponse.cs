using Newtonsoft.Json;

namespace CameraModule
{
    public class TakePhotoResponse
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("blobName")]
        public string BlobName { get; set; }

        [JsonProperty("deleteLocalFile")]
        public bool DeleteLocalFile { get; set; }

        [JsonProperty("pixelFormat")]
        public string PixelFormat { get; set; }

        [JsonProperty("imageType")]
        public string ImageType { get; set; }

        [JsonProperty("quickMode")]
        public bool QuickMode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("suceeded")]
        // Request succeded
        public bool Succeded => (string.IsNullOrEmpty(ErrorMessage));
    }
}
