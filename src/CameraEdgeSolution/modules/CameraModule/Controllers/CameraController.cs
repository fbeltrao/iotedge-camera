using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CameraModule.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly CameraConfiguration configuration;
        private readonly IHubContext<CameraHub> cameraHub;
        private readonly ICamera camera;

        public CameraController(ILogger<CameraController> logger, CameraConfiguration configuration, IHubContext<CameraHub> cameraHub, ICamera camera)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.cameraHub = cameraHub;
            this.camera = camera;
        }

        [Route("photos")]
        [HttpPost]
        public async Task<IActionResult> CreatePhotoAsync()
        {
            try
            {
                var response = await camera.TakePhotoAsync(new TakePhotoRequest());
                await this.cameraHub.Clients.All.SendCoreAsync("onnewphoto", new object[] { response });
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating photo");
                throw;
            }
        }

        [Route("photos/{image}")]
        [HttpGet]
        public async Task<IActionResult> GetPhotoImage(string image, int? width, int? height)
        {
            var safeWidth = width ?? 0;
            var safeHeight = height ?? 0;
            if (safeHeight > 0 && safeWidth > 0)
            {
                var thumbnailImageFileName = string.Concat(
                    Path.GetFileNameWithoutExtension(image),
                    $"-{safeWidth}x{safeHeight}",
                    Path.GetExtension(image));

                var thumbnailDirectory = Path.Combine(this.configuration.GetOuputDirectory(), "thumbnails/");
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

                await Utils.EnsureThumbnailExists(thumbnailFilePath, safeWidth, safeHeight, () => this.camera.GetImageStreamAsync(image));
               
                return new PhysicalFileResult(Path.GetFullPath(thumbnailFilePath), "image/jpeg");
                
            }
            
            return new FileStreamResult(await this.camera.GetImageStreamAsync(image), "image/jpeg");
        }

        [Route("photos")]
        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            return new JsonResult(await this.camera.GetImagesAsync());

        }
    }
}

