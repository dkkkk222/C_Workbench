using PPEC.Communication.Interface;
using PPEC.Communication.Parameter.Utility.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class ReadResponseModbus
     : AbstractMessageWithData<RegisterCollection>, IMessage
    {
        public ReadResponseModbus()
        {
        }

        public ReadResponseModbus(byte slaveAddress, RegisterCollection data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ByteCount = data.ByteCount;
            Data = data;
        }
        public byte[] PureBytes { get; set; }
        public byte? StartOfInfo
        {
            get => MessageImpl.StartOfInfo;
            set => MessageImpl.StartOfInfo = value;
        }
        public byte? EndOfInfo
        {
            get => MessageImpl.EndOfInfo;
            set => MessageImpl.EndOfInfo = value;
        }
        public byte ByteCount
        {
            get => MessageImpl.ByteCount.Value;
            set => MessageImpl.ByteCount = value;
        }

        public override int MinimumFrameSize => 3;
        public byte? EndInfoChart
        {
            get => MessageImpl.EndOfInfo;
            set => MessageImpl.EndOfInfo = value;
        }
        public IMessageDataCollection AddressCollection
        {
            get => MessageImpl.AddressCollection;
            set => MessageImpl.AddressCollection = value;
        }
        public bool IsQuickCommand { get; set; }
        public override string ToString()
        {
            string msg = $"Read {Data.Count} {(FunctionCode == FunctionCodes.ReadHoldingRegisters ? "holding" : "input")} registers.";
            return msg;
        }

        protected override void InitializeUnique(byte[] frame)
        {
            if (frame.Length < MinimumFrameSize + frame[2])
            {

                Logger.Instance.Error($"PPEC返回错误码：{frame[2]},返回帧{string.Join(", ", frame)}");
                throw new FormatException("Message frame does not contain enough bytes.");
            }
            MessageImpl.SlaveAddress = frame[0];
            MessageImpl.FunctionCode = frame[1];
            ByteCount = frame[2];
            Data = new RegisterCollection(frame.Slice(3, ByteCount).ToArray());

        }
    }
}
