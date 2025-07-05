using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Async;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;

namespace PPEC.Communication
{
    public class GroundDevice : IDisposable
    {
        private readonly ConnectPortType _type;
        public readonly ICommChannel _ch;
        private readonly IProtocolHandler _proto;
        public readonly BlockingCollection<IUartMessage> _q = new BlockingCollection<IUartMessage>();
        public readonly BlockingCollection<ReadOnlyMemory<byte>> _rawQueue = new BlockingCollection<ReadOnlyMemory<byte>>();
        //public readonly BlockingCollection<InvRealtimeMessage> _rawQueue = new BlockingCollection<InvRealtimeMessage>();
        private readonly Task _loop;

        private readonly byte _i2cAddr;            // I²C 专用

        public GroundDevice(string id, ConnectPortType type, int baud = 256000)
        {
            _type = type;
            _ch = ChannelFactory.Create(type, id, baud);
            _proto = ProtocolFactory.Create(type);

            //_ch.MessageParsed += OnMessageParsed;
            Task.Run(() =>
            {
                foreach (var raw in _rawQueue.GetConsumingEnumerable())
                {
                    _proto.Feed(raw.Span); // 这里做协议帧解析
                }
            });
            _ch.BytesReceived += (_, m) =>
            {
                _rawQueue.Add(m);
                //_proto.Feed(m.Span);
            };
            _proto.MessageParsed += (_, msg) =>
            {
                _q.Add(msg);
            };

            if (type == ConnectPortType.I2C)
                _i2cAddr = byte.Parse(id);         // id 传的就是 7bit 地址

            //_loop = Task.Run(ProcessLoop);
        }
        private void OnMessageParsed(object sender, IUartMessage msg)
        {
            // **一定要回到 UI 线程再修改绑定属性**
            //if(msg is InvRealtimeMessage msg1)
            //{
            //    _rawQueue.Add(msg1);                
            //}

        }
        public Task ConnectAsync() => _ch.ConnectAsync();

        public Task ResetAsync()
        {
            switch (_type)
            {
                case ConnectPortType.UART: return _ch.SendAsync(UartCommandBuilder.BuildReset());
                //case ConnectPortType.CAN: return _ch.SendAsync(CanBuilder.Reset(_i2cAddr/*占位*/));
                //case ConnectPortType.I2C: return _ch.SendAsync(I2cBuilder.Reset());
                default: throw new NotSupportedException();
            }
        }

        public async Task<uint> ReadRegAsync(ushort reg, int timeout = 100)
        {
            switch (_type)
            {
                case ConnectPortType.UART:
                    return await ReadViaQueueAsync(UartCommandBuilder.BuildReadReg(reg), "R" + reg, timeout);

                //case ConnectPortType.CAN:
                //    return await ReadViaQueueAsync(CanBuilder.Read(reg, _i2cAddr), "R" + reg, timeout);

                //case ConnectPortType.I2C:
                //    var i2cbus = (I2cCommChannel)_ch;
                //    i2cbus.SendAsync(I2cBuilder.ReadAddr(reg)).Wait();   // 写寄存器地址
                //    byte[] rx = i2cbus.WriteRead(new byte[0], 4);        // 读 4 字节
                //    return BeConv.ReadU32(rx, 0);

                default: throw new NotSupportedException();
            }
        }

        public Task WriteRegAsync(ushort reg, uint val, byte[] byteVal = null)
        {
            switch (_type)
            {
                case ConnectPortType.UART: return _ch.SendAsync(UartCommandBuilder.BuildWriteReg(reg, val));
                case ConnectPortType.INV: return _ch.SendAsync(byteVal);
                //case ConnectPortType.CAN: return _ch.SendAsync(CanBuilder.Write(reg, val, _i2cAddr));
                //case ConnectPortType.I2C: return _ch.SendAsync(I2cBuilder.Write(reg, val));
                default: throw new NotSupportedException();
            }
        }

        // 公共等待函数（UART/CAN 用）
        private Task<uint> ReadViaQueueAsync(byte[] cmd, string key, int to)
        {
            var tcs = new TaskCompletionSource<IUartMessage>();
            _pending[key] = tcs;
            _ch.SendAsync(cmd).Wait();

            var cts = new CancellationTokenSource(to);
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
                return tcs.Task.ContinueWith(t => ((ReadRegisterResponse)t.Result).Value);
        }

        // 后台处理 (与之前相同)
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IUartMessage>> _pending
            = new ConcurrentDictionary<string, TaskCompletionSource<IUartMessage>>();
        private readonly ConcurrentDictionary<ushort, RegValue> _regCache
        = new ConcurrentDictionary<ushort, RegValue>();
        //_registerMeta 可以是事先定义的 寄存器字典：Dictionary<ushort, byte>，告诉系统这个地址是 16 bit 还是 32 bit。
        //通过解析EXCEL得到的寄存器信息
        public ConcurrentDictionary<ushort, byte> _registerMeta = new ConcurrentDictionary<ushort, byte>();
        /// <summary>
        /// 这里收到数据
        /// </summary>
        private void ProcessLoop()
        {
            foreach (var m in _q.GetConsumingEnumerable())
            {
                if (m is ReadRegisterResponse rsp)
                {
                    var width = _registerMeta.TryGetValue(rsp.Address, out var w) ? w : (byte)32;
                    _regCache[rsp.Address] = new RegValue
                    {
                        Address = rsp.Address,
                        Raw = rsp.Value,
                        Width = width
                    };
                    TaskCompletionSource<IUartMessage> tcs;
                    if (_pending.TryRemove("R" + rsp.Address, out tcs))
                    {
                        tcs.TrySetResult(m);
                    }
                }
                // … 可在此更新缓存/UI
            }
        }

        public void Dispose() { _q.CompleteAdding(); _loop.Wait(); _ch.Dispose(); }
    }

    public class DeviceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, GroundDevice> _devs = new ConcurrentDictionary<string, GroundDevice>();

        public async Task AddDeviceAsync(string port, ConnectPortType type, int baud)
        {
            var dev = new GroundDevice(port, type, baud);
            await dev.ConnectAsync();
            _devs[port] = dev;
        }

        public async Task ResetAllAsync()
        {
            var tasks = new List<Task>();
            foreach (var d in _devs.Values) tasks.Add(d.ResetAsync());
            await Task.WhenAll(tasks);
        }

        public Task<uint> ReadRegAsync(string port, ushort addr) => _devs[port].ReadRegAsync(addr);

        public void Dispose()
        {
            foreach (var d in _devs.Values) d.Dispose();
        }
    }
}
