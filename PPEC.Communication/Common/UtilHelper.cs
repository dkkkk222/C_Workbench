using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Common
{
    internal class UtilHelper
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
