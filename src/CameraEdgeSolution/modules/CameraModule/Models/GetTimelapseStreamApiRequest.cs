using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class GetTimelapseStreamApiRequest : IRequest<Stream>
    {
        public string Timelapse { get; set; }

    }

    public class GetTimelapseStreamApiRequestHandler : IRequestHandler<GetTimelapseStreamApiRequest, Stream>
    {
        private readonly CameraConfiguration configuration;

        public GetTimelapseStreamApiRequestHandler(CameraConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<Stream> Handle(GetTimelapseStreamApiRequest request, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName), request.Timelapse);
            if (File.Exists(fullPath))
                return Task.FromResult((Stream)File.OpenRead(fullPath));
            
            return Task.FromResult((Stream)null);
        }
    }
}
