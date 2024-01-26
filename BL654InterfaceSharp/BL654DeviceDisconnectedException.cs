using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using BL654Interface.GAP;

namespace BL654Interface
{
    public class BL654DeviceDisconnectedException : BL65Exception
    {

        public DisconnectedReasonCode DisconnectedReasonCode
        {
            get; 
        }

        public BL654DeviceDisconnectedException(DisconnectedReasonCode code) : base(BL65ErrorCode.CONNECTION_FAILED)
        {
            this.DisconnectedReasonCode = code;
        }

        public BL654DeviceDisconnectedException(DisconnectedReasonCode code, string message) : base(BL65ErrorCode.CONNECTION_FAILED, message)
        {
            this.DisconnectedReasonCode = code;
        }

        public BL654DeviceDisconnectedException(DisconnectedReasonCode code, string message, Exception innerException) : base(BL65ErrorCode.CONNECTION_FAILED, message, innerException)
        {
            this.DisconnectedReasonCode = code;
        }
    }
}
