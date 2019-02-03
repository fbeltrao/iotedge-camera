using MediatR;
using Newtonsoft.Json;

namespace CameraModule.Models
{
    public class GetTimelapseVideoApiRequest : IRequest<GetTimelapseVideoApiResponse>
    {
        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }
}