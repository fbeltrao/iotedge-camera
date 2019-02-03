using System;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CameraModule.Models
{
    public class GetCameraStatusApiRequest : IRequest<GetCameraStatusApiResponse>
    {
    }
}