using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class WriteSingleRequestModbus
  : AbstractMessageWithData<RegisterCollection>, IRequest
    {
        public WriteSingleRequestModbus()
        {
        }

        public WriteSingleRequestModbus(byte slaveAddress, AddressCollection startAddress, RegisterCollection data)
        {
            AddressCollection = startAddress;
            SlaveAddress = slaveAddress;
            Data = data;
            MessageImpl.IsHigthOrLow = true;
            MessageImpl.FunctionCode = FunctionCodes.WriteSingleRegister;
        }
        public byte[] PureBytes { get; set; }
        public bool IsQuickCommand { get; set; }
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
        public void ValidateResponse(IMessage response)
        {
            var typedResponse = (WriteSingleRequestModbus)response;

            if (AddressCollection.NetworkBytes[0] != typedResponse.AddressCollection.NetworkBytes[0])
            {
                string msg = $"Unexpected start address in response. Expected {AddressCollection}, received {typedResponse.AddressCollection}.";
                throw new IOException(msg);
            }

            if (Data.First() != typedResponse.Data.First())
            {
                string msg = $"Unexpected data in response. Expected {Data.First()}, received {typedResponse.Data.First()}.";
                throw new IOException(msg);
            }
        }
        protected override void InitializeUnique(byte[] frame)
        {
            MessageImpl.SlaveAddress = frame[0];
            MessageImpl.FunctionCode = frame[1];
            MessageImpl.IsHigthOrLow = true;
            AddressCollection = new AddressCollection((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 2)));

            Data = new RegisterCollection((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 4)));
        }
    }
}
