using System;
using System.Runtime.Serialization;

namespace CameraModule.Models
{
    public class TimelapseInProgressException : Exception
    {
        public TimelapseInProgressException()
        {
        }

        public TimelapseInProgressException(string message) : base(message)
        {
        }

        public TimelapseInProgressException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TimelapseInProgressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}