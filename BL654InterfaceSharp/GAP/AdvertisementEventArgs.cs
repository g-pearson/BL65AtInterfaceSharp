using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{
    public delegate void BleAdvertisementHandler(Object sender, AdvertisementEventArgs e);

    public class AdvertisementEventArgs : EventArgs
    {
        public BLEDeviceAdvertisement Advertisement
        {
            get;
        }

        public AdvertisementEventArgs(BLEDeviceAdvertisement advertisement)
        {
            this.Advertisement = advertisement;
        }

    }
}
