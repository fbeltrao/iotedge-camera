using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#if DEBUG
namespace CameraModule.Models
{
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

        public Task<CameraTimelapseBase> CreateTimelapseAsync(TakeTimelapseRequest req)
        {
            throw new NotImplementedException();
        }

        public Task<StopTimelapseResponse> StopTimelapseAsync(StopTimelapseRequest req) => Task.FromResult(new StopTimelapseResponse());

        public Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req)
        {
            var picIndex = random.Next(mockupPhotos.Length);
            var newPhotoFileName = mockupPhotos[picIndex];
            var newPhoto = Path.Combine(basePath, newPhotoFileName);
            return Task.FromResult(new TakePhotoResponse() {
                DeleteLocalFile = false,
                ImageType = "jpeg",
                Filename = newPhotoFileName,           
            });
        }

        public Task<IReadOnlyList<string>> GetPhotosAsync() => Task.FromResult<IReadOnlyList<string>>(this.mockupPhotos);

        public Task<Stream> GetPhotoStreamAsync(string image) => Task.FromResult<Stream>(File.OpenRead(Path.Combine(basePath, image)));

        public Task<IReadOnlyList<string>> GetTimelapsesAsync() => Task.FromResult<IReadOnlyList<string>>(new string[0]);

        public Task<Stream> GetTimelapseStreamAsync(string timelapse) => Task.FromResult((Stream)null);

        public CameraStatus GetCameraStatus()
        {
            return new CameraStatus()
            {
                IsTakingTimelapse = false,
            };
        }
    }
}
#endif