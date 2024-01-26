using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using BL654Interface.GATT;

namespace BL654Interface
{
    public class BL65Exception : Exception
    {
        public BL65ErrorCode Code
        {
            get;
        }

        public BL65Exception(BL65ErrorCode code)
        {
            this.Code = code;
        }

        public BL65Exception(string message) : base(message)
        {
        }

        public BL65Exception(BL65ErrorCode code, string message) : base(message)
        {
            this.Code = code;
        }

        public BL65Exception(BL65ErrorCode code, string message, Exception innerException) : base(message, innerException)
        {
            this.Code = code;
        }
        
    }
}
