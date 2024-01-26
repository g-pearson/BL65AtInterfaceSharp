using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GATT
{
    /// <summary>
    /// Fired when an indication occurs on a characteristic
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public byte[] Data
        {
            get;
        }

        public int DeviceHandle
        {
            get;
        } 

        public int CharacteristicHandle
        {
            get;
        } 

        public NotificationEventArgs(int deviceHandle, int characteristicHandle, byte[] data)
        {
            Data = data;
            DeviceHandle = deviceHandle;
            CharacteristicHandle = characteristicHandle;
        }
    }
}
