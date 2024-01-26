using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BL654Interface;
using BL654Interface.GAP;
using BL654Interface.GATT;
using BL654Interface.UART;
using BL654Interface.Utils;

using static BL654Interface.SRegisters;

namespace BL654Interface
{
    public delegate void BleDisconnectedHandler(Object sender, DisconnectedEventArgs e);
    public delegate void NotificationReceivedHandler(Object sender, NotificationEventArgs e);

    public class BL654ATInterface : IDisposable
    {
        /// <summary>
        /// Fired when an advertisement message is received from the BL654. 
        /// </summary>
        public event BleAdvertisementHandler AdvertisementReceived;

        /// <summary>
        /// Fired when a remote device is disconnected. If handle is -1, ALL remote devices should be considered disconnected.
        /// </summary>
        public event BleDisconnectedHandler DeviceDisconnected;

        /// <summary>
        /// Fired when the interface is disconnected, serial port is closed, or read thread has terminated. 
        /// </summary>
        public event EventHandler InterfaceDisconnected;

        /// <summary>
        /// Fired when a notification is received from a subscribed characteristic
        /// </summary>
        public event NotificationReceivedHandler NotificationReceived;

        Thread readThread;

        public ISerialPort Port
        {
            get;
        }

        /// <summary>
        /// Enables or disables logging raw serial traffic to the module.
        /// </summary>
        public bool DebugTxRxLogging
        {
            get; set;
        }

        /// <summary>
        /// The Category parameter passed to all Debug.Write* calls.
        /// </summary>
        public String TraceCategory
        {
            get; set;
        } = "BL654Interface";


        /// <summary>
        /// Serial port must be initialized at the correct baud rate and opened prior to calling StartReadThread().
        /// Default settings for the BL654 are:
        /// 115200 Baud
        /// 1 stop bit
        /// 8 data bits
        /// Hardware flow control ENABLED
        /// Ensure the DTR line is de-asserted and BREAK state is OFF.
        /// It may be required to pull DTR low and then toggle BREAK to ensure the module does not enter VSP mode.
        /// </summary>
        /// <param name="port"></param>
        public BL654ATInterface(ISerialPort port)
        {
            Port = port;
        }

        public void StartReadThread()
        {
            readThread = new Thread(ReadThreadProc);
            readThread.IsBackground = true;
            readThread.Name = "BL654 Read Thread";
            readThread.Start(Port);
        }

        /// <summary>
        /// Write a message to the serial port (and a copy to the terminal for debugging)
        /// </summary>
        /// <param name="str"></param>
        void PortWrite(String str)
        {
            if (DebugTxRxLogging)
            {
                Debug.WriteLine($"TX: {str}", TraceCategory);
            }
            Port.WriteLine(str);
        }

        /// <summary>
        /// Info about connected devices
        /// </summary>
        /// <typeparam name="String"></typeparam>
        /// <param name=""></param>
        /// <returns></returns>
        private readonly Dictionary<int,SessionInfo> sessionInfo = new Dictionary<int,SessionInfo>();

        /// <summary>
        /// Each line read from the BL654 module will be added to this collection for synchronous command handling.
        /// </summary>
        BlockingCollection<String> readLines = new BlockingCollection<String>(1024);

        /// <summary>
        /// Flush the read buffer to ensure any leftover messages from previous commands aren't erroneously read for our command.
        /// </summary>
        void ClearRxBuffer()
        {
            while (readLines.TryTake(out _, 0))
            {
            }
        }

        /// <summary>
        /// There are some asynchronous / unsolicited messages sent under some circumstances so we need a way to read these in without an explicit read call.
        /// </summary>
        /// <param name="userState"></param>
        void ReadThreadProc(Object userState)
        {
            ISerialPort port = (ISerialPort)userState;

            DisconnectedReasonCode dcCode = DisconnectedReasonCode.SW_READ_THREAD_STOPPED;

            try
            {
                //Infinite timeout
                port.ReadTimeout = -1;

                while (true)
                {
                    String line = port.ReadLine();
                    line = line.TrimStart('\n');
                    if (DebugTxRxLogging)
                    {
                        Debug.WriteLine("RX: " + line, TraceCategory);
                    }

                    //Advertisements
                    if (line.StartsWith("AD0:") || line.StartsWith("AD1:") || line.StartsWith("AD2:") || line.StartsWith("ADE:") || line.StartsWith("ADS:"))
                    {
                        //Advertisement!!
                        var args = line.Substring(4)
                            .Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);

                        if (args.Length == 4)
                        {
                            uint handle = uint.Parse(args[0]);
                            ulong uuid = ulong.Parse(args[1], System.Globalization.NumberStyles.HexNumber);
                            int rssi = int.Parse(args[2]);
                            String deviceName = args[3].Trim('\"');

                            if (AdvertisementReceived is BleAdvertisementHandler handler)
                            {
                                handler(this, new AdvertisementEventArgs(new BLEDeviceAdvertisement(handle, uuid, rssi, deviceName)));
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"{nameof(BL654ATInterface)}: Invalid advertisement received.", TraceCategory);
                        }


                    }
                    else if (line.StartsWith("discon ")) //Device disconnected or failed to connect
                    {
                        //Parse disconnection message
                        var parts = line.Substring("discon ".Length)
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length != 2)
                        {
                            Debug.WriteLine("Received invalid discon message", TraceCategory);
                        }


                        int handle = int.Parse(parts[0]);
                        lock (sessionInfo)
                        {
                            sessionInfo.Remove(handle);
                        }

                        DisconnectedReasonCode reason = (DisconnectedReasonCode)int.Parse(parts[1]);

                        DeviceDisconnected?.Invoke(this, new DisconnectedEventArgs(handle, reason));

                    }
                    else if (line.StartsWith("IN:")) //Notification or indication read (the BL654 appears to read indication values for us)
                    {
                        
                        var parts = line.Substring("IN:".Length)
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length != 3)
                        {
                            Debug.WriteLine("Received invalid notification message.", TraceCategory);
                        }

                        int deviceHandle = int.Parse(parts[0]);
                        int characteristicHandle = int.Parse(parts[1]);
                        byte[] data = HexUtils.ParseByteString(parts[2]);

                        NotificationReceived?.Invoke(this, new NotificationEventArgs(deviceHandle, characteristicHandle, data));
                    }
                    else if (line.StartsWith("MT:")) // Line contains information about negotiated MTU. Fired on connection and when we negotiate.
                    {
                        var parts = line
                        .Substring("MT:".Length)
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        int handle = Int32.Parse(parts[0]);
                        int mtu = Int32.Parse(parts[1]);

                        lock (sessionInfo)
                        {
                            if (!sessionInfo.ContainsKey(handle))
                            {
                                sessionInfo[handle] = new SessionInfo();
                            }

                            sessionInfo[handle].MTU = mtu;
                        }
                    }    

                    if (!readLines.TryAdd(line))
                    {
                        Debug.WriteLine($"{nameof(BL654ATInterface)}: Warning, dropped line on read thread.", TraceCategory);
                    }
                }
            }
            catch (IOException x)
            {
                //Fired when someone disconnects the USB dongle!
                Debug.WriteLine($"IOException in BL654ATInterface read thread, assuming dongle disconnected: {x}", TraceCategory);
                dcCode = DisconnectedReasonCode.SW_DONGLE_DISCONNECTED;
            }
            catch (Exception x)
            {
                Debug.WriteLine($"Unhandled Exception in BL654ATInterface read thread: {x}", TraceCategory);
            }
            finally
            {
                Debug.WriteLine("Read thread exited", TraceCategory);
                
                //Let any subscribers know that their device has been effectively disconnected
                //(-1 for handle indicates all devices)
                DeviceDisconnected?.Invoke(this, new DisconnectedEventArgs(-1, dcCode));

                //Mark interface as disconnected since the read thread has stopped.
                InterfaceDisconnected?.Invoke(this, EventArgs.Empty);
                
            }
        }
       

        /// <summary>
        /// Read incoming text until the caller-supplied delegate returns true.
        /// </summary>
        /// <param name="d">The search delegate that will determine eligibility of evaluated strings.</param>
        /// <param name="timeout">Milliseconds to wait for a match</param>
        /// <returns>The matched string or null if no match is found within timeout.</returns>
        private String StringScan(SearchDelegate d, int timeout = 1000)
        {
            DateTime start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < TimeSpan.FromMilliseconds(timeout))
            {
                if (readLines.TryTake(out String line, timeout))
                {
                    if (d(line))
                    {
                        return line;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Scan for an OK message (or an ERROR response, which throws a BL65Exception)
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="BL65Exception"></exception>
        private bool OKScan(int timeout=1000)
        {
            return StringScan((s) => 
            {
                if (s.StartsWith("ERROR "))
                {
                    var code = ParseError(s);
                    throw new BL65Exception(code, $"ErrorCode: {code.ToString()}");
                }
                else
                {
                    return s == "OK";
                }
            
            }, timeout) != null;
        }

        /// <summary>
        /// Parse a raw "ERROR N" line into an error code
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static BL65ErrorCode ParseError(String line)
        {
            if (line.StartsWith("ERROR "))
            {
                //WARNING: I may have the error codes a little messed up here, I think some error codes are printed in decimal and others in hex
                if (int.TryParse(line.Substring("ERROR ".Length), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int errorVal))
                {
                    return (BL65ErrorCode)errorVal;
                }
            }

            throw new FormatException($"Invalid error string: \"{line}\"");
        }

        /// <summary>
        /// Create a managed characteristic for the specified characteristic handle. 
        /// A managed characteristic will not be created if the specified ID's cannot be found within the services collection.
        /// </summary>
        /// <param name="deviceHandle"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="charaUuid"></param>
        /// <returns>A ManagedCharacteristic or null if a service or characteristic could not be found.</returns>
        public ManagedCharacteristic FindKnownCharacteristic(GattServiceInfo[] services, int deviceHandle, ulong serviceUuid, ulong charaUuid)
        {
            var service = services.SingleOrDefault(s => s.UUID == serviceUuid);
            if (service == null)
            {
                return null;
            }
            var chara = service?.Characteristcs.SingleOrDefault(c => c.UUID == charaUuid);
            if (chara == null)
            {
                return null;
            }

            return new ManagedCharacteristic(service, deviceHandle, chara, this);
        }

        /// <summary>
        /// Returns the negotiated MTU for the connection or -1 if no MTU has been recorded yet.
        /// NOTE: I've found that the INITIALLY reported MTU isn't always correct. Despite receiving an MT:1,244 message, I had to call NegotiateMTU() before I could actually write characteristics at that size successfully. 
        /// </summary>
        /// <param name="connectionHandle"></param>
        /// <returns></returns>
        public int GetMTU(int connectionHandle)
        {
            lock (sessionInfo)
            {
                if (sessionInfo.TryGetValue(connectionHandle, out var info))
                {
                    return info.MTU;
                }
                else
                {
                    return -1;
                }
            }
        }


        /// <summary>
        /// Check to see if device responds to basic AT command (only returns OK)
        /// </summary>
        /// <returns></returns>
        public bool AT()
        {
            PortWrite("AT");
            return OKScan(250);
        }

        /// <summary>
        /// Begin scanning for advertisements/devices. 
        /// All parameters are optional. Any non supplied parameters will use default S-Register values.
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <param name="pattern"></param>
        /// <param name="minRssi"></param>
        /// <param name="scanType"></param>
        /// <returns></returns>
        public bool PerformScan(int timeoutSeconds, String pattern = null, int? minRssi = null, ScanType? scanType = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("AT+LSCN ");

            //TIMEOUT
            sb.Append(timeoutSeconds);

            sb.Append(", ");

#warning this is untested, docs for how pattern works are not part of LSCN command 
            //PATTERN
            if (pattern != null)
            {
                sb.Append(pattern);
            }

            sb.Append(", ");

            //MIN RSSI
            if (minRssi != null)
            {
                sb.Append(minRssi.Value);
            }

            sb.Append(", ");

            if (scanType != null)
            {
                //SCAN TYPE
                sb.Append((int)scanType.Value);
            }

            //END

            ClearRxBuffer();
            PortWrite(sb.ToString());
            DateTime scanStart = DateTime.UtcNow;


            //SCAN FOR OK
            
            //We can receive an OK in the middle of scanning, before advertisements have completed, so we need to wait until the device starts responding again before sending anything
            bool success = OKScan(timeoutSeconds * 1000 + 250);
            if (success)
            {
                while (DateTime.UtcNow - scanStart < TimeSpan.FromSeconds(timeoutSeconds + 5))
                {
                    ClearRxBuffer();
                    if (AT())
                    {
                        break;
                    }
                }
            }

            return success;
        }



        /// <summary>
        /// Terminate scanning before scan timeout has expired.
        /// </summary>
        /// <returns></returns>
        public bool EndScanning()
        {
            PortWrite("AT+LSCNX");
            return OKScan(300);
        }

        public void DisconnectDevice(int devicehandle, bool waitForResponse=true)
        {
            ClearRxBuffer();
            PortWrite($"AT+LDSC {devicehandle}");
            if (waitForResponse)
            {
                OKScan();
            }
        }

        /// <summary>
        /// Connect to a device with the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="FormatException">Fired if the module doesn't return an expected response.</exception>
        /// <exception cref="BL65Exception">Fired if an ERROR response is returned</exception>
        /// <exception cref="BL654DeviceDisconnectedException">Fired if initial connection fails</exception>
        public ConnectionInfo Connect(ulong address)
        {
            ClearRxBuffer();

            //BL654 ATInterface docs state that address must contain 14 characters
            PortWrite($"AT+LCON {address:X14}");

            String line = StringScan((s) => { return s.StartsWith("connect") || s.StartsWith("ERROR") || s.StartsWith("discon 0,"); }, 1000 * 5);

            if (line != null)
            {
                if (line.StartsWith("connect"))
                {
                    var parts = line
                        .Substring("connect".Length)
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 5)
                    {
                        throw new FormatException("Invalid connection string response!");
                    }

                    int handle = int.Parse(parts[0]);
                    ulong connectedAddress = ulong.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);
                    long intervalMicroSeconds = long.Parse(parts[2]);
                    long supervisionTimeoutMicroseconds = long.Parse(parts[3]);
                    long latency = long.Parse(parts[4]);

                    ConnectionInfo info = new ConnectionInfo(handle, connectedAddress, intervalMicroSeconds, supervisionTimeoutMicroseconds, latency);
                    

                    return info;

                }
                else if (line.StartsWith("ERROR"))
                {
                    //Parse error code
                    if (int.TryParse(line.Substring("ERROR".Length).Trim(), out int errorCode))
                    {
                        BL65ErrorCode error = (BL65ErrorCode)errorCode;
                        throw new BL65Exception(error);
                    }
                    else
                    {
                        throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, $"Unknown Error: {line}");
                    }
                }
                else if (line.StartsWith("discon 0,"))
                {
                    //Failed to connect!
                    if (int.TryParse(line.Substring("discon 0,".Length).Trim(), out int errorCode))
                    {
                        DisconnectedReasonCode reasonCode = (DisconnectedReasonCode)errorCode;
                        throw new BL654DeviceDisconnectedException(reasonCode, $"Failed to connect to device: {reasonCode.ToString()}");
                    }
                    else
                    {
                        throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, $"Unknown Error: {line}");
                    }

                }
            }
            
            throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "No Response");
        }

        /// <summary>
        /// Sends the disconnect command to the AT654 module which will in turn drop the connection to the specified handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool Disconnect(int handle)
        {
            PortWrite($"AT+LDSC {handle}");
            return OKScan();
        }


        /// <summary>
        /// Re-negotiate the MTU of the connection. May only be called once.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="BL65Exception"></exception>
        public int NegotiateMTU(int handle)
        {

            ClearRxBuffer();
            PortWrite($"AT+LMTU {handle}");

            if (OKScan())
            {
                String expected = $"MT:{handle},";
                String line = StringScan((s) => { return s.StartsWith(expected); });

                int mtu = int.Parse(line.Substring(expected.Length));

                return mtu;
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "No response while negotiating MTU.");
            }
        }
       

        /// <summary>
        /// Read and parse gatt services/characteristics table
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="BL65Exception"></exception>
        public GattServiceInfo[] GetDeviceServices(int handle, int lineTimeoutMs=1500)
        {
            GattTableParser parser = new GattTableParser();

            ClearRxBuffer();
            PortWrite($"AT+GCTM {handle}");

            while (!parser.CompletedOK)
            {
                if (readLines.TryTake(out var line, lineTimeoutMs))
                {
                    if (line.StartsWith("ERROR "))
                    {
                        throw new BL65Exception(ParseError(line), "Unable to retrieve GATT table.");
                    }

                    parser.ParseLine(line);
                }
                else
                {
                    //ERROR
                    throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "No response or incomplete response while reading GATT table");
                }
            }

            if (parser.CompletedOK)
            {
                return parser.GattServices.ToArray();
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Failed to parse GATT table");
            }
        }

        
        /// <summary>
        /// Enables or disables notifications on the specified characteristic.
        /// </summary>
        /// <param name="deviceHandle"></param>
        /// <param name="descriptorHandle"></param>
        /// <param name="enabled"></param>
        /// <exception cref="BL65Exception"></exception>
        public void EnableNotifications(int deviceHandle, int descriptorHandle, bool enabled=true)
        {
            //Read the CCCD to determine current value
            byte[] cccdValue = ReadCharacteristic(deviceHandle, descriptorHandle);

            if (enabled)
            {
                //Enable notifications (bit 1)
                cccdValue[0] |= 01;
            }
            else
            {
                cccdValue[0] &= 0xFE;
            }


            ClearRxBuffer();
            PortWrite($"AT+GCWC {deviceHandle}, {descriptorHandle}, {HexUtils.ByteArrayToString(cccdValue)}");
            if (!OKScan())
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }
        }

        /// <summary>
        /// Enables or disables indications on the specified characteristic.
        /// </summary>
        /// <param name="deviceHandle"></param>
        /// <param name="descriptorHandle"></param>
        /// <param name="enabled"></param>
        /// <exception cref="BL65Exception"></exception>
        public void EnableIndications(int deviceHandle, int descriptorHandle, bool enabled = true)
        {
            //Read the CCCD to determine current value
            byte[] cccdValue = ReadCharacteristic(deviceHandle, descriptorHandle);

            if (enabled)
            {
                //Enable indications (bit 2)
                cccdValue[0] |= 02;
            }
            else
            {
                cccdValue[0] &= 0xFD;
            }

            ClearRxBuffer();
            PortWrite($"AT+GCWC {deviceHandle}, {descriptorHandle}, {HexUtils.ByteArrayToString(cccdValue)}");
            if (!OKScan())
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }
        }

        public void WriteWithoutResponse(int deviceHandle, int characteristicHandle, byte[] data, int offset, int count, int timeout)
        {
            ClearRxBuffer();
            StringBuilder sb = new StringBuilder();
            sb.Append("AT+GCWC ");
            sb.Append(deviceHandle);
            sb.Append(", ");
            sb.Append(characteristicHandle);
            sb.Append(", ");
            HexUtils.ByteArrayToString(sb, data, offset, count);

            PortWrite(sb.ToString());
            if (!OKScan(timeout))
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }
        }

        public void WriteWithResponse(int deviceHandle, int characteristicHandle, byte[] data, int offset, int count, int timeout)
        {
            ClearRxBuffer();
            StringBuilder sb = new StringBuilder();
            sb.Append("AT+GCWA ");
            sb.Append(deviceHandle);
            sb.Append(", ");
            sb.Append(characteristicHandle);
            sb.Append(", ");
            HexUtils.ByteArrayToString(sb, data, offset, count);

            PortWrite(sb.ToString());
            
            //Wait for synchronous OK response
            if (!OKScan(timeout))
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }

            //Wait for asynchronous acknowledgement response
            var response = StringScan((s) => 
            {
                return s.StartsWith("AW:");
            }, timeout);

            if (response == null)
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "Wrote data but did not receive acknowledgement from remote.");
            }

            var parts = response
                       .Substring(3)
                       .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


            int handle = int.Parse(parts[0]);

            BL65ErrorCode code = (BL65ErrorCode)int.Parse(parts[1]);
            if (code != BL65ErrorCode.OK)
            {
                throw new BL65Exception(code, "Error while writing characteristic");
            }
        }

        public byte[] ReadCharacteristic(int deviceHandle, int characteristicHandle, int offset=0, int timeout=500)
        {
            ClearRxBuffer();
            PortWrite($"AT+GCRD {deviceHandle}, {characteristicHandle}, {offset}");

            if (OKScan(timeout))
            {
                String line = StringScan((s) => 
                {
                    return s.StartsWith("AR:") || s.StartsWith("AS:") || s.StartsWith("AB:");
                }, timeout);

                if (line == null)
                {
                    throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "Invalid characteristic read,  no response.");
                }

                var parts = line
                       .Substring(3)
                       .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


                int handle = int.Parse(parts[0]);

                if (line.StartsWith("AR:")) // SUCCESS
                {
                    offset = int.Parse(parts[1]);
                    byte[] data = HexUtils.ParseByteString(parts[2]);
                    return data;
                }
                else if (line.StartsWith("AS:")) // ERROR
                {
                    BL65ErrorCode code = (BL65ErrorCode)int.Parse(parts[1]);
                    throw new BL65Exception(code, "Error while reading characteristic");
                }
                else if (line.StartsWith("AB:")) // Out of memory
                {
                    throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Module out of memory / insuficient memory to perform read.");
                }
                else
                {
                    throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Invalid state");
                }

            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }

        }


        /// <summary>
        /// Get values from an S-Register
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <returns></returns>
        public int ReadSRegister(int registerNumber)
        {
            ClearRxBuffer();
            PortWrite($"ATS {registerNumber}?");

            //Wait for a numeric response
            String line = StringScan((l) =>
            {
                if (!String.IsNullOrWhiteSpace(l))
                {
                    if (l.StartsWith("ERROR "))
                    {
                        throw new BL65Exception(ParseError(l), "Read S-Register error");
                    }

                    if (uint.TryParse(l, out _))
                    {
                        return true;
                    }
                }
                return false;
            });

            if (line != null)
            {
                int val = int.Parse(line);
                //We need an OK afterwards to confirm this was the right response
                if (OKScan())
                {
                    return val;
                }
                else
                {
                    throw new BL65Exception(BL65ErrorCode.UNKNOWN_ERROR, "Never received expected OK response after ATS");
                }
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }

        }

        /// <summary>
        /// Set an S-Register value
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="value"></param>
        /// <exception cref="BL65Exception"></exception>
        public void WriteSRegister(int registerNumber, uint value)
        {
            ClearRxBuffer();
            PortWrite($"ATS {registerNumber}={value}");
            if (OKScan())
            {
                //OK
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE);
            }
        }

        /// <summary>
        /// Set an S-Register value
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="value"></param>
        /// <exception cref="BL65Exception"></exception>
        public void WriteSRegister(int registerNumber, int value)
        {
            ClearRxBuffer();
            PortWrite($"ATS {registerNumber}={value}");
            if (OKScan())
            {
                //OK
            }
            else
            {
                throw new BL65Exception(BL65ErrorCode.NO_KNOWN_RESPONSE, "No response while writing S-Register");
            }
        }

        public void WriteStartupFlags(StartupFlags flags)
        {
            WriteSRegister(100, (byte)flags);
        }

        public StartupFlags ReadStartupFlags()
        {
            return (StartupFlags)ReadSRegister(100);
        }

        public void SetConnectionTimeout(int seconds)
        {
            WriteSRegister(110, (uint)seconds);
        }

        public int GetConnectionTimeout()
        {
            return (int)ReadSRegister(110);
        }

        public void SetActiveScanType(bool active)
        {
            WriteSRegister(112, active ? 1u : 0u);
        }

        public bool GetActiveScanType()
        {
            int val = ReadSRegister(112);
            return val != 0;
        }

        public void SetMinScanRSSI(int minRSSI)
        {
            WriteSRegister(113, minRSSI);
        }

        public int GetMinScanRSSI()
        {
            return ReadSRegister(113);
        }


        public void SetMaxConnections(int maxConnections)
        {
            if (maxConnections > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConnections), "Must not be set higher than 16");
            }

            WriteSRegister(126, maxConnections);

        }

        public int GetMaxConnections()
        {
            return ReadSRegister(126);
        }

        public void SetLinkSupervisionTimeout(int milliseconds)
        {
            WriteSRegister(206, milliseconds);
        }

        public int GetLinkSupervisionTimeout()
        {
            return ReadSRegister(206);
        }

        /// <summary>
        /// Set max DLE size in s-register
        /// </summary>
        /// <param name="size"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetMaxDLESize(int size)
        {
            if (size < 20 || size > 244)
            {
                throw new ArgumentOutOfRangeException("Size must be between 20 and 244 bytes");
            }

            WriteSRegister(219, size);
        }

        public int GetDLESize()
        {
            return ReadSRegister(219);
        }

        public void SetConnectionInterval(int min_uS, int max_uS)
        {
            WriteSRegister(300, min_uS);
            WriteSRegister(301, max_uS);
        }

        public int GetMinConnectionIntervaluS()
        {
            return ReadSRegister(300);
        }

        public int GetMaxConnectionIntervaluS()
        {
            return ReadSRegister(301);
        }

        /// <summary>
        /// Set baud rate that will be needed when connecting to the module using UART (generally from a PC via FTDI adapter).
        /// Note that you'll need to save the S registers and then reset for this to take effect. 
        /// </summary>
        /// <param name="baudRate"></param>
        /// <returns></returns>
        public void SetUARTBaudRate(int baudRate)
        {
            WriteSRegister(302, baudRate);
        }

        public int GetUARTBaudRate()
        {
            return ReadSRegister(302);
        }

        public void SetMaxMsgsPerConnectionInterval(int maxMsgsPerConnectionInterval)
        {
            WriteSRegister(307, maxMsgsPerConnectionInterval);
        }

        public int GetMaxMsgsPerConnectionInterval()
        {
            return ReadSRegister(307);
        }

        /// <summary>
        /// Perform a warm reset of the BL654 module. Note that this causes all modules to disconnect.
        /// </summary>
        public void ResetModule()
        {
            try
            {
                PortWrite("ATZ");
                OKScan();
            }
            finally
            {
                //If module is rebooted nothing is connected
                DeviceDisconnected?.Invoke(this, new DisconnectedEventArgs(-1, DisconnectedReasonCode.CONN_ERROR_USER_DISCON));
            }
        }

        public void Dispose()
        {
            if (readThread != null)
            {
                readThread.Interrupt();
                readThread.Join(500);

                readLines.Dispose();
                readLines = null;
            }
        }
    }

}
