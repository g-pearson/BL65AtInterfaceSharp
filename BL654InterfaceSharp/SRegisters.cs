using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface
{
    public class SRegisters
    {

        /// <summary>
        /// S-Register 100 flags
        /// </summary>
        [Flags]
        public enum StartupFlags : byte
        {
            VSPConnectable = 1,
            /// <summary>
            /// Ignored if bi t0 is 1 otherwise start advertising with no timeout
            /// </summary>
            StartAdvertisementImmediately = 2,

            /// <summary>
            ///  Ignored if bit 0 is 1 otherwise start scanning with no timeout
            /// </summary>
            StartScanningWithNoTimeout = 4,

            /// <summary>
            /// Set for max bidirectional throughput of about 127kbps, otherwise half that
            /// </summary>
            MaxBidirectionalThroughput = 8,

            /// <summary>
            ///  Use Data Length Extension (#define DLE_ATTRIBUTE_SIZE) in smartBASIC application
            /// </summary>
            UseDLEDefine = 16,

            PhyRate_1M = 0,
            PhyRate_LongRange = 32,
            PhyRate_RFU = 64,
            PhyRate_2M = 96,
        }





    }
}
