using System;
using MediatR;

namespace CameraModule.Models
{

    public class StartTimelapseApiRequest : IRequest<TakeTimelapseResponse>
    {  
    }
}