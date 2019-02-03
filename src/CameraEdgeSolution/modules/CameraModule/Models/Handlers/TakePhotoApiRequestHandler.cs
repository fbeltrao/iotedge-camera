using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class TakePhotoApiRequestHandler : IRequestHandler<TakePhotoApiRequest, TakePhotoResponse>
    {
        private readonly ICamera camera;
        private readonly IMediator mediator;
        private readonly CameraConfiguration configuration;

        public TakePhotoApiRequestHandler(ICamera camera, IMediator mediator, CameraConfiguration configuration)
        {
            this.camera = camera;
            this.mediator = mediator;
            this.configuration = configuration;
        }

        public async Task<TakePhotoResponse> Handle(TakePhotoApiRequest request, CancellationToken cancellationToken)
        {
            var photoRequest = new TakePhotoRequest()
            {
                OutputDirectory = configuration.EnsureOutputDirectoryExists(Constants.PhotosSubFolderName),
            };

            try
            {
                var response = await camera.TakePhotoAsync(photoRequest);
                if (response.Succeded)
                {
                    await this.mediator.Publish(new PhotoTakenNotification()
                    {
                        Details = response,
                    });
                }
                    
                return response;
            }
            catch (TimelapseInProgressException)
            {
                return new TakePhotoResponse()
                {
                    IsTakingTimelapse = true,
                };
            }
        }
    }
}