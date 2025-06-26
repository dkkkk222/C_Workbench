using PPEC.Communication.Config;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class TcpClientAdapter : IStreamResource
    {
        private TcpClientWithListener _tcpClient;
        public SerialPort serialPort { get; }
        public TcpClientAdapter(TcpClient tcpClient)
        {
            _tcpClient = new TcpClientWithListener(tcpClient);
        }
        public TcpClientAdapter(string ipAddress, int port)
        {
            _tcpClient = new TcpClientWithListener(ipAddress, port);
        }
        private int _infiniteTimeout = -1;
        public int InfiniteTimeout => _infiniteTimeout;
        private int _readTimeout = 500;
        public int ReadTimeout
        {
            get => _readTimeout;
            set
            {
                _readTimeout = value;
            }
        }
        private int _writeTimeout = 500;
        public int WriteTimeout
        {
            get => _writeTimeout;
            set
            {
                _writeTimeout = value;
            }
        }

        private static EventHandler<TcpClientDataReceivedEventArgs> ConvertToSerialHandle(Action<object> action)
        {
            return new EventHandler<TcpClientDataReceivedEventArgs>((sender, e) => action(sender));
        }

        public event Action<object> DataReceived
        {
            add
            {
                _tcpClient.DataReceived += ConvertToSerialHandle(value);
            }
            remove
            {
                _tcpClient.DataReceived -= ConvertToSerialHandle(value);
            }
        }
        public void ClearReceivedBuffer()
        {
            _tcpClient.ClearReceivedBuffer();
        }
        public void DiscardInBuffer()
        {
            _tcpClient.DiscardInBuffer();
        }

        public void Connect()
        {
            _tcpClient.Connect();
        }

        public void DisConnect()
        {
            _tcpClient.DisConnect();
        }

        public bool IsConnected()
        {
            return _tcpClient.IsConnected();
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
                _tcpClient?.Dispose();
                _tcpClient = null;
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _tcpClient.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _tcpClient.Write(buffer, offset, count);
        }

        public bool ChangeSerialPortInfo(SerialPortConfig config)
        {
            return true;
        }
    }
}
