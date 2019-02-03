using MediatR;

namespace CameraModule.Models
{
    public class TakePhotoApiRequest : IRequest<TakePhotoResponse>
    {
    }
}