using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class GetTimelapsesApiRequest : IRequest<IReadOnlyList<string>>
    {
        
    }

    public class GetTimelapsesApiRequestHandler : IRequestHandler<GetTimelapsesApiRequest, IReadOnlyList<string>>
    {
        private readonly CameraConfiguration configuration;

        public GetTimelapsesApiRequestHandler(CameraConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<IReadOnlyList<string>> Handle(GetTimelapsesApiRequest request, CancellationToken cancellationToken)
        {
            var path = this.configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName);

            return Task.FromResult(GetFileList(path));
        }

        IReadOnlyList<string> GetFileList(string directory)
        {
            var fileList = new List<string>();
            foreach (var file in Directory.GetFiles(directory))
                fileList.Add(Path.GetFileName(file));

            // sort descending
            fileList.Sort((t1, t2) => t2.CompareTo(t1));

            return fileList;
        }
    }
}