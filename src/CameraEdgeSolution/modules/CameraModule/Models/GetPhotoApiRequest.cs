using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CameraModule.Models
{
    public class GetPhotoApiRequest : IRequest<Stream>
    {
        public string Photo { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

    }

    public class GetPhotoApiRequestHandler : IRequestHandler<GetPhotoApiRequest, Stream>
    {
        private readonly ICamera camera;
        private readonly CameraConfiguration configuration;
        private readonly ILogger logger;

        public GetPhotoApiRequestHandler(ICamera camera, CameraConfiguration configuration, ILogger<GetPhotoApiRequestHandler> logger)
        {
            this.camera = camera;
            this.configuration = configuration;
            this.logger = logger;
        }
        public async Task<Stream> Handle(GetPhotoApiRequest request, CancellationToken cancellationToken)
        {
            var originalFilePath = Path.Combine(this.configuration.EnsureOutputDirectoryExists(Constants.PhotosSubFolderName), request.Photo);

            var safeWidth = request.Width ?? 0;
            var safeHeight = request.Height ?? 0;
            if (safeHeight > 0 && safeWidth > 0)
            {
                var thumbnailImageFileName = string.Concat(
                    Path.GetFileNameWithoutExtension(request.Photo),
                    $"-{safeWidth}x{safeHeight}",
                    Path.GetExtension(request.Photo));

                var thumbnailDirectory = Path.Combine(this.configuration.EnsureOutputDirectoryExists(Constants.PhotosSubFolderName), "thumbnails/");
                if (!Directory.Exists(thumbnailDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(thumbnailDirectory);
                    }
                    catch
                    {
                        // might be created in parallel
                    }
                }
                var thumbnailFilePath = Path.Combine(thumbnailDirectory, thumbnailImageFileName);

                await EnsureThumbnailExists(thumbnailFilePath, safeWidth, safeHeight, () => Task.FromResult<Stream>(File.OpenRead(originalFilePath)));
               
                return File.OpenRead(Path.GetFullPath(thumbnailFilePath));
            }
            
            return File.OpenRead(originalFilePath);
        }


        async Task EnsureThumbnailExists(string destinationFilename, int width, int height, Func<Task<Stream>> streamFactory)
        {
            if (File.Exists(destinationFilename))
                return;

            try
            {
                using (var actualImage = Image.Load(await streamFactory()))
                {
                    actualImage.Mutate(x => x.Resize(width, height));
            
                    using (var fs = File.OpenWrite(destinationFilename))
                    {
                        actualImage.SaveAsJpeg(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: add retry logic
                this.logger.LogError(ex, "Failed to create image thumbnail");
            }
        }
    }
}