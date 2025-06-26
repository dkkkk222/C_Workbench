using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class SerialAssistantRequestResponse
    : AbstractMessage, IRequest
    {
        public SerialAssistantRequestResponse()
        {
        }
        public SerialAssistantRequestResponse(byte[] sendBytes, bool isQuickCommand = false, bool isSendPure = false)
        {
            PureBytes = sendBytes;
            IsQuickCommand = isQuickCommand;
            IsSendPure = isSendPure;
        }
        public bool IsSendPure
        {
            get;
            set;
        }
        public byte[] PureBytes
        {
            get;
            set;
        }
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
        public byte? ByteCount
        {
            get => MessageImpl.ByteCount;
            set => MessageImpl.ByteCount = value;
        }
        public ushort? NumberOfPoints
        {
            get => MessageImpl.NumberOfPoints;

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
            var typedResponse = (SerialAssistantRequestResponse)response;

            if (NumberOfPoints != typedResponse.NumberOfPoints)
            {
                string msg = $"Unexpected number of points in response. Expected {NumberOfPoints}, received {typedResponse.NumberOfPoints}.";
                throw new IOException(msg);
            }
        }
        protected override void InitializeUnique(byte[] frame)
        {
            PureBytes = frame;
        }
    }
}
