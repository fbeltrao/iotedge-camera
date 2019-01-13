using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CameraModule
{
    public interface ICamera
    {
        StopTimelapseResponse StopTimelapse(StopTimelapseRequest req);
        Task<TakeTimelapseResponse> StartTimelapseAsync(TakeTimelapseRequest req);
        Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req);
        bool Initialize();
        Task<IReadOnlyList<string>> GetImagesAsync();
        Task<Stream> GetImageStreamAsync(string image);
    }

    public class TestCamera : ICamera
    {
        const string basePath = "/Users/fbeltrao/dev/github.com/fbeltrao/iotedge-camera/src/CameraEdgeSolution/modules/CameraModule/testPics";
        string[] mockupPhotos = new string[] {
            "2019-01-06-105218-fbepi2-photo.jpeg",
            "2019-01-19-105218-fbepi2-photo.jpeg",
            "2019-01-19-105219-fbepi2-photo.jpeg",
            "2019-01-19-105220-fbepi2-photo.jpeg"
        };

        Random random = new Random();

        public bool Initialize() => true;

        public Task<TakeTimelapseResponse> StartTimelapseAsync(TakeTimelapseRequest req)
        {
            return Task.FromResult(new TakeTimelapseResponse());
        }

        public StopTimelapseResponse StopTimelapse(StopTimelapseRequest req) => new StopTimelapseResponse();

        public Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req)
        {
            var picIndex = random.Next(mockupPhotos.Length);
            var newPhoto = Path.Combine(basePath, mockupPhotos[picIndex]);
            return Task.FromResult(new TakePhotoResponse() {
                DeleteLocalFile = false,
                ImageType = "jpeg",
                LocalFilePath = newPhoto,           
            });
        }

        public Task<IReadOnlyList<string>> GetImagesAsync() => Task.FromResult<IReadOnlyList<string>>(this.mockupPhotos);

        public Task<Stream> GetImageStreamAsync(string image) => Task.FromResult<Stream>(File.OpenRead(Path.Combine(basePath, image)));
    }
}
