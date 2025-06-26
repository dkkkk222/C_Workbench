using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PPEC.Communication
{
    public class TransportModbus : Transport, ITransportSerial
    {
        public const int RequestFrameStartLength = 7;

        public const int ResponseFrameStartLength = 4;

        public const int ResponseUpFrameLength = 10;

        public TransportModbus(IStreamResource streamResource, IFactory modbusFactory, ILogger logger)
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

        public override void Write(IMessage message)
        {
            DiscardInBuffer();
            byte[] frame = message.MessageFrame;
            if (message.IsSendPure)
            {
                frame = message.PureBytes;
                CheckFrame = false;
            }
            else
            {
                CheckFrame = true;
            }
            Logger.LogFrameTx(frame);

            StreamResource.Write(frame, 0, frame.Length);
        }

        public override IMessage CreateResponse<T>(byte[] frame)
        {
            byte functionCode = frame[1];
            IMessage response;

            // check for slave exception response else create message from frame
            if (functionCode > ConstParams.ExceptionOffset)
            {
                //response = MessageFactory.CreateMessage<SlaveExceptionResponse>(frame);
                response = MessageFactory.CreateMessage<ExceptionResponse>(frame);
            }
            else
            {
                response = MessageFactory.CreateMessage<T>(frame);
            }
            if (CheckFrame && !ChecksumsMatch(response, frame))
            {
                string msg = $"Checksums CRC failed to match {string.Join(", ", response.MessageFrame)} != {string.Join(", ", frame)}";
                Logger.Warning(msg);
                Logger.Error(msg);
                throw new IOException(msg);
            }

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
        public virtual byte[] Read(int count)
        {
            byte[] frameBytes = new byte[count];
            int numBytesRead = 0;

            while (numBytesRead != count)
            {
                numBytesRead += StreamResource.Read(frameBytes, numBytesRead, count - numBytesRead);
                if (numBytesRead == RequestFrameStartLength && !CheckFrame)
                {
                    return frameBytes.Take(numBytesRead).ToArray();
                }
            }

            return frameBytes;
        }
        public void IgnoreResponse()
        {
            byte[] frame = ReadResponse();

            Logger.LogFrameIgnoreRx(frame);
        }

        public override IMessage ReadResponse<T>()
        {
            byte[] frame = ReadResponse();

            Logger.LogFrameRx(frame);

            return CreateResponse<T>(frame);
        }
        private byte[] ReadResponse()
        {
            if (CheckFrame)
            {
                byte[] frameStart = Read(ResponseFrameStartLength);
                byte[] frameEnd = Read(GetResponseBytesToRead(frameStart));
                byte[] frame = frameStart.Concat(frameEnd).ToArray();
                return frame.ToArray();
            }
            else
            {
                byte[] frame = Read(ResponseUpFrameLength);
                return frame.ToArray();
            }
            //return frame.ToArray();
        }
        public bool ChecksumsMatch(IMessage message, byte[] messageFrame)
        {
            ushort messageCrc = BitConverter.ToUInt16(messageFrame, messageFrame.Length - 2);

            ushort calculatedCrc = BitConverter.ToUInt16(message.MessageFrame, message.MessageFrame.Length - 2);
            if (message.IsSendPure)
                calculatedCrc = BitConverter.ToUInt16(message.PureBytes, message.PureBytes.Length - 2);
            return messageCrc == calculatedCrc;
        }

        public static int GetResponseBytesToRead(byte[] frameStart)
        {
            byte commandCode = frameStart[1];
            if (commandCode > ConstParams.ExceptionOffset)
            {
                return 1;
            }
            int returnData = 0;
            switch (commandCode)
            {
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x21:
                    returnData = frameStart[2] + 1;
                    break;
                case 0x05:
                case 0x06:
                case 0x08:
                case 0x00:
                case 0x15:
                case 0x16:
                    returnData = 4;
                    break;
                    break;
                case 0x23:
                    throw new NotSupportedException();
                    break;
                default:
                    returnData = 4;
                    break;
            }
            return returnData;
        }
    }
}
