using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class StopTimelapseApiRequest : IRequest<StopTimelapseResponse>
    {  
    }

    public class StopTimelapseApiRequestHandler : IRequestHandler<StopTimelapseApiRequest, StopTimelapseResponse>
    {
        private readonly ICamera camera;

        public StopTimelapseApiRequestHandler(ICamera camera)
        {
            this.camera = camera;
        }

        public Task<StopTimelapseResponse> Handle(StopTimelapseApiRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.camera.StopTimelapse(new StopTimelapseRequest()));
        }
    }
}