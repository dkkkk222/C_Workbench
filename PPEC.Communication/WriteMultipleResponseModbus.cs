using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class WriteMultipleResponseModbus
    : AbstractMessage, IMessage
    {
        public WriteMultipleResponseModbus()
        {
        }

        public WriteMultipleResponseModbus(byte slaveAddress, IMessageDataCollection startAddress, ushort numberOfPoints)
        {
            AddressCollection = startAddress;
            NumberOfPoints = numberOfPoints;
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

        public override int MinimumFrameSize => 6;

        public IMessageDataCollection AddressCollection
        {
            get => MessageImpl.AddressCollection;
            set => MessageImpl.AddressCollection = value;
        }
        public bool IsQuickCommand { get; set; }
        public override string ToString()
        {
            string msg = $"Wrote {NumberOfPoints} holding registers starting at address {""}.";
            return msg;
        }

        protected override void InitializeUnique(byte[] frame)
        {
            MessageImpl.SlaveAddress = frame[0];
            MessageImpl.FunctionCode = frame[1];
            MessageImpl.IsHigthOrLow = true;
            AddressCollection = new AddressCollection((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 2)));
            NumberOfPoints = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 4));
        }
    }
}
