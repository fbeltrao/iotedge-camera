using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public abstract class CameraTimelapseBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly CameraConfiguration configuration;

        public TimeSpan Interval { get; set; }

        public TimeSpan Duration { get; set; }

        public string ID { get; }

        internal Func<CameraTimelapseBase, Task> OnFinished { get; set; }

        internal Func<CameraTimelapseBase, TakePhotoResponse, Task> OnTimelapsePhotoTaken { get; set; }

        protected CameraTimelapseBase(TimeSpan interval, TimeSpan duration, CameraConfiguration configuration)
        {
            Interval = interval;
            Duration = duration;
            this.configuration = configuration;
            this.ID = DateTime.UtcNow.ToString(Constants.TimestampFormat);
        }

        public void Start()
        {
            if (this.cancellationTokenSource != null)
                throw new InvalidOperationException("Timelapse already started");

            this.cancellationTokenSource = new CancellationTokenSource(this.Duration);
            _ = this.TakeTimelapseAsync(this.cancellationTokenSource.Token)
                    .ContinueWith(OnTimelapseEnded, null);
        }

        private void OnTimelapseEnded(Task task, object _)
        {
            if (this.OnFinished != null)
                _ = this.OnFinished(this);
        }

        async Task TakeTimelapseAsync(CancellationToken cts)
        {
            while (!cts.IsCancellationRequested)
            {
                var timelapsePhoto = await TakeTimelapsePhotoAsync();

                if (OnTimelapsePhotoTaken != null)
                    await OnTimelapsePhotoTaken(this, timelapsePhoto);

                await Task.Delay(this.Interval);
            }
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }

        protected abstract Task<TakePhotoResponse> TakeTimelapsePhotoAsync();
    }

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