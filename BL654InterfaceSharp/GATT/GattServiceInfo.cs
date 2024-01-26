using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GATT
{
    public class GattServiceInfo
    {
        public int Handle
        {
            get;
        }

        public uint UUID
        {
            get;
        }

        public GattServiceInfo(int handle, uint uuidHandleFromFw)
        {
            this.Handle = handle;
            this.UUID = uuidHandleFromFw;
        }


        public List<GattCharacteristicInfo>   Characteristcs
        {
            get; 
        } = new List<GattCharacteristicInfo>();

    }
}
