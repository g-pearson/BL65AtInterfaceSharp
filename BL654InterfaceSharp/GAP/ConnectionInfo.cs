using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{
    public class ConnectionInfo
    {
        public int Handle
        {
            get;
        }

        public ulong Address
        {
            get;
        }

        public long IntervalMicroseconds
        {
            get;
        }

        public long SupervisionTimeoutMicroseconds
        {
            get;
        }

        public long Latency
        {
            get;
        }

        public BLEDeviceAdvertisement ConnectedFromAdvertisement
        {
            get; set;
        }

        public ConnectionInfo(int handle, ulong address, long intervalMicroseconds, long supervisionTimeoutMicroseconds, long latency)
        {
            Handle = handle;
            Address = address;
            IntervalMicroseconds = intervalMicroseconds;
            SupervisionTimeoutMicroseconds = supervisionTimeoutMicroseconds;
            Latency = latency;
        }

    }
}
