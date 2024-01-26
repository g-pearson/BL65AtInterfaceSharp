using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface.Utils
{
    public static class HexUtils
    {
        private static byte HexCharToStr(char c)
        {
            switch (c)
            {
                default: // Invalid characters will be interpreted as 0
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'A':
                case 'a':
                    return 10;
                case 'B':
                case 'b':
                    return 11;
                case 'C':
                case 'c':
                    return 12;
                case 'D':
                case 'd':
                    return 13;
                case 'E':
                case 'e':
                    return 14;
                case 'F':
                case 'f':
                    return 15;
            }
        }

        static readonly char[] stringToHexLookup = new char[16] { '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F' }; 


        /// <summary>
        /// Parse a hex string (with no spaces) into a byte array.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] ParseByteString(String str)
        {
            str = str.Trim();

            if (str.Length / 2 * 2 != str.Length)
            {
                throw new ArgumentException("Invalid byte string: number of characters must be a multiple of two.");
            }

            byte[] data = new byte[str.Length / 2];

            for (int i = 0; i < str.Length / 2; i++)
            {
                //Small optimization so we don't have to substring every character pair
                data[i] = (byte)((HexCharToStr(str[i * 2]) << 4) | HexCharToStr(str[i * 2 + 1]));
            }

            return data;
        }

        public static String ByteArrayToString(byte[] data, int offset = 0, int length = -1)
        {
            StringBuilder stringBuilder = new StringBuilder();
            ByteArrayToString(stringBuilder, data, offset, length);
            return stringBuilder.ToString();
        }

        public static void ByteArrayToString(StringBuilder sb, byte[] data, int offset=0, int length=-1)
        {
            if (length == -1)
            {
                length = data.Length - offset;
            }

            for (int i = 0; i < length; i++)
            {
                sb.Append(stringToHexLookup[data[offset + i] >> 4]);
                sb.Append(stringToHexLookup[data[offset + i] & 0xF]);
            }
        }

    }
}
