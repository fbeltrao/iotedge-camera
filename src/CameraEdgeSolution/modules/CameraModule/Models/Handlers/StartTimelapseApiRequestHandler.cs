using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class StartTimelapseApiRequestHandler : IRequestHandler<StartTimelapseApiRequest, TakeTimelapseResponse>
    {
        private readonly ICamera camera;
        private readonly CameraConfiguration configuration;
        private readonly IMediator mediator;
        private CameraTimelapseBase timelapse;

        public StartTimelapseApiRequestHandler(ICamera camera, CameraConfiguration configuration, IMediator mediator)
        {
            this.camera = camera;
            this.configuration = configuration;
            this.mediator = mediator;
        }

        public async Task<TakeTimelapseResponse> Handle(StartTimelapseApiRequest request, CancellationToken cancellationToken)
        {
            try
            {
                this.timelapse = await camera.CreateTimelapseAsync(new TakeTimelapseRequest()
                {
                    OutputDirectory = configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName),
                });

                var localMediator = this.mediator;
                timelapse.OnFinished = async (t) => {
                    Logger.Log("OnFinished called");
                    await localMediator.Publish(new TimelapseTakenNotification()
                    {  
                        Timelapse = t.ID,
                    });
                };           
                
                timelapse.OnTimelapsePhotoTaken = async (tm, photo) => {
                    Logger.Log("OnTimelapsePhotoTaken called");
                    await localMediator.Publish(new PhotoTakenNotification()
                    {
                        Details = photo,
                        IsTimelapsePhoto = true,
                        Timelapse = tm.ID,
                    });
                };

                await mediator.Publish(new TimelapseStartedNotification()
                {
                    Timelapse = timelapse.ID,
                });
                
                timelapse.Start();

                return new TakeTimelapseResponse()
                {
                    Duration = (int)timelapse.Duration.TotalSeconds,
                    Interval = (int)timelapse.Interval.TotalSeconds,
                    Id = timelapse.ID,
                };
            }
            catch (TimelapseInProgressException)
            {
                return new TakeTimelapseResponse()
                {
                    ErrorMessage = "Timelapse already in progress",
                    IsTakingTimelapse = true,
                };
            }
        }
    }
}