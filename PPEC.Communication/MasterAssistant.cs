using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class MasterAssistant
    {
        private ITransport _transport;
        internal MasterAssistant(ITransport transport)
        {
            _transport = transport;
        }

        /// <summary>
        ///     Gets the Modbus Transport.
        /// </summary>
        public ITransport Transport => _transport;

        #region Write

        public IRequest Send(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false)
        {
            try
            {
                var request = new SerialAssistantRequestResponse(sendBytes, isQuickCommand, IsSendPure);
                return Transport.UnicastMessage<SerialAssistantRequestResponse>(request);

            }
            catch (Exception ex)
            {
                throw ex;
                return null;
            }
        }
        public Task<IRequest> SendAsync(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false)
        {
            try
            {
                return Task.Factory.StartNew(() => Send(sendBytes, isQuickCommand, IsSendPure));
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region Method
        internal static void ValidateNumberOfPoints(string argumentName, ushort numberOfPoints, ushort maxNumberOfPoints)
        {
            if (numberOfPoints < 1 || numberOfPoints > maxNumberOfPoints)
            {
                string msg = $"Argument {argumentName} must be between 1 and {maxNumberOfPoints} inclusive.";
                throw new ArgumentException(msg);
            }
        }
        internal static void ValidateData<T>(string argumentName, T[] data, int maxDataLength)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length == 0 || data.Length > maxDataLength)
            {
                string msg = $"The length of argument {argumentName} must be between 1 and {maxDataLength} inclusive.";
                throw new ArgumentException(msg);
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposableUtility.Dispose(ref _transport);
            }
        }
        #endregion

        #region Connect
        public void Connect()
        {
            _transport.StreamResource.Connect();
        }
        public void DisConnect()
        {
            _transport?.StreamResource.DisConnect();
        }
        public bool IsConnected()
        {
            return _transport.StreamResource.IsConnected();
        }
        #endregion

        #region Event
        public event Action<object> ReceiveDataChanged
        {
            add
            {
                _transport.ReceiveDataChanged += (value);
            }
            remove
            {
                _transport.ReceiveDataChanged -= (value);
            }
        }

        public event Action<object, IMessage> SendDataChanged
        {
            add
            {
                _transport.SendDataChanged += (value);
            }
            remove
            {
                _transport.SendDataChanged -= (value);
            }
        }
        #endregion
    }
}
