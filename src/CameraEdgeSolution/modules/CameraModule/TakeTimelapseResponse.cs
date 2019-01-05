using System;

namespace CameraModule
{
    public class TakeTimelapseResponse
    {
        public string Id { get; set; }
        public TimeSpan Duration { get; set; }
        public int Interval { get; set; }
    }
}
