using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BL654Interface.GAP;

namespace BL654Interface.GATT
{
    /// <summary>
    /// A characterstic linked to the supporting interface, services, and device handle which can be called directly to more cleanly perform characterstic operations.
    /// </summary>
    public class ManagedCharacteristic : IDisposable
    {
        /// <summary>
        /// Fired whenever an indication or notification is received for this characteristic.
        /// </summary>
        public event NotificationReceivedHandler OnNotificationOrIndicationReceived;

        /// <summary>
        /// Fired when the characteristic's associated device or interface is disconnected.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// The service the characteristic is associated to.
        /// </summary>
        public GattServiceInfo ParentService
        {
            get;
        }

        /// <summary>
        /// Raw characteristic info.
        /// </summary>
        public GattCharacteristicInfo CharacteristicInfo
        {
            get; 
        }

        /// <summary>
        /// Interface that the associated device is connected through.
        /// </summary>
        public BL654ATInterface IFace
        {
            get;
        }

        /// <summary>
        /// BL654 handle to the associated device.
        /// </summary>
        public int DeviceHandle
        {
            get;
        }

        /// <summary>
        /// Indicates if the associated remote BLE device is connected to the interface.
        /// </summary>
        public bool DeviceConnected
        {
            get;private set;
        }

        public ManagedCharacteristic(GattServiceInfo parentService, int deviceHandle, GattCharacteristicInfo info, BL654ATInterface iFace)
        {
            ParentService = parentService;
            this.CharacteristicInfo = info;
            IFace = iFace;
            this.DeviceHandle = deviceHandle;
            IFace.NotificationReceived += IFace_NotificationReceived;
            IFace.DeviceDisconnected += IFace_DeviceDisconnected;
            IFace.InterfaceDisconnected += IFace_InterfaceDisconnected;
            DeviceConnected = true;
        }

        private void IFace_InterfaceDisconnected(object sender, EventArgs e)
        {
            DeviceConnected = false;
            Disconnected?.Invoke(this, e);
        }

        private void IFace_DeviceDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.Handle == this.DeviceHandle || e.Handle == -1)
            {
                DeviceConnected = false;
                Disconnected?.Invoke(this, e);
            }
        }

        private void IFace_NotificationReceived(object sender, BL654Interface.GATT.NotificationEventArgs e)
        {
            if(e.DeviceHandle == DeviceHandle && e.CharacteristicHandle == CharacteristicInfo.Handle && DeviceConnected)
            {
                if (OnNotificationOrIndicationReceived is NotificationReceivedHandler handler)
                {
                    handler(this, e);
                }
            }
        }

        public void WriteWithoutResponse(byte[] data, int offset, int length, int timeout)
        {
            if (!DeviceConnected)
            {
                throw new DeviceDisconnectedException("Device no longer connected.");
            }

            if (!CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.WriteWithoutResponse))
            {
                throw new InvalidOperationException("Characteristic does not support Write Without Response");
            }

            IFace.WriteWithoutResponse(DeviceHandle, CharacteristicInfo.Handle, data, offset, length, timeout);
        }

        public void WriteWithResponse(byte[] data, int offset, int length, int timeout)
        {
            if (!DeviceConnected)
            {
                throw new DeviceDisconnectedException("Device no longer connected.");
            }

            if (!CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.WriteWithResponse))
            {
                throw new InvalidOperationException("Characteristic does not support Write With Response");
            }

            IFace.WriteWithResponse(DeviceHandle, CharacteristicInfo.Handle, data, offset, length, timeout);
        }

        public byte[] Read(int offset=0, int timeout=1000)
        {
            if (!DeviceConnected)
            {
                throw new DeviceDisconnectedException("Device no longer connected.");
            }

            if (!CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.Read))
            {
                throw new InvalidOperationException("Characteristic does not support Read");
            }

            return IFace.ReadCharacteristic(DeviceHandle, CharacteristicInfo.Handle, offset, timeout);
        }

        public void EnableIndications()
        {
            if (!DeviceConnected)
            {
                throw new DeviceDisconnectedException("Device no longer connected.");
            }

            if (CharacteristicInfo.CCCD != null)
            {
                IFace.WriteWithResponse(DeviceHandle, CharacteristicInfo.CCCD.Handle, new byte[2] { 02, 00 }, 0, 2, 1000);
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Characteristic does not have a descriptor to enable indications upon.");
            }
        }

        public void EnableNotifications()
        {
            if (!DeviceConnected)
            {
                throw new DeviceDisconnectedException("Device no longer connected.");
            }

            if (CharacteristicInfo.CCCD != null)
            {
                IFace.WriteWithResponse(DeviceHandle, CharacteristicInfo.CCCD.Handle, new byte[2] { 01, 00 }, 0, 2, 1000);
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Characteristic does not have a descriptor to enable notifications upon.");
            }
        }

        public void Dispose()
        {
            IFace.NotificationReceived -= this.IFace_NotificationReceived;
            IFace.DeviceDisconnected -= IFace_DeviceDisconnected;
            IFace.InterfaceDisconnected -= IFace_InterfaceDisconnected;
            DeviceConnected = false;
        }
    }
}
