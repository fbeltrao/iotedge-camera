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
}
