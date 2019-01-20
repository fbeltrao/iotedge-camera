using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CameraModule.Models
{
    public static class CameraApplicationExtensions
    {
        public static void UseCamera(this IApplicationBuilder app)
        {
            var camera = app.ApplicationServices.GetService<ICamera>();
            if (camera == null)
            {
                throw new InvalidOperationException("Unable to find the ICamera");
            }

            camera.Initialize();
        }
    }
}
