using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GAP
{

    public enum DisconnectedReasonCode
    {
        CONN_OK                               =      0,
        /// <summary>
        /// Could not find this error in source but this is the code received when you power off your peripheral until the disconn message is sent.
        /// </summary>
        CONN_ERROR_SUPERVISION_TIMEOUT  = 8,
        CONN_ERROR_BLECONNECT           =            80,
        CONN_ERROR_INVALID_ADDRESS          =        81,
        CONN_ERROR_CMDPINSTATE              =        82,
        CONN_ERROR_TOOMANYCONNECTIONS        =       83,
        CONN_ERROR_TIMEOUT                  =        84,
        CONN_ERROR_OUTOFMEM                 =        85,
        CONN_ERROR_UNENCRYPTED            =          86,
        CONN_ERROR_NOVSPSERVICE             =        87,
        CONN_ERROR_PAIRUI                   =        88,
        CONN_ERROR_USER_DISCON              =        90,
        CONN_ERROR_AUTHLINK_REQUIRED        =        91,
        CONN_SUSPEND                       =         -1,


        //All errors below this point are software generated, NOT firmware error codes.
        SW_READ_THREAD_STOPPED = -100000,
        SW_DONGLE_DISCONNECTED = -100001,
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public int Handle
        {
            get;
        }

        public DisconnectedReasonCode Reason
        {
            get;
        }
        public DisconnectedEventArgs(int handle, DisconnectedReasonCode reason)
        {
            Handle = handle;
            Reason = reason;
        }


    }
}
