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

        public StartTimelapseApiRequestHandler(ICamera camera, CameraConfiguration configuration, IMediator mediator)
        {
            this.camera = camera;
            this.configuration = configuration;
            this.mediator = mediator;
        }

        public async Task<TakeTimelapseResponse> Handle(StartTimelapseApiRequest request, CancellationToken cancellationToken)
        {
            var response = await camera.StartTimelapseAsync(new TakeTimelapseRequest()
            {
                OutputDirectory = configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName),
            });

            if (response.Succeded)
            {
                await mediator.Publish(new TimelapseStartedNotification()
                {
                    Timelapse = response.Id,
                });
            }
          

            return response;
        }
    }
}