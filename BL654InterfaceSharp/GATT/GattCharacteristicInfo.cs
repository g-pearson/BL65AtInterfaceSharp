using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.GATT
{
    /// <summary>
    /// Information about a scanned characterstic.
    /// Use for attaining handle and CCCD when communicating with a BLE peripheral. 
    /// </summary>
    public class GattCharacteristicInfo
    {
        [Flags]
        public enum CharacteristicCapability : ushort
        {
            BroadcastCapable = 1,
            Read = 2,
            WriteWithoutResponse = 4,
            WriteWithResponse = 8,
            Notify = 16,
            Indicate = 32,
        }


        public int Handle
        {
            get;
        }

        /// <summary>
        /// Capabilities of the characteristic (such as read, write, indicate, etc)
        /// </summary>
        public CharacteristicCapability Capabilities
        {
            get;
        }

        public uint UUID
        {
            get;
        }

        public GattCharacteristicInfo(int handle, CharacteristicCapability capabilities, uint uuid)
        {
            Handle = handle;
            Capabilities = capabilities;
            UUID = uuid;
        }

        /// <summary>
        /// Client Characteristic Configuration Descriptor for the characteristic, if present.
        /// </summary>
        public GattCCCDInfo CCCD
        {
            get;set;
        }


    }
}
