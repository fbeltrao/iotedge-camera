using System.Threading.Tasks;

namespace CameraModule
{
    public interface ICamera
    {
        StopTimelapseResponse StopTimelapse(StopTimelapseRequest req);
        Task<TakeTimelapseResponse> StartTimelapseAsync(TakeTimelapseRequest req);
        Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req);
        bool Initialize();
    }

    public class TestCamera : ICamera
    {
        public bool Initialize() => true;

        public Task<TakeTimelapseResponse> StartTimelapseAsync(TakeTimelapseRequest req)
        {
            return Task.FromResult(new TakeTimelapseResponse());
        }

        public StopTimelapseResponse StopTimelapse(StopTimelapseRequest req) => new StopTimelapseResponse();

        public Task<TakePhotoResponse> TakePhotoAsync(TakePhotoRequest req)
        {
            return Task.FromResult(new TakePhotoResponse());
        }
    }
}
