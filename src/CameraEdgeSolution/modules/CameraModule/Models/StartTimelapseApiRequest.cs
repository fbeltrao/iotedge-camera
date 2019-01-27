using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{

    public class StartTimelapseApiRequest : IRequest<TakeTimelapseResponse>
    {  
    }

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

                timelapse.OnFinished = OnTimelapseFinished;
                timelapse.OnTimelapsePhotoTaken = OnTimelapsePhotoTaken;

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

        private async Task OnTimelapsePhotoTaken(CameraTimelapseBase arg1, TakePhotoResponse arg2)
        {
            await mediator.Publish(new PhotoTakenNotification()
            {
                Details = arg2,
                IsTimelapsePhoto = true,
                Timelapse = this.timelapse.ID,
            });
        }

        private async Task OnTimelapseFinished(CameraTimelapseBase timelapse)
        {
            await mediator.Publish(new TimelapseTakenNotification()
            {
                Timelapse = timelapse.ID,
            });
        }
    }
}