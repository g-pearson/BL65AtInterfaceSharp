using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.UART
{
    /// <summary>
    /// Adapts to a System.IO.Ports Serial Port
    /// </summary>
    public class ConcreteSerialPort : ISerialPort
    {

        /// <summary>
        /// Configures a serial port to communicate using the default BL65x connection settings. The port is NOT opened. 
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public static ConcreteSerialPort DefaultSerialPortConfig(String portName)
        {
            SerialPort port = new SerialPort(portName);
            port.BaudRate = 115200;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            port.DataBits = 8;
            port.NewLine = "\r"; // Very important to get the interface working!
            port.Handshake = Handshake.RequestToSend;
            return new ConcreteSerialPort(port);
        }



        public SerialPort Port
        {
            get;
        }

        public ConcreteSerialPort(SerialPort port)
        {
            this.Port = port;
            
        }

        public int ReadTimeout
        {
            get
            {
                return Port.ReadTimeout;
            }

            set
            {
                Port.ReadTimeout = value;
            }
        }

        public int WriteTimeout
        {
            get
            {
                return Port.WriteTimeout;
            }

            set
            {
                Port.WriteTimeout = value;
            }
        }

        public bool Break
        {
            get
            {
                return Port.BreakState;
            }

            set
            {
                Port.BreakState = value;
            }
        }

        public bool DTR
        {
            get
            {
                return Port.DtrEnable;
            }

            set
            {
                Port.DtrEnable = value;
            }
        }

        public string ReadLine()
        {
            return Port.ReadLine();
        }

        public void WriteLine(string line)
        {
            Port.WriteLine(line);
        }
    }
}
