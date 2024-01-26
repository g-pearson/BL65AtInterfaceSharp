using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BL654Interface.Examples;
using BL654Interface;
using BL654Interface.UART;
using BL654Interface.GAP;
using BL654Interface.GATT;
using BL654Interface.Utils;

namespace BL654InterfaceSharp.Examples
{
    internal class ReadWriteCharacteristic
    {
        //Replace these with your values. You can use the BasicConsole demo to enumerate services/characteristics and scan for your device to populate these fields.

        const ulong DEVICE_ADDRESS = 0x0;
        const uint SERVICE_UUID = 0x0;
        const uint CHARACTERISTIC_UUID = 0x0;

        const String COMM_PORT = "COM_YOUR_PORT_HERE";

        public static void Main(String[] args)
        {
            var port = ConcreteSerialPort.DefaultSerialPortConfig(COMM_PORT);
            port.Port.Open();

            //Always reset interface to a known state
            port.Port.BreakState = true;
            Thread.Sleep(100);
            port.Port.BreakState = false;
            Thread.Sleep(100);

            //Create a new interface to the BL65x
            var iface = new BL654ATInterface(port);

            //Do a basic test with the AT command to ensure the interface is correctly communicating
            if (!iface.AT())
            {
                Console.WriteLine("Interface is not responding!");
                return;
            }

            ConnectionInfo connectionInfo = null;

            //Connect to your BLE peripheral
            try
            {
                connectionInfo = iface.Connect(DEVICE_ADDRESS);
            }
            catch (BL654DeviceDisconnectedException x)
            {
                //Uhoh, failed to connect - is device powered up / address correct? Sometimes  you just have to try again (make sure you let your device boot up)
                Console.WriteLine(x.Message);
                return;
            }

            //Enumerate device services
            var services = iface.GetDeviceServices(connectionInfo.Handle);

            //Now let's see if we can find your characteristic...
            using (ManagedCharacteristic characteristic = iface.FindKnownCharacteristic(services, connectionInfo.Handle, SERVICE_UUID, CHARACTERISTIC_UUID))
            {

                if (characteristic == null)
                {
                    Console.WriteLine("Unable to find service or characteristic!");
                    return;
                }

                if (characteristic.CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.Notify))
                {
                    //It looks like your characteristic supports notifications, let's subscribe to them.
                    characteristic.OnNotificationOrIndicationReceived += Characteristic_OnNotificationOrIndicationReceived;
                }

                if (characteristic.CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.Read))
                {
                    //Read the characteristic value!
                    byte[] characteristicValue = characteristic.Read();
                    Console.WriteLine($"Read Characteristic: {HexUtils.ByteArrayToString(characteristicValue)}");
                }

                //Uncomment to write the characteristic. Make sure you get the length right and be sure that writing the characteristic won't cause any issues with your device!
                //if (characteristic.CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.WriteWithoutResponse))
                //{
                //    byte[] data = new byte[1] { 1 };
                //    characteristic.WriteWithoutResponse(data, 0, data.Length, 1000);
                //}    



            } // No need to unsubscribe from notifications - disposing the ManagedCharacteristic will remove the handler from the parent interface and the reference is from the characteristic to us.


        }

        private static void Characteristic_OnNotificationOrIndicationReceived(object sender, NotificationEventArgs e)
        {
            Console.WriteLine($"Notification received from {e.DeviceHandle} on characteristic {e.CharacteristicHandle}. Data: {HexUtils.ByteArrayToString(e.Data)}");
        }
    }
}
