using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

using BL654Interface.GAP;
using BL654Interface.GATT;
using BL654Interface.UART;

namespace BL654Interface.Examples
{
    internal class BasicConsole
    {
        BL654ATInterface iface;
        readonly Dictionary<int, ConnectionInfo> connections = new Dictionary<int, ConnectionInfo>();
      
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Serial port name is required.");
                WriteHelp();
                return;
            }

            String comPort = args[0];

            int baudRate = 115200;
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out baudRate))
                {
                    Console.WriteLine("Invalid baud rate specified.");
                    WriteHelp();
                    return;
                }
            }

            ConcreteSerialPort port = ConcreteSerialPort.DefaultSerialPortConfig(comPort);
            port.Port.BaudRate = baudRate;
            port.Port.Open();

            //Always reset module
            Console.WriteLine("Resetting module...");
            //Reset the module on connect
            port.Port.BreakState = true;
            Thread.Sleep(100);
            port.Port.BreakState = false;
            Thread.Sleep(100);

            BasicConsole p = new BasicConsole();
            p.iface = new BL654ATInterface(port);

            p.iface.AdvertisementReceived += Iface_AdvertisementReceived;
            p.iface.DeviceDisconnected += p.Iface_DeviceDisconnected;
            p.iface.StartReadThread();

            Console.WriteLine("Testing AT...");
            if (p.iface.AT())
            {
                Console.WriteLine("Successful response!");
            }
            else
            {
                Console.WriteLine("Failure; no response.");
            }

            List<BLEDeviceAdvertisement> devices = null;
            while (true)
            {

                ConsoleKeyInfo info = Console.ReadKey();
                switch (info.Key)
                {
                    case ConsoleKey.C: // CONNECT
                        {
                            int idx = p.ConnectLoop(devices);

                            if (idx != -1)
                            {
                                try
                                {
                                    var connInfo = p.iface.Connect(devices[idx].DeviceAddress);
                                    lock (p.connections)
                                    {
                                        connInfo.ConnectedFromAdvertisement = devices[idx];
                                        p.connections[connInfo.Handle] = connInfo;
                                    }
                                    Console.WriteLine($"Successfully connected to {devices[idx].DeviceAddress:X14}, Handle: {connInfo.Handle}");

                                    var services = p.iface.GetDeviceServices(connInfo.Handle);
                                    Console.WriteLine("Services:");
                                    PrintServices(services);
                                }
                                catch (BL65Exception x)
                                {
                                    Console.WriteLine($"Failed to connect: {x.Code} : \"{x.Message}\"");
                                }
                                catch (Exception x)
                                {
                                    Console.WriteLine(x);
                                }
                            }

                            break;
                        }
                    case ConsoleKey.S: // SCAN
                        {
                            devices = p.Scan();
                            break;
                        }
                    case ConsoleKey.X: // eXit
                        {
                            return;
                        }
                }
            }
        }

        private static void WriteHelp()
        {
            String assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            Console.WriteLine($"{assemblyName} <COMPORT> [BAUD RATE]");
            Console.WriteLine();
            Console.WriteLine("Available Ports:");
            foreach (var port in SerialPort.GetPortNames())
            {
                Console.WriteLine(port);
            }
        }

        private static void PrintServices(IEnumerable<GattServiceInfo> services)
        {
            foreach (var svc in services)
            {
                Console.WriteLine($"Service: {svc.Handle}\t{svc.UUID:X}");
                foreach (var c in svc.Characteristcs)
                {
                    Console.WriteLine($"\tCharacteristic: {c.Handle}\t{c.UUID:X}\t{c.Capabilities}");

                    if (c.CCCD != null)
                    {
                        Console.WriteLine($"\t\tDescriptor: {c.CCCD.Handle}\t{c.CCCD.UUID}");
                    }
                }
            }
        }

        private void Iface_DeviceDisconnected(object sender, DisconnectedEventArgs e)
        {
            lock (connections)
            {
                if (connections.TryGetValue(e.Handle, out var info))
                {

                    Console.WriteLine($"Disconnected: {e.Handle}:{e.Reason} ({info.Address}, \"{info.ConnectedFromAdvertisement?.DeviceName}\")");
                    connections.Remove(e.Handle);
                }

            }
        }

        private int ConnectLoop(List<BLEDeviceAdvertisement> devices)
        {
            if (devices == null)
            {
                Console.WriteLine("No devices to connect to. Use S to scan first.");
                return -1;
            }

            Console.WriteLine("INDEX\t\tADDRESS\t\t\tRSSI\t\tNAME");

            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i}\t\t{devices[i].DeviceAddress:X16}\t{devices[i].RSSI}\t\t{devices[i].DeviceName}");
            }

            while (true)
            {
                Console.WriteLine("Enter index of device to connect");
                String line = Console.ReadLine();

                if (line == null)
                {
                    continue;
                }

                if (Int32.TryParse(line, out int result))
                {
                    if (result >= 0 && result < devices.Count)
                    {
                        //CONNECT
                        return result;
                    }
                    else
                    {
                        Console.WriteLine("Result out of range");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid key; Abort connection.");
                    return -1;
                }

            }

        }



        public List<BLEDeviceAdvertisement> Scan()
        {
            Dictionary<ulong, BLEDeviceAdvertisement> devices = new Dictionary<ulong, BLEDeviceAdvertisement>();
            BleAdvertisementHandler handler = (o, args) =>
            {
                lock (devices)
                {
                    devices[args.Advertisement.DeviceAddress] = args.Advertisement;
                }
            };

            try
            {
                iface.AdvertisementReceived += handler;
                Console.WriteLine("Scanning...");
                iface.PerformScan(2);
                Thread.Sleep(3000);
            }
            finally
            {
                iface.AdvertisementReceived -= handler;
            }

            lock (devices)
            {
                return devices.Values.OrderByDescending(d => d.RSSI).ToList();
            }
        }



        private static void Iface_AdvertisementReceived(object sender, AdvertisementEventArgs e)
        {
            Console.WriteLine($"{e.Advertisement.DeviceName}: {e.Advertisement.DeviceAddress} ({e.Advertisement.RSSI})");
        }
    }
}