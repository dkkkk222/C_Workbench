using PPEC.Communication.Interface;
using PPEC.Communication.Parameter.Utility.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class WriteMultipleRequestModbus
    : AbstractMessageWithData<RegisterCollection>, IRequest
    {
        public WriteMultipleRequestModbus()
        {
        }

        public WriteMultipleRequestModbus(byte slaveAddress, AddressCollection startAddress, RegisterCollection data)
        {
            AddressCollection = startAddress;
            SlaveAddress = slaveAddress;
            NumberOfPoints = (ushort)data.Count;
            ByteCount = (byte)(data.Count * 2);
            Data = data;
            MessageImpl.IsHigthOrLow = true;
            MessageImpl.FunctionCode = FunctionCodes.WriteMultipleRegistersModbus;
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
        public ushort NumberOfPoints
        {
            get => MessageImpl.NumberOfPoints.Value;

            set
            {
                if (value > ConstParams.MaximumRegisterRequestResponseSize)
                {
                    string msg = $"Maximum amount of data {ConstParams.MaximumRegisterRequestResponseSize} registers.";
                    throw new ArgumentOutOfRangeException(nameof(NumberOfPoints), msg);
                }

                MessageImpl.NumberOfPoints = value;
            }
        }
        public IMessageDataCollection AddressCollection
        {
            get => MessageImpl.AddressCollection;
            set => MessageImpl.AddressCollection = value;
        }
        public bool IsQuickCommand { get; set; }
        public override int MinimumFrameSize => 7;

        public override string ToString()
        {
            string msg = $"Write {NumberOfPoints} holding registers starting at address {""}.";
            return msg;
        }

        public void ValidateResponse(IMessage response)
        {
            var typedResponse = (WriteMultipleResponseModbus)response;

            if (NumberOfPoints != typedResponse.NumberOfPoints)
            {
                string msg = $"Unexpected number of points in response. Expected {NumberOfPoints}, received {typedResponse.NumberOfPoints}.";
                throw new IOException(msg);
            }
        }
        protected override void InitializeUnique(byte[] frame)
        {
            if (frame.Length < MinimumFrameSize + frame[6])
            {
                throw new FormatException("Message frame does not contain enough bytes.");
            }
            MessageImpl.SlaveAddress = frame[0];
            MessageImpl.FunctionCode = frame[1];
            MessageImpl.IsHigthOrLow = true;
            AddressCollection = new AddressCollection((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 2)));

            NumberOfPoints = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 4));
            ByteCount = frame[6];
            Data = new RegisterCollection(frame.Slice(7, ByteCount).ToArray());
        }
    }
}
