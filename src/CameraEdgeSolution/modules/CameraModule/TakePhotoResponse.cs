namespace CameraModule
{
    public class TakePhotoResponse
    {
        public string LocalFilePath { get; set; }

        public string BlobName { get; set; }

        public bool DeleteLocalFile { get; set; }
        public string PixelFormat { get; set; }
        public string ImageType { get; set; }
    }
}
