using System.IO;
using MediatR;

namespace CameraModule.Models
{
    public class GetPhotoApiRequest : IRequest<Stream>
    {
        public string Photo { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

    }
}