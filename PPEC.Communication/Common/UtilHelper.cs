using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;

namespace PPEC.Communication.Common
{
    public static class UtilHelper
    {
        private const ushort Poly = 0x1021;

        public static ushort Calc(byte[] buf, int offset, int len)
        {
            ushort crc = 0xFFFF;

            for (int i = offset; i < offset + len; i++)
            {
                crc ^= (ushort)(buf[i] << 8);
                for (int j = 0; j < 8; j++)
                    crc = (crc & 0x8000) != 0
                          ? (ushort)((crc << 1) ^ Poly)
                          : (ushort)(crc << 1);
            }
            return crc;
        }
        public static ushort CalcCrc16(byte[] buf, int offset, int len)
        {
            const ushort poly = 0xA001;
            ushort crc = 0xFFFF;

            for (int i = 0; i < len; i++)
            {
                crc ^= buf[offset + i];
                for (int j = 0; j < 8; j++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= poly;
                }
            }
            return crc;
        }
        public static double GetValueForFormula(FormulaEnum formula, double paramA, double paramB, uint value)
        {
            double result = 1 * value;
            if (paramA == 0)
            {
                return result;
            }

            switch (formula)
            {
                case FormulaEnum.Add:
                    result = paramA * value + paramB;
                    break;
                case FormulaEnum.Sub:
                    result = paramA * value - paramB;
                    break;
                case FormulaEnum.Mul:
                    result = paramA * value * paramB;
                    break;
                case FormulaEnum.Exc:
                    result = paramA * value / paramB;
                    break;
                case FormulaEnum.None:
                    result = result * paramA;
                    break;

            }
            return result;
        }
        public static bool TryGetByDecimalKey(Dictionary<string, string> dict, uint decimalKey, out string value)
        {
            value = null;
            var formats = new[]
            {
            $"0x{decimalKey:X}",
            $"0x{decimalKey:x}",
            $"0x{decimalKey:X4}",
            $"0x{decimalKey:x4}",
            decimalKey.ToString()
        };

            foreach (var format in formats)
            {
                if (dict.TryGetValue(format, out value))
                {
                    return true;
                }
            }

            return false;
        }
        public static Dictionary<string, string> ParseExcelDataToDictionary(string excelData)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (string.IsNullOrEmpty(excelData))
                    return null;
                // 按换行符分割
                var lines = excelData.Split(new[] { '\n', '\r',';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // 找到第一个冒号的位置进行分割
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim();
                        var value = line.Substring(colonIndex + 1).Trim();
                        result[key] = value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    internal static class EndianExt          // 大端输出
    {
        public static byte[] GetBigEndian(this ushort v) =>
            new[] { (byte)(v >> 8), (byte)v };
        public static byte[] GetBigEndian(this uint v) =>
            new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    }
    internal static class BeConverter        // 大端读取
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(byte[] buf, int idx) =>
            (ushort)((buf[idx] << 8) | buf[idx + 1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(byte[] buf, int idx) =>
            ((uint)buf[idx] << 24) |
            ((uint)buf[idx + 1] << 16) |
            ((uint)buf[idx + 2] << 8) |
             buf[idx + 3];
    }

}
