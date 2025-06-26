using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Workbench.Models.Enums;

namespace Workbench.Models.BootLoader
{
    public struct HexData
    {
        public int Address;
        public byte[] Data;
        public UInt16 Crc;
        public int index;
        public UInt16 Len;
    }

    public class HexDataContainer
    {
        private string stx = "\u0002";
        private string etx = "\u0003";
        public List<HexData> HexDatas = new List<HexData>();
        private List<byte> byteBuffer = new List<byte>();
        private int address = -1;
        private UInt16 crc = 0;
        private UInt16 len = 0;
        public UpdateTopoEnum UpgradeFileTopo = UpdateTopoEnum.None;

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
                bool special = false;
                var line = sr.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("$18fff,"))
                    {
                        special = true;
                        line = sr.ReadLine();
                        continue;
                    }
                    if (special)
                    {
                        special = false;
                        SpecialDataParser(line);
                        line = sr.ReadLine();
                        continue;
                    }

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
            var hexData = new HexData();
            hexData.Address = address;
            hexData.Data = new byte[byteBuffer.Count];
            Array.Copy(byteBuffer.ToArray(), 0, hexData.Data, 0, hexData.Data.Length);
            hexData.Crc = crc;
            hexData.Len = len;
            HexDatas.Add(hexData);
            byteBuffer.Clear();
        }

        private void SpecialDataParser(string line)
        {
            switch (line)
            {
                case "FF FE 00 00 86 CA 31 00 01 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.移相全桥;
                    break;
                case "FF FE 00 00 86 CA 32 00 02 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.LLC;
                    break;
                case "FF FE 00 00 86 CA 33 00 03 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.BuckBoost;
                    break;
                case "FF FE 00 00 86 CA 34 00 04 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.DAB;
                    break;
                case "FF FE 00 00 86 CA 35 00 05 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.单相逆变整流;
                    break;
                case "FF FE 00 00 86 CA 36 00 06 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.三相逆变整流;
                    break;
                case "FF FE 00 00 86 CA 37 00 07 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.维也纳整流;
                    break;
                case "FF FE 00 00 86 CA 38 00 08 A5 0A EF FF":
                    UpgradeFileTopo = UpdateTopoEnum.LC;
                    break;
                default:
                    UpgradeFileTopo = UpdateTopoEnum.None;
                    break;
            }
        }

    }
}
