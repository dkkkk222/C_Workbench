using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace PPEC.Communication
{
    public class TcpClientWithListener : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        public byte[] ReceivedBuffer = new byte[1024];
        public int _byteToRead = 0;
        private object _syncLock = new object();
        public event EventHandler<TcpClientDataReceivedEventArgs> DataReceived;

        public TcpClientWithListener(string ipAddress, int port)
        {
            // 创建TCP Client对象
            _tcpClient = new TcpClient(ipAddress, port);
            _stream = _tcpClient.GetStream();
            _stream.BeginRead(ReceivedBuffer, 0, ReceivedBuffer.Length, new AsyncCallback(OnDataReceived), null);
        }

        public TcpClientWithListener(TcpClient tcpClient)
        {
            // 创建TCP Client对象
            _tcpClient = tcpClient ?? throw new Exception("tcpClient cannot be null");
            _stream = _tcpClient.GetStream();
            _stream.BeginRead(ReceivedBuffer, 0, ReceivedBuffer.Length, new AsyncCallback(OnDataReceived), null);
        }

        private void OnDataReceived(IAsyncResult result)
        {
            try
            {
                lock (_syncLock)
                {
                    _byteToRead = _stream.EndRead(result);
                }
                if (DataReceived != null && _byteToRead != 0)
                {
                    byte[] tempBuff = new byte[_byteToRead];
                    Array.Copy(ReceivedBuffer, tempBuff, _byteToRead);
                    DataReceived(this, new TcpClientDataReceivedEventArgs(tempBuff, _byteToRead));
                }
                _stream.BeginRead(ReceivedBuffer, 0, ReceivedBuffer.Length, new AsyncCallback(OnDataReceived), null);
            }
            catch (Exception e)
            {
                if (_tcpClient != null)
                {
                    try
                    {
                        _stream.BeginRead(ReceivedBuffer, 0, ReceivedBuffer.Length, new AsyncCallback(OnDataReceived), null);
                    }
                    catch (Exception ex)
                    {
                        return;
                    }
                }
            }
        }
        public void ClearReceivedBuffer()
        {
            ReceivedBuffer = new byte[1024];
            _byteToRead = 0;
        }
        public void DiscardInBuffer()
        {
            _stream.Flush();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_syncLock)
            {
                return _stream.Read(buffer, offset, count);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            lock (_syncLock)
            {
                _stream.Write(buffer, offset, count);
            }
        }
        public void Connect()
        {
        }

        public void DisConnect()
        {
            if (_tcpClient == null)
                return;
            _tcpClient.GetStream().Close();
            _tcpClient.Close();
            _tcpClient = null;
        }

        public bool IsConnected()
        {
            return _tcpClient == null ? false : _tcpClient.Connected;
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
                DisposableUtility.Dispose(ref _tcpClient);
            }
        }

    }
}
