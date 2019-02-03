using System.Collections.Generic;
using MediatR;

namespace CameraModule.Models
{
    public class GetPhotosApiRequest : IRequest<IReadOnlyList<string>>
    {
    }
}