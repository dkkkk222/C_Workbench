using PPEC.Communication.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public abstract class AbstractMessage
    {
        private readonly PublicMessageImpl _messageImpl;
        /// <summary>
        ///     Abstract Modbus message.
        /// </summary>
        public AbstractMessage()
        {
            _messageImpl = new PublicMessageImpl();
        }

        public byte FunctionCode
        {
            get => _messageImpl.FunctionCode;
            set => _messageImpl.FunctionCode = value;
        }

        public byte? SlaveAddress
        {
            get => _messageImpl.SlaveAddress;
            set => _messageImpl.SlaveAddress = value;
        }
        public byte[] MessageFrame => _messageImpl.MessageFrame;

        public virtual byte[] ProtocolDataUnit => _messageImpl.ProtocolDataUnit;

        public abstract int MinimumFrameSize { get; }

        public bool IsSendPure { get; set; }

        internal PublicMessageImpl MessageImpl => _messageImpl;

        public void Initialize(byte[] frame)
        {
            InitializeUnique(frame);
        }
        protected abstract void InitializeUnique(byte[] frame);
    }
}
