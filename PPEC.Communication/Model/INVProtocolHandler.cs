using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Interface;

namespace PPEC.Communication.Model
{
    public class INVProtocolHandler : IProtocolHandler, IDisposable
    {
        private const int FrameLen = 16;          // = 帧头2 + 数据12 + CRC2
        private const byte Head1 = 0x55;
        private const byte Head2 = 0xAA;

        private readonly byte[] _buf = new byte[4096];
        private int _len = 0;
        private bool _disposed;

        public event EventHandler<IUartMessage> MessageParsed;

        /// <summary>上层收到串口原始字节后直接调用</summary>
        public void Feed(ReadOnlySpan<byte> raw)
        {
            if (_disposed || raw.IsEmpty) return;

            // ---------- 复制到内部缓冲 ----------
            int srcLen = raw.Length;
            if (_len + srcLen > _buf.Length) _len = 0;     // 简单溢出保护
            raw.CopyTo(new Span<byte>(_buf, _len, srcLen));
            _len += srcLen;

            // ---------- 状态机解析 ----------
            int idx = 0;
            while (_len - idx >= FrameLen)
            {
                // (1) 找帧头
                if (_buf[idx] != Head1 || _buf[idx + 1] != Head2)
                {
                    idx++;                                  // 滑动一个字节继续
                    continue;
                }

                // (2) CRC16-IBM（Modbus）校验
                //ushort crcCalc = Crc16IBM(_buf, idx, FrameLen - 2);
                //ushort crcRecv = (ushort)(_buf[idx + FrameLen - 2] |
                //                          (_buf[idx + FrameLen - 1] << 8));
                //if (crcCalc != crcRecv)
                //{
                //    idx++;                                  // 校验失败，不丢大段
                //    continue;
                //}

                // (3) 组装消息对象
                var msg = new InvRealtimeMessage
                {
                    Vout = BitConverter.ToUInt16(_buf, idx + 2),
                    Iout = BitConverter.ToUInt16(_buf, idx + 4),
                    Phase = BitConverter.ToUInt16(_buf, idx + 6),
                    PI = BitConverter.ToUInt16(_buf, idx + 8),
                    FreqVout = BitConverter.ToUInt16(_buf, idx + 10),
                    FreqIout = BitConverter.ToUInt16(_buf, idx + 12),
                    Timestamp = DateTime.Now
                };
                MessageParsed?.Invoke(this, msg);

                idx += FrameLen;                            // 消费完一帧
            }

            // ---------- 前移剩余字节 ----------
            if (idx > 0)
            {
                Buffer.BlockCopy(_buf, idx, _buf, 0, _len - idx);
                _len -= idx;
            }
        }

        public void Reset() => _len = 0;

        #region CRC16-IBM
        private static ushort Crc16IBM(byte[] data, int offset, int length)
        {
            const ushort Poly = 0xA001;
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[offset + i];
                for (int j = 0; j < 8; j++)
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ Poly) : (ushort)(crc >> 1);
            }
            return crc;
        }
        #endregion

        public void Dispose()
        {
            _disposed = true;
            MessageParsed = null;
        }
    }
}
