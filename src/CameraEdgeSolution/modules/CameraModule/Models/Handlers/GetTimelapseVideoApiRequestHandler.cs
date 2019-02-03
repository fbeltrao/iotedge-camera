using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CameraModule.Models
{
    public class GetTimelapseVideoApiRequestHandler : IRequestHandler<GetTimelapseVideoApiRequest, GetTimelapseVideoApiResponse>
    {
        private readonly CameraConfiguration configuration;
        private readonly IMediator mediator;

        public GetTimelapseVideoApiRequestHandler(CameraConfiguration configuration, IMediator mediator)
        {
            this.configuration = configuration;
            this.mediator = mediator;
        }

        public async Task<GetTimelapseVideoApiResponse> Handle(GetTimelapseVideoApiRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var path = configuration.EnsureOutputDirectoryExists(Path.Combine(Constants.TimelapsesSubFolderName, request.Timelapse));
                var targetFilePath = Path.Combine(request.Timelapse, string.Concat(request.Timelapse, ".mp4"));
                
                if (File.Exists(targetFilePath))
                {
                    return new GetTimelapseVideoApiResponse()
                    {
                        Stream = File.OpenRead(targetFilePath),
                        Timelapse = request.Timelapse,
                        Format = "mp4",
                    };
                }

                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = "ffmpeg"
                    }
                };

                const string extension = ".jpeg";
                var fps = 2;
                var args = $"-framerate {fps} -f image2 -pattern_type glob -y -i {path + "/*." + extension} -c:v libx264 -pix_fmt yuv420p {targetFilePath}";
                Logger.Log($"Starting ffmpeg with args: {args}");
                process.StartInfo.Arguments = args;
                process.Start();
                process.WaitForExit();

                Logger.Log($"Timelapse video for {request.Timelapse} created");
                if (configuration.HasStorageInformation())
                {
                    await IOUtils.UploadFileAsync(Constants.TimelapsesSubFolderName, targetFilePath, this.configuration);
                }

                return new GetTimelapseVideoApiResponse()
                {
                    Stream = File.OpenRead(targetFilePath),
                    Timelapse = request.Timelapse,
                    Format = "mp4",
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create timelapse video");
                return new GetTimelapseVideoApiResponse()
                {
                    Timelapse = request.Timelapse,
                    ErrorMessage = "Failed to create timelapse video",
                };
            }
        }
    }
}