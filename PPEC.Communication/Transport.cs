using PPEC.Communication.Interface;
using PPEC.Communication.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public abstract class Transport : ITransport
    {
        private readonly object _syncLock = new object();
        private int _retries = ConstParams.DefaultRetries;
        private int _waitToRetryMilliseconds = ConstParams.DefaultWaitToRetryMilliseconds;
        private IStreamResource _streamResource;

        public event Action<object> ReceiveDataChanged;

        public event Action<object, IMessage> SendDataChanged;

        /// <summary>
        ///     This constructor is called by the NullTransport.
        /// </summary>
        public Transport(IFactory factory, ILogger logger)
        {
            Factory = factory;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal Transport(IStreamResource streamResource, IFactory factory, ILogger logger)
            : this(factory, logger)
        {
            _streamResource = streamResource ?? throw new ArgumentNullException(nameof(streamResource));
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

                        bool readAgain;
                        do
                        {
                            readAgain = false;
                            response = ReadResponse<T>();

                            var exceptionResponse = response as ExceptionResponse; 

                            if (exceptionResponse != null)
                            {
                                // if SlaveExceptionCode == ACKNOWLEDGE we retry reading the response without resubmitting request
                                readAgain = exceptionResponse.SlaveExceptionCode == ExceptionCodes.Acknowledge;

                                if (readAgain)
                                {
                                    Logger.Debug($"Received ACKNOWLEDGE slave exception response, waiting {_waitToRetryMilliseconds} milliseconds and retrying to read response.");
                                    Sleep(WaitToRetryMilliseconds);
                                }
                                else
                                {
                                    //throw new SlaveException(exceptionResponse);
                                    throw new Exception();
                                }
                            }
                            else if (ShouldRetryResponse(message, response))
                            {
                                readAgain = true;
                            }
                        }
                        while (readAgain);
                    }

                    ValidateResponse(message, response);
                    success = true;
                }
                catch (SlaveException se)
                {
                    if (se.SlaveExceptionCode != ExceptionCodes.SlaveDeviceBusy)
                    {
                        throw;
                    }

                    if (SlaveBusyUsesRetryCount && attempt++ > _retries)
                    {
                        throw;
                    }

                    Logger.Warning($"Received SLAVE_DEVICE_BUSY exception response, waiting {_waitToRetryMilliseconds} milliseconds and resubmitting request.");

                    Sleep(WaitToRetryMilliseconds);
                }
                catch (Exception e)
                {
                    if (e is SocketException || e.InnerException is SocketException)
                    {
                        throw;
                    }
                    else if (e is FormatException ||
                        e is NotImplementedException ||
                        e is TimeoutException ||
                        e is IOException)
                    {
                        Logger.Error($"{e.GetType().Name}, {(_retries - attempt + 1)} retries remaining - {e}");

                        if (attempt++ > _retries)
                        {
                            throw;
                        }

                        Sleep(WaitToRetryMilliseconds);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (!success);

            return (T)response;
        }


        public abstract IMessage CreateResponse<T>(byte[] frame)
            where T : IMessage, new();


        public void ValidateResponse(IMessage request, IMessage response)
        {
            // always check the function code and slave address, regardless of transport protocol
            if (request.FunctionCode != response.FunctionCode)
            {
                string msg = $"Received response with unexpected Function Code. Expected {request.FunctionCode}, received {response.FunctionCode}.";
                throw new IOException(msg);
            }

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
            // These checks are enforced in ValidateRequest, we don't want to retry for these
            if (request.FunctionCode != response.FunctionCode)
            {
                return false;
            }

            if (request.SlaveAddress != response.SlaveAddress)
            {
                return false;
            }

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

        public abstract IMessage ReadResponse<T>()
            where T : IMessage, new();

        //public abstract byte[] BuildMessageFrame(IMessage message);

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
