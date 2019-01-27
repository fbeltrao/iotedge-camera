using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class VideoFromTimelapseApiRequest : IRequest<VideoFromTimelapseApiResponse>
    {
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }

    public class VideoFromTimelapseApiResponse
    {   
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }

    public class VideoFromTimelapseApiRequestHandler : IRequestHandler<VideoFromTimelapseApiRequest, VideoFromTimelapseApiResponse>
    {
        private readonly CameraConfiguration configuration;
        private readonly IMediator mediator;

        public VideoFromTimelapseApiRequestHandler(CameraConfiguration configuration, IMediator mediator)
        {
            this.configuration = configuration;
            this.mediator = mediator;
        }

        public async Task<VideoFromTimelapseApiResponse> Handle(VideoFromTimelapseApiRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = "ffmpeg"
                    }
                };

                var extension = ".jpeg";

                var path = configuration.EnsureOutputDirectoryExists(Path.Combine(Constants.TimelapsesSubFolderName, request.Timelapse));
                var targetFilePath = Path.Combine(request.Timelapse, string.Concat(request.Timelapse, ".mp4"));
                

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

                // clean up temporary folder
                //IOUtils.TryRemoveFolder(imgCaptureHandler.ProcessedFiles.First().Directory);

                // notify
                await this.mediator.Publish(new TimelapseVideoNotification()
                {
                    Timelapse = request.Timelapse,
                    Format = "mp4",
                });

                return new VideoFromTimelapseApiResponse()
                {
                    Timelapse = request.Timelapse,
                    Format = "mp4",
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create timelapse video");
                return new VideoFromTimelapseApiResponse()
                {
                    Timelapse = request.Timelapse,
                    ErrorMessage = "Failed to create timelapse video",
                };
            }
        }
    }
}