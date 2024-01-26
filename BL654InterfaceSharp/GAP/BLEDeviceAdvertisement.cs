using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{
    public class BLEDeviceAdvertisement
    {
        public uint Handle
        {
            get;
        }

        public ulong DeviceAddress
        {
            get;
        }

        public int RSSI
        {
            get;
        }

        public String DeviceName
        {
            get;
        }

        public BLEDeviceAdvertisement(uint handle, ulong deviceAddress, int rssi, string deviceName)
        {
            Handle = handle;
            DeviceAddress = deviceAddress;
            RSSI = rssi;
            DeviceName = deviceName;
        }


    }
}
