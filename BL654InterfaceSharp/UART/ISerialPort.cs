using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.UART
{
    public interface ISerialPort
    {
        int ReadTimeout
        {
            get;set;
        }

        int WriteTimeout
        {
            get; set; 
        }


        String ReadLine();
        void WriteLine(String line);

        bool Break
        {
            get;
            set;
        }

        bool DTR
        {
            get;set;
        }

    }
}
