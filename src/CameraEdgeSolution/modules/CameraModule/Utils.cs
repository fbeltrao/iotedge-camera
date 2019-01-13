using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CameraModule
{
    public static class Utils
    {
        public static async Task EnsureThumbnailExists(string destinationFilename, int width, int height, Func<Task<Stream>> streamFactory)
        {
            if (File.Exists(destinationFilename))
                return;

            using (var actualImage = Image.Load(await streamFactory()))
            {
                actualImage.Mutate(x => x
                    .Resize(width, height)
                );

                
                using (var fs = File.OpenWrite(destinationFilename))
                {
                    actualImage.SaveAsJpeg(fs);
                }
            }
        }

    }
}