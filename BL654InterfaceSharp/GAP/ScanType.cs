using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{
    [Flags]
    public enum ScanType
    {
        PrimaryAdverts1MPHY = 1,
        PrimaryAdvertsLECODED = 2,

        /// <summary>
        /// If PrimaryAdverts1MPHY is set, this will be enabled as well.
        /// </summary>
        ExtendedScanningSecondaryChannels = 4,

        /// <summary>
        /// Active scanning if 0, passive scanning if 1
        /// </summary>
        PassiveScanning = 8,
    }
}
