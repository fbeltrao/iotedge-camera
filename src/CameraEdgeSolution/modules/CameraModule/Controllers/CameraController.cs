using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CameraModule.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly ICamera camera;

        public CameraController(ILogger<CameraController> logger, ICamera camera)
        {
            this.logger = logger;
            this.camera = camera;
        }

        [Route("photos")]
        [HttpPost]
        public async Task<IActionResult> CreatePhotoAsync()
        {
            try
            {
                var response = await camera.TakePhotoAsync(new TakePhotoRequest());
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
        public async Task<IActionResult> GetPhotoImage(string image)
        {
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

