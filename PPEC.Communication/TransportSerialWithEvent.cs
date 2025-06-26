using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class TransportSerialWithEvent : TransportBaseEvent
    {
        public TransportSerialWithEvent(IStreamResource streamResource, IFactory modbusFactory, ILogger logger)
            : base(streamResource, modbusFactory, logger)
        {
            if (modbusFactory == null) throw new ArgumentNullException(nameof(modbusFactory));
            Debug.Assert(streamResource != null, "Argument streamResource cannot be null.");
        }
        #region MySelfSerialTransport
        public void DiscardInBuffer()
        {
            StreamResource.DiscardInBuffer();
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
            SerialPort serialPort = (SerialPort)sender;
            const long ticksWait = TimeSpan.TicksPerMillisecond * 5;
            DateTime dateTimeLastRead = DateTime.Now;
            do
            {
                try
                {
                    dateTimeLastRead = DateTime.Now;
                    while ((serialPort.BytesToRead) == 0)
                    {
                        System.Threading.Thread.Sleep(1);
                        if ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) > ticksWait)
                            break;
                    }
                    if (serialPort.BytesToRead > 0)
                    {
                        int nums = serialPort.BytesToRead;
                        byte[] bytes = new byte[nums];
                        serialPort.Read(bytes, 0, nums);
                        ReceivedBuffer.Write(bytes, 0, bytes.Length);
                        break;
                    }
                }
                catch (Exception ex)
                { }
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
