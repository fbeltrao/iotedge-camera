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
        private readonly IHubContext<CameraHub> cameraHub;
        private readonly ICamera camera;

        public CameraController(ILogger<CameraController> logger, IHubContext<CameraHub> cameraHub, ICamera camera)
        {
            this.logger = logger;
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
               using (var actualImage = Image.Load(await this.camera.GetImageStreamAsync(image)))
                {
                    actualImage.Mutate(x => x
                        .Resize(safeWidth, safeHeight)
                        );

                    var memStream = new MemoryStream();
                    actualImage.SaveAsJpeg(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);
                    return File(memStream, "image/jpeg", image);
                }
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

