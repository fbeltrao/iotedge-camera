using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class GetTimelapsePhotoApiRequestHandler : IRequestHandler<GetTimelapsePhotoApiRequest, Stream>
    {
        private readonly CameraConfiguration configuration;

        public GetTimelapsePhotoApiRequestHandler(CameraConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Task<Stream> Handle(GetTimelapsePhotoApiRequest request, CancellationToken cancellationToken)
        {
            var originalFilePath = Path.Combine(this.configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName), request.Timelapse, request.Photo);
            
            return Task.FromResult<Stream>(File.OpenRead(originalFilePath));
        }
    }
}