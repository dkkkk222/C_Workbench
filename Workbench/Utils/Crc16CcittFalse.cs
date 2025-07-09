using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Utils
{
    public class Crc16CcittFalse
    {
        private const ushort Poly = 0x1021;
        private const ushort InitialValue = 0xFFFF;

        private static readonly ushort[] CrcTable = GenerateCrcTable();

        /// <summary>
        /// 生成CRC-16查找表
        /// </summary>
        private static ushort[] GenerateCrcTable()
        {
            var table = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)(i << 8); // 将字节移到高8位
                for (int j = 0; j < 8; j++)
                {
                    // 检查最高位是否为1
                    if ((crc & 0x8000) != 0)
                    {
                        // 如果是1，左移一位并与多项式异或
                        crc = (ushort)((crc << 1) ^ Poly);
                    }
                    else
                    {
                        // 如果是0，仅左移一位
                        crc <<= 1;
                    }
                }
                table[i] = crc;
            }
            return table;
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// </summary>
        /// <param name="hex">输入的十六进制字符串</param>
        /// <returns>字节数组</returns>
        private static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return new byte[0];
            }
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符串的长度必须是偶数。", nameof(hex));
            }

            int byteCount = hex.Length / 2;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                try
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                catch (FormatException)
                {
                    throw new ArgumentException("输入的字符串包含非十六进制字符。", nameof(hex));
                }
            }
            return bytes;
        }

        /// <summary>
        /// 计算给定字节数组的CRC-16/CCITT-FALSE值
        /// </summary>
        /// <param name="data">要计算的字节数组</param>
        /// <returns>16位的CRC校验码</returns>
        private static ushort CalculateCrc(byte[] data)
        {
            ushort crc = InitialValue; // 从初始值开始

            foreach (byte b in data)
            {
                // 这是查表法的核心计算公式 (对于非反转的CRC)
                // 1. (crc >> 8) ^ b: 取CRC的高8位与当前数据字节异或，得到表索引
                // 2. CrcTable[...]: 在表中查找预先计算好的值
                // 3. (crc << 8): 将CRC的低8位移到高8位
                // 4. ... ^ ...: 将移位后的CRC与查表结果异或
                crc = (ushort)((crc << 8) ^ CrcTable[((crc >> 8) ^ b) & 0xFF]);
            }

            // 此特定CRC变体没有最终的XOR操作 (XOR Out = 0x0000)
            return crc;
        }

        /// <summary>
        /// 计算十六进制字符串的CRC-16/CCITT-FALSE，并以十六进制字符串形式返回
        /// </summary>
        /// <param name="hexInput">输入的十六进制字符串 (例如 "01A2FF")</param>
        /// <returns>4个字符的十六进制CRC字符串 (例如 "B4A1")</returns>
        public static string Calculate(string hexInput)
        {
            // 1. 将输入的Hex字符串转换为字节数组
            byte[] data = HexStringToByteArray(hexInput);

            // 2. 计算字节数组的CRC值
            ushort crcValue = CalculateCrc(data);

            // 3. 将计算结果格式化为4位大写的Hex字符串
            return crcValue.ToString("X4");
        }
    }
}
