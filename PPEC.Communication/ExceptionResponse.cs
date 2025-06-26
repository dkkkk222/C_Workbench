using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PPEC.Communication
{
    internal class ExceptionResponse : AbstractMessageWithData<RegisterCollection>, IMessage
    {
        private static readonly Dictionary<byte, string> _exceptionMessages = CreateExceptionMessages();

        public ExceptionResponse()
        {
        }

        public ExceptionResponse(byte slaveAddress, byte functionCode, byte exceptionCode)
        {
            SlaveExceptionCode = exceptionCode;
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
        public override int MinimumFrameSize => 3;
        public byte SlaveAddress
        {
            get => MessageImpl.SlaveAddress.Value;
            set => MessageImpl.SlaveAddress = value;
        }
        public byte SlaveExceptionCode
        {
            get => MessageImpl.ExceptionCode.Value;
            set => MessageImpl.ExceptionCode = value;
        }
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
        /// <summary>
        ///     Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            string msg = _exceptionMessages.ContainsKey(SlaveExceptionCode)
                ? _exceptionMessages[SlaveExceptionCode]
                : Resources.Unknown;

            return string.Format(
                CultureInfo.InvariantCulture,
                Resources.SlaveExceptionResponseFormat,
                Environment.NewLine,
                FunctionCode,
                SlaveExceptionCode,
                msg);
        }
        internal static Dictionary<byte, string> CreateExceptionMessages()
        {
            return new Dictionary<byte, string>(9)
            {
                {1, Resources.IllegalFunction},
                {2, Resources.IllegalDataAddress},
                {3, Resources.IllegalDataValue},
                {4, Resources.SlaveDeviceFailure},
                {5, Resources.Acknowlege},
                {6, Resources.SlaveDeviceBusy},
                {8, Resources.MemoryParityError},
                {10, Resources.GatewayPathUnavailable},
                {11, Resources.GatewayTargetDeviceFailedToRespond}
            };
        }

        protected override void InitializeUnique(byte[] frame)
        {
            FunctionCode = frame[1];

            switch (FunctionCode)
            {
                default:
                    SlaveExceptionCode = 0x00;
                    break;
            }
        }
    }
}
