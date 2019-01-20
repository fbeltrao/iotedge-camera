using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class GetCameraStatusApiRequest : IRequest<GetCameraStatusApiResponse>
    {

    }

    public class GetCameraStatusApiResponse
    {
        [JsonProperty("isTakingTimelapse")]
        public bool IsTakingTimelapse { get; set; }        

        [JsonProperty("photos")]
        public IReadOnlyList<string> Photos { get; set; }

        [JsonProperty("timelapses")]
        public IReadOnlyList<string> Timelapses { get; set; }
    }


    public class GetCameraStatusApiRequestHandler : IRequestHandler<GetCameraStatusApiRequest, GetCameraStatusApiResponse>
    {
        private readonly ICamera camera;
        private readonly CameraConfiguration configuration;
        private readonly ILogger logger;

        public GetCameraStatusApiRequestHandler(ICamera camera, CameraConfiguration configuration, ILogger<GetCameraStatusApiRequestHandler> logger)
        {
            this.camera = camera;
            this.configuration = configuration;
            this.logger = logger;
        }
        public Task<GetCameraStatusApiResponse> Handle(GetCameraStatusApiRequest request, CancellationToken cancellationToken)
        {
            var cameraStatus = this.camera.GetCameraStatus();
            return Task.FromResult(new GetCameraStatusApiResponse() {
                IsTakingTimelapse = cameraStatus.IsTakingTimelapse,
                Photos = GetFileList(this.configuration.EnsureOutputDirectoryExists(Constants.PhotosSubFolderName)),
                Timelapses = GetFileList(this.configuration.EnsureOutputDirectoryExists(Constants.TimelapsesSubFolderName)),
            });
        }

        IReadOnlyList<string> GetFileList(string directory)
        {
            var fileList = new List<string>();
            foreach (var file in Directory.GetFiles(directory))
                fileList.Add(item: Path.GetFileName(file));

            // sort descending
            fileList.Sort((t1, t2) => t2.CompareTo(t1));

            return fileList;
        }
    }
}