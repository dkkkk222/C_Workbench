using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.BootLoader
{
    public struct CANHexData
    {
        public int Address;
        public byte[] Data;
        public ushort Crc;
        public int index;
        public int Len;
    }
    public class CANHexDataContainer
    {
        private string stx = "\u0002";
        private string etx = "\u0003";
        public List<CANHexData> HexDatas = new List<CANHexData>();
        private List<byte> byteBuffer = new List<byte>();
        private int address = -1;
        private UInt16 crc = 0;
        private UInt16 len = 0;

        public void LoadHexFile(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                if (sr == null)
                    return;

                if (!stx.Equals(sr.ReadLine().Trim()))
                {
                    return;
                }
                var line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("$"))
                    {
                        RecordHexData();
                        AddressParser(line, out address, out crc, out len);
                    }
                    else if (etx.Equals(line.Trim()))
                    {
                        RecordHexData();
                        break;
                    }
                    else
                    {
                        DataLineParser(line);
                    }
                    line = sr.ReadLine();
                }
            }

        }

        private void DataLineParser(string line)
        {
            foreach (string hex in line.Trim().Split(' '))
            {
                byte v = Convert.ToByte(hex.Trim(), 16);
                byteBuffer.Add(v);
            }
        }

        private void AddressParser(string line, out int addr, out UInt16 crc, out UInt16 len)
        {
            try
            {
                var v = line.Substring(1, line.Length - 1);
                var vs = v.Trim().Split(',');
                crc = Convert.ToUInt16(vs[1], 16);
                addr = Convert.ToInt32(vs[0], 16);
                len = Convert.ToUInt16(vs[2], 16);
            }
            catch (Exception)
            {
                addr = -1;
                crc = 0;
                len = 0;
            }

        }

        private void RecordHexData()
        {
            if (address == -1)
            {
                return;
            }
            var hexData = new CANHexData();
            hexData.Address = address;
            hexData.Data = new byte[byteBuffer.Count];
            Array.Copy(byteBuffer.ToArray(), 0, hexData.Data, 0, hexData.Data.Length);
            hexData.Crc = crc;
            hexData.Len = len;
            HexDatas.Add(hexData);
            byteBuffer.Clear();
        }
    }
}
