using PPEC.Communication.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class SerialPortAdapter : IStreamResource
    {
        private const string NewLine = "\r\n";
        private SerialPort _serialPort;
        public SerialPort serialPort
        {
            get => _serialPort;
        }
        public SerialPortAdapter(SerialPort serialPort, int bufferSize = 1024)
        {
            Debug.Assert(serialPort != null, "Argument serialPort cannot be null.");

            _serialPort = serialPort;
            _serialPort.NewLine = NewLine;
        }

        public int InfiniteTimeout
        {
            get { return SerialPort.InfiniteTimeout; }
        }

        public int ReadTimeout
        {
            get { return _serialPort.ReadTimeout; }
            set { _serialPort.ReadTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return _serialPort.WriteTimeout; }
            set { _serialPort.WriteTimeout = value; }
        }

        public void DiscardInBuffer()
        {
            _serialPort.DiscardInBuffer();
        }
        public void ClearReceivedBuffer()
        { }
        public int Read(byte[] buffer, int offset, int count)
        {
            //var bufsize = (int)_serialPort.BytesToRead;
            return _serialPort.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _serialPort.Write(buffer, offset, count);
        }

        private static SerialDataReceivedEventHandler ConvertToSerialHandle(Action<object> action)
        {
            return new SerialDataReceivedEventHandler((sender, e) => action(sender));
        }

        public event Action<object> DataReceived
        {
            add
            {
                _serialPort.DataReceived += ConvertToSerialHandle(value);
            }
            remove
            {
                _serialPort.DataReceived -= ConvertToSerialHandle(value);
            }
        }

        public void Connect()
        {
            if (_serialPort == null)
                return;
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void DisConnect()
        {
            if (_serialPort == null)
                return;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public bool IsConnected()
        {
            return _serialPort == null ? false : _serialPort.IsOpen;
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
                _serialPort?.Dispose();
                _serialPort = null;
            }
        }

        public bool ChangeSerialPortInfo(SerialPortConfig config)
        {
            if (_serialPort == null || config == null)
                return false;
            if (_serialPort.IsOpen)
                return false;
            _serialPort.PortName = config.SerialProtName;
            _serialPort.BaudRate = config.BaudRate;
            _serialPort.DataBits = config.DataBits;
            _serialPort.Parity = config.Parity;
            _serialPort.StopBits = config.StopBits;
            _serialPort.WriteBufferSize = config.BufferSize;
            _serialPort.ReadBufferSize = config.BufferSize;
            _serialPort.Handshake = config.FlowControl;
            return true;
        }
    }
}
