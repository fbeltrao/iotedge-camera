using System.IO;
using MediatR;

namespace CameraModule.Models
{
    public class GetTimelapsePhotoApiRequest : IRequest<Stream>
    {
        public string Timelapse { get; set; }
        public string Photo { get; set; }
    }
}