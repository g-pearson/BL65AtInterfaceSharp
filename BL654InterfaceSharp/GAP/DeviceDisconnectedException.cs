using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{
    public class DeviceDisconnectedException : InvalidOperationException
    {
        public DeviceDisconnectedException()
        {
        }

        public DeviceDisconnectedException(string message) : base(message)
        {
        }

        public DeviceDisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceDisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
