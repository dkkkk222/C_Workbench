using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication.Common;
using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;

namespace PPEC.Communication
{
    public sealed class SerialCommChannel : ICommChannel
    {
        private readonly SerialPort _sp;
        private readonly byte[] _rxBuf = new byte[4096];

        public string Name => $"SERIAL:{_sp.PortName}";
        public bool IsConnected => _sp.IsOpen;

        public event EventHandler<ReadOnlyMemory<byte>> BytesReceived;
        public event EventHandler<Exception> ChannelFaulted;

        public SerialCommChannel(string port, int baud = 115200)
        {
            _sp = new SerialPort(port, baud, Parity.None, 8, StopBits.Two)
            {
                ReadBufferSize = 4096,
                WriteBufferSize = 4096
            };

            _sp.DataReceived += OnDataReceived;
            _sp.ErrorReceived += (_, e) =>
                RaiseChannelFaulted(new IOException($"Serial error: {e.EventType}"));
        }

        public Task ConnectAsync(CancellationToken token = default)
        {
            if (IsConnected) return Task.CompletedTask;

            _sp.Open();
            return Task.Delay(50, token);   // 让驱动/MCU 稳定
        }

        public Task DisconnectAsync()
        {
            if (_sp.IsOpen)
                _sp.Close();
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] buffer, int offset = 0, int count = -1,
                              CancellationToken token = default)
        {
            try
            {
                if (count < 0) count = buffer.Length - offset;
                _sp.Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                RaiseChannelFaulted(ex);
                throw;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int n = _sp.Read(_rxBuf, 0, _rxBuf.Length);
                if (n > 0)
                    RaiseBytesReceived(new ReadOnlyMemory<byte>(_rxBuf, 0, n));
            }
            catch (Exception ex)
            {
                RaiseChannelFaulted(ex);
            }
        }

        #region —— 线程安全触发事件 —— 
        private void RaiseBytesReceived(ReadOnlyMemory<byte> data)
        {
            var handler = BytesReceived;
            if (handler != null) handler(this, data);
        }

        private void RaiseChannelFaulted(Exception ex)
        {
            var handler = ChannelFaulted;
            if (handler != null) handler(this, ex);
        }
        #endregion

        public void Dispose() => _sp.Dispose();
    }
    public sealed class AutoReconnectChannel : ICommChannel
    {
        private readonly ICommChannel _inner;
        private readonly TimeSpan _retryInterval;
        private CancellationTokenSource _loopCts;

        public string Name => _inner.Name;
        public bool IsConnected => _inner.IsConnected;

        public event EventHandler<ReadOnlyMemory<byte>> BytesReceived
        {
            add => _inner.BytesReceived += value;
            remove => _inner.BytesReceived -= value;
        }
        public event EventHandler<Exception> ChannelFaulted
        {
            add => _inner.ChannelFaulted += value;
            remove => _inner.ChannelFaulted -= value;
        }

        public AutoReconnectChannel(ICommChannel inner,
                                    TimeSpan? retry = null)
        {
            _inner = inner;
            _retryInterval = retry ?? TimeSpan.FromSeconds(3);

            _inner.ChannelFaulted += (_, __) => _ = RestartLoopAsync();
        }

        public Task ConnectAsync(CancellationToken token = default) =>
            _inner.ConnectAsync(token);

        public Task DisconnectAsync() => _inner.DisconnectAsync();

        public Task SendAsync(byte[] buffer, int offset = 0, int count = -1,
                              CancellationToken token = default) =>
            _inner.SendAsync(buffer, offset, count, token);

        private async Task RestartLoopAsync()
        {
            _loopCts?.Cancel();
            _loopCts = new CancellationTokenSource();

            while (!_loopCts.IsCancellationRequested)
            {
                try
                {
                    await _inner.DisconnectAsync();
                    await _inner.ConnectAsync(_loopCts.Token);
                    if (_inner.IsConnected) return;            // 恢复成功
                }
                catch { /* 记录日志 */ }

                await Task.Delay(_retryInterval, _loopCts.Token);
            }
        }

        public void Dispose()
        {
            _loopCts?.Cancel();
            _inner.Dispose();
        }
    }

    public readonly struct CanRawFrame
    {
        public uint ExtId { get; }
        public byte[] Data { get; }
        public byte Len { get; }

        public CanRawFrame(uint id, byte[] data, byte len)
        {
            ExtId = id;
            Data = data;
            Len = len;
        }

        public ReadOnlyMemory<byte> ToBytes() => new ReadOnlyMemory<byte>(Data, 0, Len);
    }
    public class CanCommChannel : ICommChannel
    {
        private readonly uint _channelIndex;
        private readonly IntPtr _hDev;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _rxTask;

        //private readonly CanDll.CanFrame* _rxBuf;

        public string Name => $"CAN:{_channelIndex}";
        public bool IsConnected { get; private set; }

        public event EventHandler<ReadOnlyMemory<byte>> BytesReceived;
        public event EventHandler<Exception> ChannelFaulted;

        public CanCommChannel(uint channelIndex=0)
        {
            _channelIndex = channelIndex;

            // 打开硬件
            //_hDev = CanDll.OpenDevice(channelIndex);
            if (_hDev == IntPtr.Zero)
                throw new InvalidOperationException("Open CAN failed");

            IsConnected = true;

            // 申请非托管缓存
            //_rxBuf = (CanDll.CanFrame*)System.Runtime.InteropServices.Marshal
            //         .AllocHGlobal(sizeof(CanDll.CanFrame) * 64);

            // 启动接收线程
            _rxTask = Task.Run(ReceiveLoop, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var token = _cts.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    //int count = CanDll.Receive(_hDev, _rxBuf, 64);
                    //if (count > 0)
                    //{
                    //    for (int i = 0; i < count; i++)
                    //    {
                    //        var f = _rxBuf[i];
                    //        var data = new byte[f.Dlc];
                    //        fixed (byte* p = data)
                    //            Buffer.MemoryCopy(f.Data, p, 8, f.Dlc);

                    //        RaiseBytesReceived(
                    //            new CanRawFrame(f.Id, data, f.Dlc).ToBytes());
                    //    }
                    //}
                    await Task.Delay(1, token); // 1 ms 轮询
                }
            }
            catch (Exception ex)
            {
                RaiseChannelFaulted(ex);
            }
        }

        public Task ConnectAsync(CancellationToken token = default) =>
            Task.CompletedTask;            // 打开时已连接

        public Task DisconnectAsync()
        {
            if (!IsConnected) return Task.CompletedTask;

            _cts.Cancel();
            _rxTask.Wait();
            //CanDll.CloseDevice(_hDev);
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] buffer, int offset = 0, int count = -1,
                              CancellationToken token = default)
        {
            // TODO: 根据您的 USB-CAN API 封装 buffer→CanFrame 再发送
            return Task.CompletedTask;
        }

        private void RaiseBytesReceived(ReadOnlyMemory<byte> data)
        {
            var handler = BytesReceived;
            if (handler != null) handler(this, data);
        }
        private void RaiseChannelFaulted(Exception ex)
        {
            var handler = ChannelFaulted;
            if (handler != null) handler(this, ex);
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            //System.Runtime.InteropServices.Marshal.FreeHGlobal(
            //    (IntPtr)_rxBuf);
        }
    }

    public sealed class I2cCommChannel : ICommChannel
    {
        private readonly byte _addr7;
        private readonly object _lock = new object();

        public string Name => "I2C:0x" + _addr7.ToString("X2");
        public bool IsConnected { get; private set; }

        public event EventHandler<ReadOnlyMemory<byte>> BytesReceived;     // 仅在 Read 后回调
        public event EventHandler<Exception> ChannelFaulted;

        public I2cCommChannel(byte addr7) { _addr7 = addr7; }

        public Task ConnectAsync(CancellationToken t = default)
        {
            /* TODO 打开适配器 eg. I2cDll.Open(), set 100 kHz */
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            /* TODO Close */
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] buf, int off = 0, int cnt = -1,
                              CancellationToken t = default)
        {
            if (cnt < 0) cnt = buf.Length - off;
            try
            {
                lock (_lock)
                {
                    /* TODO I2cDll.Write(_addr7, buf+off, cnt) */
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                ChannelFaulted?.Invoke(this, ex);
                throw;
            }
        }

        /* 仅 I²C 需要同步读，无需持续监听总线 */
        public byte[] WriteRead(byte[] write, int readLen)
        {
            var rx = new byte[readLen];
            lock (_lock)
            {
                /* TODO I2cDll.WriteRead(_addr7, write, write.Length, rx, readLen) */
            }
            BytesReceived?.Invoke(this, new ReadOnlyMemory<byte>(rx));
            return rx;
        }

        public void Dispose() { if (IsConnected) DisconnectAsync().Wait(); }
    }
}
