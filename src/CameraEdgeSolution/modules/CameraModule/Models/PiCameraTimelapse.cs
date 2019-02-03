using System;
using System.Threading.Tasks;

namespace CameraModule.Models
{
    internal class PiCameraTimelapse : CameraTimelapseBase
    {
        private readonly PiCamera camera;

        internal PiCameraTimelapse(TimeSpan interval, TimeSpan duration, CameraConfiguration configuration, PiCamera camera)
            : base(interval, duration, configuration)
        {
            this.camera = camera;
        }

        protected override Task<TakePhotoResponse> TakeTimelapsePhotoAsync() => this.camera.TakeTimelapsePhotoAsync(this.ID);
    }
}