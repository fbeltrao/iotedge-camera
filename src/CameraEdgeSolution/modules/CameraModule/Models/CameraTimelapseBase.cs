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
            _ = this.TakeTimelapseAsync(this.cancellationTokenSource.Token);
        }

        async Task TakeTimelapseAsync(CancellationToken cts)
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var timelapsePhoto = await TakeTimelapsePhotoAsync();

                    if (OnTimelapsePhotoTaken != null)
                    {
                        Logger.Log("Will call OnTimelapsePhotoTaken");
                        await OnTimelapsePhotoTaken(this, timelapsePhoto);
                    }

                    await Task.Delay(this.Interval);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during timelapse capture");
            }
            finally
            {
                await this.OnFinished?.Invoke(this);
            }
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }

        protected abstract Task<TakePhotoResponse> TakeTimelapsePhotoAsync();
    }
}