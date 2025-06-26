using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant.Utils
{
    public static class StringHelper
    {
        public static IList<char> HexSet = new List<char>()
       { '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F','a','b','c','d','e','f' };
        private static readonly ushort[] CrcTable =
        {
            0X0000, 0XC0C1, 0XC181, 0X0140, 0XC301, 0X03C0, 0X0280, 0XC241,
            0XC601, 0X06C0, 0X0780, 0XC741, 0X0500, 0XC5C1, 0XC481, 0X0440,
            0XCC01, 0X0CC0, 0X0D80, 0XCD41, 0X0F00, 0XCFC1, 0XCE81, 0X0E40,
            0X0A00, 0XCAC1, 0XCB81, 0X0B40, 0XC901, 0X09C0, 0X0880, 0XC841,
            0XD801, 0X18C0, 0X1980, 0XD941, 0X1B00, 0XDBC1, 0XDA81, 0X1A40,
            0X1E00, 0XDEC1, 0XDF81, 0X1F40, 0XDD01, 0X1DC0, 0X1C80, 0XDC41,
            0X1400, 0XD4C1, 0XD581, 0X1540, 0XD701, 0X17C0, 0X1680, 0XD641,
            0XD201, 0X12C0, 0X1380, 0XD341, 0X1100, 0XD1C1, 0XD081, 0X1040,
            0XF001, 0X30C0, 0X3180, 0XF141, 0X3300, 0XF3C1, 0XF281, 0X3240,
            0X3600, 0XF6C1, 0XF781, 0X3740, 0XF501, 0X35C0, 0X3480, 0XF441,
            0X3C00, 0XFCC1, 0XFD81, 0X3D40, 0XFF01, 0X3FC0, 0X3E80, 0XFE41,
            0XFA01, 0X3AC0, 0X3B80, 0XFB41, 0X3900, 0XF9C1, 0XF881, 0X3840,
            0X2800, 0XE8C1, 0XE981, 0X2940, 0XEB01, 0X2BC0, 0X2A80, 0XEA41,
            0XEE01, 0X2EC0, 0X2F80, 0XEF41, 0X2D00, 0XEDC1, 0XEC81, 0X2C40,
            0XE401, 0X24C0, 0X2580, 0XE541, 0X2700, 0XE7C1, 0XE681, 0X2640,
            0X2200, 0XE2C1, 0XE381, 0X2340, 0XE101, 0X21C0, 0X2080, 0XE041,
            0XA001, 0X60C0, 0X6180, 0XA141, 0X6300, 0XA3C1, 0XA281, 0X6240,
            0X6600, 0XA6C1, 0XA781, 0X6740, 0XA501, 0X65C0, 0X6480, 0XA441,
            0X6C00, 0XACC1, 0XAD81, 0X6D40, 0XAF01, 0X6FC0, 0X6E80, 0XAE41,
            0XAA01, 0X6AC0, 0X6B80, 0XAB41, 0X6900, 0XA9C1, 0XA881, 0X6840,
            0X7800, 0XB8C1, 0XB981, 0X7940, 0XBB01, 0X7BC0, 0X7A80, 0XBA41,
            0XBE01, 0X7EC0, 0X7F80, 0XBF41, 0X7D00, 0XBDC1, 0XBC81, 0X7C40,
            0XB401, 0X74C0, 0X7580, 0XB541, 0X7700, 0XB7C1, 0XB681, 0X7640,
            0X7200, 0XB2C1, 0XB381, 0X7340, 0XB101, 0X71C0, 0X7080, 0XB041,
            0X5000, 0X90C1, 0X9181, 0X5140, 0X9301, 0X53C0, 0X5280, 0X9241,
            0X9601, 0X56C0, 0X5780, 0X9741, 0X5500, 0X95C1, 0X9481, 0X5440,
            0X9C01, 0X5CC0, 0X5D80, 0X9D41, 0X5F00, 0X9FC1, 0X9E81, 0X5E40,
            0X5A00, 0X9AC1, 0X9B81, 0X5B40, 0X9901, 0X59C0, 0X5880, 0X9841,
            0X8801, 0X48C0, 0X4980, 0X8941, 0X4B00, 0X8BC1, 0X8A81, 0X4A40,
            0X4E00, 0X8EC1, 0X8F81, 0X4F40, 0X8D01, 0X4DC0, 0X4C80, 0X8C41,
            0X4400, 0X84C1, 0X8581, 0X4540, 0X8701, 0X47C0, 0X4680, 0X8641,
            0X8201, 0X42C0, 0X4380, 0X8341, 0X4100, 0X81C1, 0X8081, 0X4040
        };
        public static byte[] CalculateCrc(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ushort crc = ushort.MaxValue;

            foreach (byte b in data)
            {
                byte tableIndex = (byte)(crc ^ b);
                crc >>= 8;
                crc ^= CrcTable[tableIndex];
            }

            return BitConverter.GetBytes(crc);
        }
        public static string CheckCRC16Modbus(string src)
        {
            byte[] data = HexStringToByte(src);
            byte[] crc16 = GetCrc16(data, 0, data.Length);
            return ByteArrayToHexString(crc16);
        }
        public static string ByteArrayToHexString(byte[] data)//字节数组转为十六进制字符串  
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));  //如果想每个字节之间有空格，只需要sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3,' ')即可
            return sb.ToString().ToUpper();
        }
        public static string ReplaceHexStr(string hex)
        {
            string returnStr = "";
            foreach (char item in hex)
            {
                if (HexSet.Contains<char>(item))
                    returnStr += item;
                else
                    returnStr += "0";
            }
            return returnStr;
        }
        public static byte[] HexStringToByte(string text)
        {
            text = ReplaceHexStr(text);
            string send_data = "";
            byte[] temp = new byte[1];
            text = text.Replace(" ", "");
            string buf = text;
            string pattern = @"\s";
            string replacement = "";
            Regex rgx = new Regex(pattern);
            send_data = rgx.Replace(buf, replacement);
            int num = 0;
            num = text.Length / 2;
            byte[] return_data = new byte[num];
            //插入转换
            if (return_data.Length == 0 || (text.Length != 1 && text.Length % 2 != 0))
            {
                string HexText = StringToHexHelper(text);
                send_data = HexText;
                num = HexText.Length / 2; ;
                return_data = new byte[num];
            }

            for (int i = 0; i < num; i++)
            {
                var tempStr = send_data.Substring(i * 2, 2).Trim();
                temp[0] = Convert.ToByte(tempStr, 16);
                return_data[i] = temp[0];
            }
            return return_data;
        }

        static string StringReplace(string text)
        {
            string returnStr = "";
            for (int i = 0; i < text.Length; i++)
            {
                if ((int)text[i] > 127)
                    returnStr += "0";
                else
                    returnStr += text[i];
            }
            return returnStr;
        }

        public static string StringToHexHelper(string text)
        {
            int lengthIndex = 0;
            if (text.Length != 1 && text.Length % 2 != 0)
            {
                lengthIndex = text.Length - 1;
            }
            if (HexSet.Contains<char>(text[lengthIndex]))
            {
                string byteStr = "0" + text[lengthIndex];
                text = text.Substring(0, text.Length - 1) + byteStr;// byteStr.Length / 2;
            }
            return text;
        }
        public static byte[] GetCrc16(byte[] buffer, int start = 0, int len = 0)
        {
            if (buffer == null || buffer.Length == 0) return null;
            if (start < 0) return null;
            if (len == 0) len = buffer.Length - start;
            int length = start + len;
            if (length > buffer.Length) return null;
            ushort crc = 0xFFFF;// Initial value
            for (int i = start; i < length; i++)
            {
                crc ^= buffer[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) > 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);// 0xA001 = reverse 0x8005 
                    else
                        crc = (ushort)(crc >> 1);
                }
            }
            byte[] ret = BitConverter.GetBytes(crc);
            return ret;
        }
        public static string BytesToHexStrWithSpace(byte[] bytes, string Separator = " ")
        {
            string text = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    text = text + bytes[i].ToString("X2") + Separator;
                }
            }

            return text;
        }

        public static byte[] StringFormat(string txtsend, bool IsHex, bool IsCrc)
        {
            byte[] btSend = new byte[1];
            byte[] crcSend = new byte[1];
            if (IsHex)
            {
                //如果是HEX,清空命令中的空格
                string[] txts = txtsend.Split(' ');
                string txtssend = "";
                for (int i = 0; i < txts.Length; i++)
                {
                    txtssend += txts[i];
                }
                txtsend = txtssend;
            }

            if (IsHex)
            {
                txtsend = txtsend.Replace(" ", "").ToUpper(); //20220616格式化字符串
                if (IsCrc)
                {
                    string strcrc = StringHelper.CheckCRC16Modbus(txtsend);
                    //txtsend += strcrc; //20220616增加修复 自动计算CRC错误，并且不显示完整的命令字符串
                    crcSend = StringHelper.HexStringToByte(strcrc);
                    btSend = StringHelper.HexStringToByte(txtsend);
                    byte[] dataRSend = new byte[btSend.Length + crcSend.Length];
                    btSend.CopyTo(dataRSend, 0);
                    crcSend.CopyTo(dataRSend, btSend.Length);

                    btSend = dataRSend;
                }
                else
                {
                    btSend = StringHelper.HexStringToByte(txtsend);
                }
            }
            else
            {
                btSend = Encoding.ASCII.GetBytes(txtsend);
            }
            return btSend;
        }

        public static byte[] StringFormat(string messageType, string message, bool IsCrc)
        {
            if (messageType == Constants.Hex)
            {
                if (IsCrc)
                {
                    var crcStr = CheckCRC16Modbus(message);
                    var crcBytes = HexStringToByte(crcStr);
                    var dataBytes = HexStringToByte(message);
                    byte[] data = new byte[dataBytes.Length + crcBytes.Length];
                    dataBytes.CopyTo(data, 0);
                    crcBytes.CopyTo(data, dataBytes.Length);
                    return data;
                }
                else
                {
                    return HexStringToByte(message);
                }
            }
            else
            {
                return Encoding.ASCII.GetBytes(message);
            }
        }
    }
}
