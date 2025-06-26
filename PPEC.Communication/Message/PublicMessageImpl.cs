using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Message
{
    internal class PublicMessageImpl
    {
        // smallest supported message frame size (sans checksum)
        private const int MinimumFrameSize = 2;


        public PublicMessageImpl()
        {
        }
        /// <summary>
        /// 下位机地址
        /// </summary>
        public byte? SlaveAddress { get; set; }
        /// <summary>
        /// 起始位
        /// </summary>
        public byte? StartOfInfo { get; set; }
        /// <summary>
        /// 命令码
        /// </summary>
        public byte FunctionCode { get; set; }
        /// <summary>
        /// 异常码-NMODEBUS
        /// </summary>
        public byte? ExceptionCode { get; set; }
        /// <summary>
        /// 起始地址
        /// </summary>
        public IMessageDataCollection AddressCollection { get; set; }
        /// <summary>
        /// 上位机传递的长度
        /// </summary>
        public ushort? NumberOfPoints { get; set; }
        /// <summary>
        /// 数据长度，下位机返回的长度
        /// </summary>
        public byte? ByteCount { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public IMessageDataCollection Data { get; set; }
        /// <summary>
        /// 停止位
        /// </summary>
        public byte? EndOfInfo { get; set; }

        public byte? EndInfoChart { get; set; }
        /// <summary>
        /// True是大端，False是小端
        /// </summary>
        public bool IsHigthOrLow { get; set; } = false;
        /// <summary>
        /// 获取协议帧，对数据进行CRC效验，并根据情况判断是否添加结束符
        /// </summary>
        public byte[] MessageFrame
        {
            get
            {
                var messageFrame = ProtocolDataUnit;
                var crc = Utility.CalculateCrc(messageFrame);
                int framLength = messageFrame.Length + crc.Length;

                if (EndOfInfo != null)
                    framLength += 1;

                var messageBody = new MemoryStream(framLength);
                messageBody.Write(messageFrame, 0, messageFrame.Length);
                messageBody.Write(crc, 0, crc.Length);

                if (EndOfInfo != null)
                    messageBody.WriteByte(EndOfInfo.Value);

                return messageBody.ToArray();
            }
        }
        /// <summary>
        /// 协议帧处理
        /// </summary>
        public byte[] ProtocolDataUnit
        {
            get
            {
                List<byte> pdu = new List<byte>();

                if (StartOfInfo != null)
                {
                    pdu.Add(StartOfInfo.Value);
                }

                if (SlaveAddress != null)
                {
                    pdu.Add(SlaveAddress.Value);
                }

                pdu.Add(FunctionCode);
                if (ExceptionCode.HasValue)
                {
                    pdu.Add(ExceptionCode.Value);
                }
                if (AddressCollection != null)
                {
                    if (IsHigthOrLow)
                    {
                        pdu.AddRange(AddressCollection.HostNetworkBytes);
                    }
                    else
                    {
                        pdu.AddRange(AddressCollection.NetworkBytes);
                    }
                }
                if (NumberOfPoints.HasValue)
                {
                    if (IsHigthOrLow)
                    {
                        pdu.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)NumberOfPoints.Value)));
                    }
                    else
                    {
                        pdu.AddRange(BitConverter.GetBytes(IPAddress.NetworkToHostOrder((short)NumberOfPoints.Value)));
                    }
                }
                if (ByteCount != null)
                {
                    pdu.Add(ByteCount.Value);
                }

                if (Data != null)
                {
                    pdu.AddRange(Data.NetworkBytes);
                }
                return pdu.ToArray();
            }
        }
    }
}
