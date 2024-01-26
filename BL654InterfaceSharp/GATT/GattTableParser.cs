using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using static BL654Interface.GATT.GattCharacteristicInfo;

namespace BL654Interface.GATT
{
    public class GattTableParser
    {
        const string LINE_START = "TM:";
        const string LINE_OK = "OK";

        public LinkedList<GattServiceInfo> GattServices
        {
            get;
        } = new LinkedList<GattServiceInfo>();

        public int Line
        {
            get; private set;
        } = 0;

        public bool CompletedOK
        {
            get;private set;
        }

        public void Reset()
        {
            GattServices.Clear();
            Line = 0;
            CompletedOK = false;
        }


        public void ParseLine(String line, int index=0)
        {
            Line++;


            if (CompletedOK)
            {
                throw new InvalidOperationException("Parser already received OK signal, call Reset() before attempting to parse another GATT table.");
            }

            if(line.StartsWith(LINE_OK))
            {
                CompletedOK=true;
                return;
            }

            //Find TM
            while(!FindIn(LINE_START, line, index) && index < line.Length)
            {
                index++;
            }

            index += LINE_START.Length;

            //Find and parse line type
            while (index < line.Length)
            {
                if (line[index] == 'S')
                {
                    //Line type is SERVICE
                    index++;
                    //Expect :
                    if (line[index] != ':')
                    {
                        throw new FormatException($"Unexpected character at index {index}. Expected :, found {line[index]}.");
                    }

                    index++;

                    //Split!
                    var parts = line.Substring(index).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    int handle = Int32.Parse(parts[0]);
                    
                    //Ignore ID of last item

                    uint uuid = UInt32.Parse(parts[2], System.Globalization.NumberStyles.HexNumber);

                   

                    GattServices.AddLast(new GattServiceInfo(handle, uuid));
                    break;

                }
                else if (line[index] == 'C')
                {
                    //line type is CHARACTERISTIC
                    index++;
                    //Expect :
                    if (line[index] != ':')
                    {
                        throw new FormatException($"Line {Line}: Unexpected character at index {index}. Expected :, found {line[index]}.");
                    }

                    index++;

                    //Split!
                    var parts = line.Substring(index).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 4)
                    {
                        throw new FormatException($"Line {Line}: Expected 4 parameters in characteristic descriptor, found {parts.Length}");
                    }


                    int handle = Int32.Parse(parts[0]);

                    CharacteristicCapability characteristicProperties = (CharacteristicCapability)ushort.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);

                    uint uuid = UInt32.Parse(parts[2], System.Globalization.NumberStyles.HexNumber);

                    GattCharacteristicInfo chara = new GattCharacteristicInfo(handle, characteristicProperties, uuid);

                    if (GattServices.Last == null)
                    {
                        throw new FormatException($"Line {Line}: Attempted to parse a GATT characteristic without a parent service being parsed first.");
                    }

                    GattServices.Last.Value.Characteristcs.Add(chara);
                    break;

                }
                else if (line[index] == 'D')
                {
                    //Line type is DESCRIPTOR
                    index++;
                    //Expect :
                    if (line[index] != ':')
                    {
                        throw new FormatException($"Line {Line}: Unexpected character at index {index}. Expected :, found {line[index]}.");
                    }

                    index++;

                    //Split!
                    var parts = line.Substring(index).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                    {
                        throw new FormatException($"Line {Line}: Expected 2 parameters in characteristic descriptor, found {parts.Length}");
                    }

                    int handle = Int32.Parse(parts[0]);
                    uint uuid = UInt32.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);


                    if (GattServices.Last == null)
                    {
                        throw new FormatException($"Line {Line}: Attempted to parse a GATT characteristic descriptor without a parent service being parsed first.");
                    }

                    if (GattServices.Last.Value.Characteristcs.Last() == null)
                    {
                        throw new FormatException($"Line {Line}: Attempted to parse a GATT characteristic descriptor without a parent characteristic being parsed first.");
                    }

                    GattServices.Last.Value.Characteristcs.Last().CCCD = new GattCCCDInfo(handle, uuid);

                    break;
                }

                index++;
            }

        }

        private static bool FindIn(String find, String within, int startIdx)
        {
            //Ensure the string we're searching for isn't longer than the string we're searching within
            if(find.Length + startIdx > find.Length)
            {
                return false;
            }

            for (int i = 0; i < find.Length; i++)
            {
                if (find[i] != within[i + startIdx])
                {
                    return false;
                }
            }

            return true;
        }





    }
}
