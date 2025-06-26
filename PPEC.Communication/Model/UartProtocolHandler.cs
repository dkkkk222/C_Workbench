using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication.Common;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    /// <summary>
    /// 帧解析
    /// </summary>
    public sealed class UartProtocolHandler: IProtocolHandler
    {
        public event EventHandler<IUartMessage> MessageParsed;
        private readonly List<byte> _buf = new List<byte>(1024);

        public event EventHandler<FrameUart> FrameParsed;

        public void Feed(ReadOnlySpan<byte> raw)
        {
            _buf.AddRange(raw.ToArray());

            while (_buf.Count >= 19)                          // 最小帧长
            {
                // 1) 查找帧头 D2 8C
                if (_buf[0] != 0xD2 || _buf[1] != 0x8C) { _buf.RemoveAt(0); continue; }

                // 2) 取有效数据长度
                ushort len = (ushort)((_buf[14] << 8) | _buf[15]);
                int frameLen = 2 + 1 + 1 + 7 + 2 + 1 + 2 + len + 2;
                if (_buf.Count < frameLen) break;             // 数据还没收全

                byte[] full = _buf.GetRange(0, frameLen).ToArray();
                _buf.RemoveRange(0, frameLen);

                // 3) CRC16/CCITT-FALSE 校验
                ushort crcIn = (ushort)((full[frameLen - 2] << 8) | full[frameLen - 1]);
                if (crcIn != UtilHelper.Calc(full, 16, len)) continue;  // CRC 错，丢弃

                // 4) 组装 Frame → Decode → 触发事件
                var dt = (UartDataType)((full[11] << 8) | full[12]);
                var data = new ReadOnlyMemory<byte>(full, 16, len);
                var msg = Decode(new FrameUart(dt, data));
                if (msg != null) MessageParsed?.Invoke(this, msg);
            }
        }

        private static IUartMessage Decode(FrameUart f)
        {
            byte[] d = f.Payload.ToArray();
            switch (f.DataType)
            {
                case UartDataType.ResetCommand: return new ResetCommand();

                case UartDataType.ReadRegisterCommand:
                    return new ReadRegisterCommand(
                                    (ushort)((d[0] << 8) | d[1]));

                case UartDataType.WriteRegisterCommand:
                    return new WriteRegisterCommand(
                                    (ushort)((d[0] << 8) | d[1]),
                                    ((uint)d[2] << 24) | ((uint)d[3] << 16) |
                                    ((uint)d[4] << 8) | d[5]);

                case UartDataType.ReadRegisterResponse:
                    return new ReadRegisterResponse(
                                    (ushort)((d[0] << 8) | d[1]),
                                    ((uint)d[2] << 24) | ((uint)d[3] << 16) |
                                    ((uint)d[4] << 8) | d[5]);

                default: return null;   // 未知类型可返回 null 或抛异常
            }
        }
    }
    #region 消息解析
    /// <summary>
    /// 复位
    /// </summary>
    public sealed class ResetCommand : IUartMessage { public UartDataType Type => UartDataType.ResetCommand; }
    /// <summary>
    /// 读
    /// </summary>
    public sealed class ReadRegisterCommand : IUartMessage
    {
        public ushort Address;
        public ReadRegisterCommand(ushort addr) { Address = addr; }
        public UartDataType Type => UartDataType.ReadRegisterCommand;
    }
    /// <summary>
    /// 写
    /// </summary>
    public sealed class WriteRegisterCommand : IUartMessage
    {
        public ushort Address; public uint Value;
        public WriteRegisterCommand(ushort addr, uint val)
        { Address = addr; Value = val; }
        public UartDataType Type => UartDataType.WriteRegisterCommand;
    }
    /// <summary>
    /// 读命令返回
    /// </summary>
    public sealed class ReadRegisterResponse : IUartMessage
    {
        public ushort Address; public uint Value;
        public ReadRegisterResponse(ushort addr, uint val)
        { Address = addr; Value = val; }
        public UartDataType Type => UartDataType.ReadRegisterResponse;
    }
    /// <summary>
    /// 帧解析分类
    /// </summary>
    //public static class UartMessageDecoder
    //{
    //    public static IUartMessage Decode(FrameUart f)
    //    {
    //        byte[] bytes = f.Payload.ToArray();   // 小片段复制一次即可
    //        switch (f.DataType)
    //        {
    //            case UartDataType.ResetCommand:
    //                return new ResetCommand();

    //            case UartDataType.ReadRegisterCommand:
    //                return new ReadRegisterCommand(BeConverter.ReadUInt16(bytes, 0));

    //            case UartDataType.WriteRegisterCommand:
    //                return new WriteRegisterCommand(
    //                    BeConverter.ReadUInt16(bytes, 0),
    //                    BeConverter.ReadUInt32(bytes, 2));

    //            case UartDataType.ReadRegisterResponse:
    //                return new ReadRegisterResponse(
    //                    BeConverter.ReadUInt16(bytes, 0),
    //                    BeConverter.ReadUInt32(bytes, 2));

    //            default:
    //                throw new NotSupportedException("未知 DataType: " + f.DataType);
    //        }
    //    }
    //}
    /// <summary>
    /// 发送帧组合
    /// </summary>
    public static class UartCommandBuilder
    {
        private static readonly byte[] Header = { 0xD2, 0x8C };
        private static readonly byte[] SrcDst = { 0x00, 0x0A };
        private static readonly byte[] FF7 = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        /// <summary>
        /// 复位
        /// </summary>
        /// <returns></returns>
        public static byte[] BuildReset()
        {
            var buf = new byte[20];
            int o = 0;
            Array.Copy(Header, buf, 2); o += 2;
            Array.Copy(SrcDst, 0, buf, o, 2); o += 2;
            Array.Copy(FF7, 0, buf, o, 7); o += 7;
            buf[o++] = 0x00; buf[o++] = 0x00;  // Dt
            buf[o++] = 0xFF;                    // DtRes
            buf[o++] = 0x00; buf[o++] = 0x02;  // Len
            buf[o++] = 0x3C; buf[o++] = 0xFF;  // Data
            ushort crc = UtilHelper.Calc(buf, 16, 2);
            buf[o++] = (byte)(crc >> 8); buf[o++] = (byte)crc;
            return buf;
        }
        /// <summary>
        /// 读
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static byte[] BuildReadReg(ushort addr)
        {
            var buf = new byte[20];
            int o = 0;
            Array.Copy(Header, buf, 2); o += 2;
            Array.Copy(SrcDst, 0, buf, o, 2); o += 2;
            Array.Copy(FF7, 0, buf, o, 7); o += 7;
            buf[o++] = 0x00; buf[o++] = 0x0A;
            buf[o++] = 0xFF;
            buf[o++] = 0x00; buf[o++] = 0x02;
            buf[o++] = (byte)(addr >> 8); buf[o++] = (byte)addr;
            ushort crc = UtilHelper.Calc(buf, 16, 2);
            buf[o++] = (byte)(crc >> 8); buf[o++] = (byte)crc;
            return buf;
        }
        /// <summary>
        /// 写
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] BuildWriteReg(ushort addr, uint value)
        {
            var buf = new byte[24];
            int o = 0;
            Array.Copy(Header, buf, 2); o += 2;
            Array.Copy(SrcDst, 0, buf, o, 2); o += 2;
            Array.Copy(FF7, 0, buf, o, 7); o += 7;
            buf[o++] = 0x00; buf[o++] = 0x0F;
            buf[o++] = 0xFF;
            buf[o++] = 0x00; buf[o++] = 0x06;
            buf[o++] = (byte)(addr >> 8); buf[o++] = (byte)addr;
            buf[o++] = (byte)(value >> 24);
            buf[o++] = (byte)(value >> 16);
            buf[o++] = (byte)(value >> 8);
            buf[o++] = (byte)value;
            ushort crc = UtilHelper.Calc(buf, 16, 6);
            buf[o++] = (byte)(crc >> 8); buf[o++] = (byte)crc;
            Array.Resize(ref buf, o);    // 精确长度
            return buf;
        }
    }
    #endregion
}
