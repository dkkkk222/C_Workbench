using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Workbench.Communication
{
    public class SerialPortService : IBaseCommService
    {
        private SerialPort _serialPort;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        /// <summary>
        /// key：十六进制地址，value：十进制数据
        /// </summary>
        private ConcurrentDictionary<string, object> ReceiveCache { get; } = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 数据解析器
        /// </summary>
        public Func<byte[], (string key, object value)> DataParser { get; set; }

        public void Connect(string portName, int baudRate = 115200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            if (IsConnected)
            {
                Close();
            }

            try
            {
                _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    // 设置读写超时，防止无限阻塞
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    Encoding = Encoding.UTF8
                };
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
            }
            catch
            {
                throw;
            }
        }

        public void Close()
        {
            if (!IsConnected) return;

            try
            {
                // 先取消事件订阅，再关闭和释放
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;

                ReceiveCache.Clear();
            }
            catch
            {
                throw;
            }
        }
        public Task<uint?> ReadRegisterAsync(ushort regAddr, bool param1 = false, byte param2 = 0xA0, int timeoutMs = 20)
        {
            return default;
        }
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected)
            {
                return false;
            }
            try
            {
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType != SerialData.Chars) return;

            try
            {
                // 等待一小段时间，确保数据完整接收
                Task.Delay(50).Wait();

                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);

                    if (DataParser != null)
                    {
                        var tuple = DataParser.Invoke(buffer);
                        ReceiveCache.AddOrUpdate(tuple.key, tuple.value, (key, oldValue) => tuple.value);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public uint? Read(string hexAddress)
        {
            bool res = ReceiveCache.TryGetValue(hexAddress, out object value);
            if (!res) return null;
            return (uint)value;
        }
    }
}
