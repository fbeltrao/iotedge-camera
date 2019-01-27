using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CameraModule.Models
{
    public interface ICamera
    {
        Task<CameraTimelapseBase> CreateTimelapseAsync(TakeTimelapseRequest req);
        Task<StopTimelapseResponse> StopTimelapseAsync(StopTimelapseRequest req);

        Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req);
        bool Initialize();
        CameraStatus GetCameraStatus();
    }
}
