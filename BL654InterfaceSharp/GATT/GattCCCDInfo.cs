using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GATT
{
    public class GattCCCDInfo
    {

        public int Handle
        {
            get;
        }

        public uint UUID
        {
            get;
        }

        public GattCCCDInfo(int handle, uint uUID)
        {
            Handle = handle;
            UUID = uUID;
        }
    }
}
