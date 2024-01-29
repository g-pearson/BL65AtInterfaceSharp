# BL65AtInterfaceSharp ![main workflow](https://github.com/g-pearson/BL65AtInterfaceSharp/actions/workflows/dotnet.yml/badge.svg)
A C# client for communicating with remote BLE devices using Laird's BL65x modules loaded with their AT Interface application.

For setup, follow the guide here: https://www.lairdconnect.com/documentation/user-guide-bl65x-interface-application , particularly, secion 2, "smartBASIC Application Loading Instructions". 

## Usage
### Initializing a Serial Port
```c#
            var port = ConcreteSerialPort.DefaultSerialPortConfig(COMM_PORT);
            port.Port.Open();

            //Always reset interface to a known state
            port.Port.BreakState = true;
            Thread.Sleep(100);
            port.Port.BreakState = false;
            Thread.Sleep(100);

            //Create a new interface to the BL65x
            var iface = new BL654ATInterface(port);
```

### Testing Communication

```c#
            //Do a basic test with the AT command to ensure the interface is correctly communicating
            if (!iface.AT())
            {
                Console.WriteLine("Interface is not responding!");
                return;
            }
```

### Connecting to a Peripheral

```c#
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
```

### Characteristic Operations
List services and characteristics, read and write characteristic values, subscribe to notifications and indications

```c#
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

               
                if (characteristic.CharacteristicInfo.Capabilities.HasFlag(GattCharacteristicInfo.CharacteristicCapability.WriteWithoutResponse))
                {
                    byte[] data = new byte[1] { 1 };
                    characteristic.WriteWithoutResponse(data, 0, data.Length, 1000);
                }    

            } 


        }

        private static void Characteristic_OnNotificationOrIndicationReceived(object sender, NotificationEventArgs e)
        {
            Console.WriteLine($"Notification received from {e.DeviceHandle} on characteristic {e.CharacteristicHandle}. Data: {HexUtils.ByteArrayToString(e.Data)}");
        }
```
