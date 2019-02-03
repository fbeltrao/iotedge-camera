using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class StopTimelapseApiRequestHandler : IRequestHandler<StopTimelapseApiRequest, StopTimelapseResponse>
    {
        private readonly ICamera camera;
        private readonly IMediator mediator;

        public StopTimelapseApiRequestHandler(ICamera camera, IMediator mediator)
        {
            this.camera = camera;
            this.mediator = mediator;
        }

        public async Task<StopTimelapseResponse> Handle(StopTimelapseApiRequest request, CancellationToken cancellationToken)
        {
            var timelapseRespone = await this.camera.StopTimelapseAsync(new StopTimelapseRequest());
            if (timelapseRespone.Succeded)
            {
                await this.mediator.Publish(new TimelapseTakenNotification()
                {
                    Timelapse = timelapseRespone.Id
                });
            }

            return timelapseRespone;
        }
    }
}