using System;
using System.IO;
using System.Threading.Tasks;
using CameraModule.Models;
using MediatR;
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
        private readonly IMediator mediator;


        public CameraController(ILogger<CameraController> logger, IMediator mediator)
        {
            this.logger = logger;
            this.mediator = mediator;
        }

        public async Task<IActionResult> GetCameraStatus()
        {
            var response = await mediator.Send(new GetCameraStatusApiRequest());
            return this.Ok(response);
        }

        [Route("photos")]
        [HttpPost]
        public async Task<IActionResult> CreatePhotoAsync()
        {
            try
            {
                var response = await mediator.Send(new TakePhotoApiRequest());
                return response.Succeded ? 
                    (IActionResult)Ok(response) : BadRequest(response);
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
            var response = await this.mediator.Send(new GetPhotoApiRequest()
            {
                Photo = image,
                Width = width,
                Height = height,
            });

            if (response == null)
                return NotFound();

            return new FileStreamResult(response, "image/jpeg");
        }

        [Route("photos")]
        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            return Ok(await this.mediator.Send(new GetPhotosApiRequest()));
        }

        [Route("timelapses/start")]
        [HttpPost]
        public async Task<IActionResult> StartTimelapseAsync()
        {
            try
            {
                var response = await this.mediator.Send(new StartTimelapseApiRequest());
                return response.Succeded ?
                    (IActionResult)Ok(response) : new BadRequestObjectResult(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating timelapse");
                throw;
            }
        }


        [Route("timelapses/stop")]
        [HttpPost]
        public async Task<IActionResult> StopTimelapse()
        {
            try
            {
                var response = await this.mediator.Send(new StopTimelapseApiRequest());
                return response.Succeded ?
                    (IActionResult)Ok(response) : new BadRequestObjectResult(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating timelapse");
                throw;
            }
        }


        [Route("timelapses/{timelapse}")]
        [HttpGet]
        public async Task<IActionResult> GetTimelapseAsync(string timelapse)
        {
            try
            {
                var response = await this.mediator.Send(new GetTimelapseStreamApiRequest()
                {
                    Timelapse = timelapse,
                });
                
                if (response == null)
                    return NotFound();

                return new FileStreamResult(response, "video/mp4");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating timelapse");
                throw;
            }
        }
    }
}

