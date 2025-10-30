using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using log4net;
using PPEC.Communication.Model;
using Workbench.ViewModels.dw;

namespace Workbench.Communication
{
    public class SerialPortService : IBaseCommService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SerialPortService));
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
        public string Delay { get; set; } = "0";

        private volatile bool _isClosing; // 关闭标记，Close() 时置 true
        private int _recvBusy;            // 防重入
        public void Connect(string portName, int baudRate = 115200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            if (IsConnected)
            {
                Close();
            }
            SerialPort sp = null;
            try
            {
                sp = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    // 设置读写超时，防止无限阻塞
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    Encoding = Encoding.UTF8
                };
                sp.Open();
                sp.ErrorReceived += (s, ev) => _log.Warn($"Serial error: {ev.EventType}");
                sp.DataReceived -= OnDataReceived;
                sp.DataReceived += OnDataReceived;

                _serialPort = sp;
                _isClosing = false;
            }
            catch(Exception ex)
            {
                _log.Error("Serial connect failed", ex);
                try
                {
                    if (sp != null)
                    {
                        try { sp.DataReceived -= OnDataReceived; } catch { }
                        if (sp.IsOpen) try { sp.Close(); } catch { }
                        sp.Dispose();
                    }
                }
                catch { /* ignore */ }
                throw; // 让上层知道连接失败
            }
        }

        public void Close()
        {
            var sp = _serialPort;
            if (sp == null) return;

            _isClosing = true; // 告诉接收线程别再读了
            try
            {
                // 先取消事件订阅，再关闭和释放
                try { sp.DataReceived -= OnDataReceived; } catch { }
                try { sp.ErrorReceived -= null; } catch { } // 无法逐个移除匿名委托，忽略
                try { sp.DiscardInBuffer(); sp.DiscardOutBuffer(); } catch { }
                try { if (sp.IsOpen) sp.Close(); } catch { }
                try { sp.Dispose(); } catch { }
            }
            finally
            {
                _serialPort = null;
                ReceiveCache.Clear();

            }
        }
        public Task<uint?> ReadRegisterAsync(ushort regAddr, bool param1 = false, byte param2 = 0xA0, int timeoutMs = 20)
        {
            return default;
        }
        public async Task<bool> SendAsync(byte[] data)
        {
            var sp = _serialPort;
            if (sp == null || !sp.IsOpen) return false;
           
            try
            {
                await sp.BaseStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType != SerialData.Chars) return;
            if (Interlocked.Exchange(ref _recvBusy, 1) == 1) return; // 防重入
            try
            {
                var sp = _serialPort;                       // 拍快照，避免 race
                if (sp == null || _isClosing || !sp.IsOpen) // 端口已关/正在关：直接返回
                    return;

                // 可留可去：给驱动一点点时间合并数据
                Thread.Sleep(40);

                int bytesToRead;
                try
                {
                    bytesToRead = sp.BytesToRead; // 这里在端口已关时会抛异常
                }
                catch (InvalidOperationException) { return; }   // 端口已关
                catch (IOException) { return; }                 // I/O 错误

                if (bytesToRead <= 0) return;

                var buffer = new byte[bytesToRead];
                int read = 0;
                while (read < bytesToRead)
                {
                    int n;
                    try
                    {
                        n = sp.Read(buffer, read, bytesToRead - read);
                    }
                    catch (TimeoutException) { break; }
                    catch (InvalidOperationException) { return; } // 端口被关闭
                    catch (IOException) { return; }

                    if (n <= 0) break;
                    read += n;
                }
                if (read <= 0) return;

                // 解析也要护一下，避免解析异常把进程拉死
                if (DataParser != null)
                {
                    try
                    {
                        var data = (read == buffer.Length) ? buffer : buffer.Take(read).ToArray();
                        var (key, value) = DataParser.Invoke(data);
                        if (!string.IsNullOrEmpty(key))
                            ReceiveCache.AddOrUpdate(key, value, (_, __) => value);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("DataParser failed", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // 只记录，不要 throw；否则后台线程未处理异常会让进程崩溃
                _log.Warn("OnDataReceived error", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _recvBusy, 0);
            }
        }

        public uint? Read(string hexAddress)
        {
            if (string.IsNullOrWhiteSpace(hexAddress)) return null;
            return ReceiveCache.TryGetValue(hexAddress, out var value)
                ? (value is uint u ? u : ConvertToUIntOrNull(value))
                : (uint?)null;
            static uint? ConvertToUIntOrNull(object v)
            {
                try { return Convert.ToUInt32(v); } catch { return null; }
            }
        }

        public Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 160, int delayMs = 5)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteRegisterAsync(ushort regAddr, uint value4)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Close();
        }

        public Task<ControlAck> SendRemoteControlAsync(byte[] payload, int timeoutMs = 50)
        {
            throw new NotImplementedException();
        }

        public Task<ControlAck> SendInjectionAsync(byte[] payload, int timeoutMs = 80)
        {
            throw new NotImplementedException();
        }
    }
}
