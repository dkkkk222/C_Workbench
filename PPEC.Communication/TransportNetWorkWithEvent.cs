using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class TransportNetWorkWithEvent : TransportBaseEvent
    {
        public TransportNetWorkWithEvent(IStreamResource streamResource, IFactory modbusFactory, ILogger logger)
            : base(streamResource, modbusFactory, logger)
        {
            if (modbusFactory == null) throw new ArgumentNullException(nameof(modbusFactory));
            Debug.Assert(streamResource != null, "Argument streamResource cannot be null.");
        }
        #region MySelfSerialTransport
        private bool _checkFrame = true;
        /// <summary>
        /// Gets or sets a value indicating whether LRC/CRC frame checking is performed on messages.
        /// </summary>
        public bool CheckFrame
        {
            get => _checkFrame;
            set => _checkFrame = value;
        }
        public void DiscardInBuffer()
        {
            StreamResource.DiscardInBuffer();
        }
        public void ClearReceivedBuffer()
        {
            StreamResource.ClearReceivedBuffer();
        }
        public override void Write(IMessage message)
        {
            DiscardInBuffer();
            byte[] frame = BuildMessageFrame(message);

            Logger.LogFrameTx(frame);

            StreamResource.Write(frame, 0, frame.Length);
        }

        public override void DataReceivedToBufferHandler(object sender)
        {
            TcpClientWithListener tcpClientListener = (TcpClientWithListener)sender;
            const long ticksWait = TimeSpan.TicksPerMillisecond * 5;

            DateTime dateTimeLastRead = DateTime.Now;
            do
            {
                try
                {
                    dateTimeLastRead = DateTime.Now;
                    if (tcpClientListener._byteToRead > 0)
                    {
                        ReceivedBuffer.Write(tcpClientListener.ReceivedBuffer, 0, tcpClientListener._byteToRead);
                        ClearReceivedBuffer();
                        break;
                    }

                }
                catch (Exception ex)
                {
                    string a = ex.Message;
                }
            }
            while ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) < ticksWait);
        }

        public override byte[] BuildMessageFrame(IMessage message)
        {
            var messageFrame = message.PureBytes;
            return messageFrame;
        }

        public override IMessage BuildResponseFromBuffer<T>()
        {
            IMessage response = MessageFactory.CreateMessage<T>(ReceivedBuffer.ToArray());

            return response;
        }

        public override void OnValidateResponse(IMessage request, IMessage response)
        {
            if (request.SlaveAddress != response.SlaveAddress)
            {
                string msg = $"Response slave address does not match request. Expected {request.SlaveAddress}, received {response.SlaveAddress}.";
                throw new IOException(msg);
            }
        }
        #endregion
    }
}
