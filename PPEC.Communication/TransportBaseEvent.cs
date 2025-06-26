using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public abstract class TransportBaseEvent : ITransport
    {
        private readonly object _syncLock = new object();
        private int _retries = ConstParams.DefaultRetries;
        private int _waitToRetryMilliseconds = ConstParams.DefaultWaitToRetryMilliseconds;
        private IStreamResource _streamResource;
        public MemoryStream ReceivedBuffer;
        private ManualResetEvent _doReceive = new ManualResetEvent(true);

        public event Action<object> ReceiveDataChanged;

        public event Action<object, IMessage> SendDataChanged;


        /// <summary>
        ///     This constructor is called by the NullTransport.
        /// </summary>
        public TransportBaseEvent(IFactory factory, ILogger logger)
        {
            Factory = factory;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal TransportBaseEvent(IStreamResource streamResource, IFactory factory, ILogger logger, int capacity = 1024)
            : this(factory, logger)
        {
            _streamResource = streamResource ?? throw new ArgumentNullException(nameof(streamResource));
            ReceivedBuffer = new MemoryStream(1024);
            _streamResource.DataReceived += new Action<object>(DataReceivedHandler);
        }

        /// <summary>
        ///     Number of times to retry sending message after encountering a failure such as an IOException,
        ///     TimeoutException, or a corrupt message.
        /// </summary>
        public int Retries
        {
            get => _retries;
            set => _retries = value;
        }

        /// <summary>
        /// If non-zero, this will cause a second reply to be read if the first is behind the sequence number of the
        /// request by less than this number.  For example, set this to 3, and if when sending request 5, response 3 is
        /// read, we will attempt to re-read responses.
        /// </summary>
        public uint RetryOnOldResponseThreshold { get; set; }

        /// <summary>
        /// If set, Slave Busy exception causes retry count to be used.  If false, Slave Busy will cause infinite retries
        /// </summary>
        public bool SlaveBusyUsesRetryCount { get; set; }

        /// <summary>
        ///     Gets or sets the number of milliseconds the tranport will wait before retrying a message after receiving
        ///     an ACKNOWLEGE or SLAVE DEVICE BUSY slave exception response.
        /// </summary>
        public int WaitToRetryMilliseconds
        {
            get => _waitToRetryMilliseconds;

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("异常");
                }

                _waitToRetryMilliseconds = value;
            }
        }

        /// <summary>
        ///     Gets or sets the number of milliseconds before a timeout occurs when a read operation does not finish.
        /// </summary>
        public int ReadTimeout
        {
            get => StreamResource.ReadTimeout;
            set => StreamResource.ReadTimeout = value;
        }

        /// <summary>
        ///     Gets or sets the number of milliseconds before a timeout occurs when a write operation does not finish.
        /// </summary>
        public int WriteTimeout
        {
            get => StreamResource.WriteTimeout;
            set => StreamResource.WriteTimeout = value;
        }

        /// <summary>
        ///     Gets the stream resource.
        /// </summary>
        public IStreamResource StreamResource => _streamResource;

        protected IFactory Factory { get; }

        /// <summary>
        /// Gets the logger for this instance.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void BufferReset()
        {
            ReceivedBuffer.Position = 0;
            ReceivedBuffer.SetLength(0);
        }

        public void DataReceivedHandler(object sender)
        {
            _doReceive.WaitOne();
            _doReceive.Reset();
            BufferReset();
            DataReceivedToBufferHandler(sender);
            if (ReceiveDataChanged != null)
            {
                ReceiveDataChanged(this);
            }
            _doReceive.Set();
        }

        public abstract void DataReceivedToBufferHandler(object sender);

        public virtual T UnicastMessage<T>(IMessage message)
            where T : IMessage, new()
        {
            IMessage response = null;
            int attempt = 1;
            bool success = false;
            do
            {
                try
                {
                    lock (_syncLock)
                    {
                        Write(message);
                        if (SendDataChanged != null)
                        {
                            SendDataChanged(this, message);
                        }
                        DateTime dateTimeSend = DateTime.Now;
                        bool readAgain;
                        do
                        {
                            readAgain = false;
                            while (ReceivedBuffer.Length == 0)//|| _doReceive.WaitOne()
                            {
                                Thread.Sleep(1);
                                if ((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _streamResource.ReadTimeout)
                                    return default;// throw new TimeoutException();
                            }
                            response = BuildResponseFromBuffer<T>();
                            if (ShouldRetryResponse(message, response))
                            {
                                readAgain = true;
                            }
                            else
                            {
                                BufferReset();
                            }
                        }
                        while (readAgain);
                    }
                    ValidateResponse(message, response);
                    success = true;
                }
                catch (TimeoutException te)
                {
                    if (attempt++ > Retries)
                    {
                        throw te;
                    }
                    Sleep(WaitToRetryMilliseconds);
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
            while (!success);

            return (T)response;
        }

        public void ValidateResponse(IMessage request, IMessage response)
        {
            // message specific validation
            var req = request as IRequest;

            if (req != null)
            {
                req.ValidateResponse(response);
            }

            OnValidateResponse(request, response);
        }
        public bool ShouldRetryResponse(IMessage request, IMessage response)
        {
            return OnShouldRetryResponse(request, response);
        }
        public virtual bool OnShouldRetryResponse(IMessage request, IMessage response)
        {
            return false;
        }

        /// <summary>
        ///     Provide hook to do transport level message validation.
        /// </summary>
        public abstract void OnValidateResponse(IMessage request, IMessage response);

        public abstract IMessage BuildResponseFromBuffer<T>()
            where T : IMessage, new();

        public abstract byte[] BuildMessageFrame(IMessage message);

        public abstract void Write(IMessage message);
        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposableUtility.Dispose(ref _streamResource);
            }
        }

        private static void Sleep(int millisecondsTimeout)
        {
            Task.Delay(millisecondsTimeout).Wait();
        }
    }
}
