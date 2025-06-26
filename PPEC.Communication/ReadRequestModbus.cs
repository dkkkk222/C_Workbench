using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class ReadRequestModbus
    : AbstractMessage, IRequest
    {
        public ReadRequestModbus()
        {
        }

        public ReadRequestModbus(byte slaveAddress, AddressCollection startAddress, ushort numberOfPoints, byte? endData = null)
        {
            SlaveAddress = slaveAddress;
            AddressCollection = startAddress;
            NumberOfPoints = numberOfPoints;
            MessageImpl.IsHigthOrLow = true;
            MessageImpl.FunctionCode = FunctionCodes.ReadHoldingRegisters;
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
        public IMessageDataCollection AddressCollection
        {
            get => MessageImpl.AddressCollection;
            set => MessageImpl.AddressCollection = value;
        }
        public override int MinimumFrameSize => 6;

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
        public bool IsQuickCommand { get; set; }
        public override string ToString()
        {
            string msg = $"Read {NumberOfPoints} {(FunctionCode == FunctionCodes.ReadHoldingRegisters ? "holding" : "input")} registers starting at address {""}.";
            return msg;
        }

        public void ValidateResponse(IMessage response)
        {
            var typedResponse = response as ReadResponseModbus;
            Debug.Assert(typedResponse != null, "Argument response should be of type ReadHoldingInputRegistersResponse.");
            var expectedByteCount = NumberOfPoints * 2;

            if (expectedByteCount != typedResponse.ByteCount)
            {
                string msg = $"Unexpected byte count. Expected {expectedByteCount}, received {typedResponse.ByteCount}.";
                throw new IOException(msg);
            }
        }

        protected override void InitializeUnique(byte[] frame)
        {
            MessageImpl.SlaveAddress = frame[0];
            MessageImpl.FunctionCode = frame[1];
            MessageImpl.IsHigthOrLow = true;
            AddressCollection = new AddressCollection((ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(frame, 2)));
            NumberOfPoints = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 4));// (ushort)(BitConverter.ToInt16(frame, 4));
        }
    }
}
