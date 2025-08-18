using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication;
using Workbench.Models;

namespace Workbench.Communication
{
    /// <summary>
    /// 周立功 CAN 实现，接口风格对齐 I2c/Serial 版本
    /// 端口字符串格式：
    ///   "CAN:<DevType>:<DevId>:<CanId>:<BaudIndex>"
    ///   例："CAN:21:0:0:1" -> USBCAN-2E-U, Dev0, CAN0, 500kbps(index=1)
    /// </summary>
    public class CanCommService : IBaseCommService
    {
        private ControlCAN _can;
        private CANDevice _dev; // 保存创建参数
        private CancellationTokenSource _cts;
        private Task _rxLoop;
        private CanConnectOptions _opts;

        public bool IsConnected => _can != null;

        /// <summary> key：4位HEX寄存器地址（比如 "0170"）；value：解析后的十进制数据（uint 或其它 object） </summary>
        private readonly ConcurrentDictionary<string, object> _cache = new();

        /// <summary> 可选自定义解析器（把 VCI_CAN_OBJ -> (key,value)）；不设置则用默认协议解析 </summary>
        public Func<VCI_CAN_OBJ, (string key, object value)?> FrameParser { get; set; }

        // ====== 公共：Connect / Close / Dispose / Send / Read ======

        public void Connect(CanConnectOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (IsConnected) Close();

            _opts = options;

            _dev = new CANDevice(options.DevType, options.DevId, options.CanId, options.BaudIndex);
            _can = new ControlCAN(_dev);

            if (!_can.OpenCAN()) throw new Exception("VCI_OpenDevice 失败");
            if (!_can.StartCan(options.CanId)) throw new Exception("VCI_StartCAN 失败");

            if (options.SendTimeoutMs.HasValue)
                _can.SetSendTimeout(options.SendTimeoutMs.Value);

            _cts = new CancellationTokenSource();
            _rxLoop = Task.Run(() => RxLoopAsync(_cts.Token));
        }

        public void Connect(string portName, int baudRate = 0, System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
                        int dataBits = 8, System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One)
        {
            if (IsConnected) Close();
            if (string.IsNullOrWhiteSpace(portName) || !portName.StartsWith("CAN", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("请传 CAN 连接串，示例：CAN:21:0:0:1 或 CAN;DevType=21;DevId=0;CanId=0;Baud=1", nameof(portName));

            CanConnectOptions opt;
            if (portName.Contains(":")) // 旧格式
            {
                var parts = portName.Split(':');
                if (parts.Length != 5) throw new ArgumentException("格式应为 CAN:<DevType>:<DevId>:<CanId>:<BaudIndex>");
                opt = new CanConnectOptions
                {
                    DevType = uint.Parse(parts[1], CultureInfo.InvariantCulture),
                    DevId = uint.Parse(parts[2], CultureInfo.InvariantCulture),
                    CanId = uint.Parse(parts[3], CultureInfo.InvariantCulture),
                    BaudIndex = int.Parse(parts[4], CultureInfo.InvariantCulture)
                };
            }
            else // kv 格式
            {
                // CAN;DevType=21;DevId=0;CanId=1;Baud=1;Timeout=400
                var kv = portName.Split(';').Skip(1)
                                 .Select(s => s.Split('='))
                                 .Where(p => p.Length == 2)
                                 .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());

                uint devType = uint.Parse(kv["devtype"]);
                uint devId = uint.Parse(kv["devid"]);
                uint canId = uint.Parse(kv["canid"]);
                int baudIdx = int.Parse(kv["baud"]);
                uint? timeout = kv.TryGetValue("timeout", out var t) ? uint.Parse(t) : (uint?)null;

                opt = new CanConnectOptions { DevType = devType, DevId = devId, CanId = canId, BaudIndex = baudIdx, SendTimeoutMs = timeout };
            }

            Connect(opt); // 复用强类型版本
        }

        public void Close()
        {
            try
            {
                _cts?.Cancel();
                try { _rxLoop?.Wait(200); } catch { }
                _cts = null; _rxLoop = null;

                _can?.CloseCAN();
            }
            finally
            {
                _can = null;
                _dev = default;
                _opts = null;
                _cache.Clear();
            }
        }

        public void Dispose() => Close();

        public void SwitchChannel(uint newCanId)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            if (_opts.CanId == newCanId) return;

            // 停当前通道，切到新通道
            _can.CloseCAN(); // 保险起见全关后重开
            _opts.CanId = newCanId;
            _dev = new CANDevice(_opts.DevType, _opts.DevId, _opts.CanId, _opts.BaudIndex);
            _can = new ControlCAN(_dev);
            if (!_can.OpenCAN()) throw new Exception("VCI_OpenDevice 失败");
            if (!_can.StartCan(_opts.CanId)) throw new Exception("VCI_StartCAN 失败");
            if (_opts.SendTimeoutMs.HasValue) _can.SetSendTimeout(_opts.SendTimeoutMs.Value);
        }

        public void SwitchBaudIndex(int baudIndex)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            if (_opts.BaudIndex == baudIndex) return;

            _opts.BaudIndex = baudIndex;
            // 简单策略：重启 CAN 使波特率生效（你的 ControlCAN.StartCan 内部已按 DevType 分支 SetReference 波特率）
            _can.CloseCAN();
            _dev = new CANDevice(_opts.DevType, _opts.DevId, _opts.CanId, _opts.BaudIndex);
            _can = new ControlCAN(_dev);
            if (!_can.OpenCAN()) throw new Exception("VCI_OpenDevice 失败");
            if (!_can.StartCan(_opts.CanId)) throw new Exception("VCI_StartCAN 失败");
            if (_opts.SendTimeoutMs.HasValue) _can.SetSendTimeout(_opts.SendTimeoutMs.Value);
        }

        private bool UseCanBDefault() => _opts != null && _opts.CanId == 1;

        /// <summary>
        /// 裸发：把 data 填入一帧的 Data[]；ID/标志位请由上层通过 SetLastTxId/ExternFlag/RemoteFlag 先设置
        /// 实际工程里通常不会直接用本方法；建议用 Reset/Read/Write 协议级 API
        /// </summary>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected || data == null) return false;
            // 使用最近一次设置的 ID/标志位（或填默认）
            var frame = new VCI_CAN_OBJ
            {
                ID = _lastTxId,
                ExternFlag = 1,
                RemoteFlag = 0,
                SendType = 0,
                TimeFlag = 0,
                TimeStamp = 0,
                DataLen = (byte)Math.Min(8, data.Length),
                Data = Pad8(data),
                Reserved = new byte[3]
            };
            return await Task.Run(() => _can.Transmit(frame));
        }

        public uint? Read(string hexAddress)
        {
            if (string.IsNullOrWhiteSpace(hexAddress)) return null;
            if (_cache.TryGetValue(hexAddress.ToUpperInvariant(), out var val))
            {
                if (val is uint u) return u;
                try { return Convert.ToUInt32(val); } catch { return null; }
            }
            return null;
        }

        public bool TryGetCached(string hexAddress, out object value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(hexAddress)) return false;
            return _cache.TryGetValue(hexAddress.ToUpperInvariant(), out value);
        }

        // ====== 协议级 API：Reset / ReadRegister / WriteRegister ======

        /// <summary> 复位指令（见协议 4.2）：SUM=(DT+3C+FF)%256；无应答 </summary>
        public async Task<bool> ResetAsync(bool useCanB = false, byte dest = 0xA0, int delayMs = 20)
        {
            bool busB = useCanB ? useCanB: UseCanBDefault();
            EnsureConnected();

            uint id = ArbId.Build(busB, InfoType.ResetCmd, DT.Reset, src: 0x00, dst: dest);
            _lastTxId = id;

            byte dl = 0x02;
            byte dt = DT.Reset;
            byte w0 = 0x3C, w1 = 0xFF;
            byte sum = Sum8(dt, w0, w1);

            var data = new byte[] { dl, dt, w0, w1, sum };
            bool ok = await SendAsync(data);
            if (ok && delayMs > 0) await Task.Delay(delayMs);
            return ok;
        }

        /// <summary> 读寄存器（4.3 → 4.4 两帧应答），返回 4 字节原始数据；也会写入缓存 key=寄存器4位HEX </summary>
        //public async Task<byte[]> ReadRegisterAsync(ushort regAddr, bool useCanB = false, byte dest = 0xA0, int timeoutMs = 20)
        //{
        //    bool busB = useCanB ? useCanB : UseCanBDefault();
        //    EnsureConnected();

        //    uint reqId = ArbId.Build(busB, InfoType.ReadCmd, DT.Read, 0x00, dest);
        //    _lastTxId = reqId;

        //    byte dl = 0x02;
        //    byte dt = DT.Read;
        //    byte w0 = (byte)((regAddr >> 8) & 0xFF);
        //    byte w1 = (byte)(regAddr & 0xFF);
        //    byte sum = Sum8(dt, w0, w1);

        //    var cmd = new byte[] { dl, dt, w0, w1, sum };
        //    if (!await SendAsync(cmd)) throw new Exception("读命令发送失败");

        //    // 等应答（两帧）：InfoType=ReadReply，DT=0x3F，SRC=dest，DST=0x00，总线与请求一致
        //    uint repId = ArbId.Build(useCanB, InfoType.ReadReply, DT.Reply, src: dest, dst: 0x00);
        //    var (f0, f1) = await WaitTwoFramesAsync(repId, timeoutMs);

        //    // 解析
        //    ParseReplyTwoFrames(f0, f1, out var addrHi, out var addrLo, out var data4, out var sumRx);

        //    // 校验
        //    var calc = Sum8(DT.Reply, addrHi, addrLo, data4[0], data4[1], data4[2], data4[3]);
        //    if (calc != sumRx) throw new Exception($"SUM错误，应答={sumRx:X2}, 计算={calc:X2}");

        //    ushort addrRet = (ushort)((addrHi << 8) | addrLo);
        //    if (addrRet != regAddr) throw new Exception($"寄存器地址不一致，期望0x{regAddr:X4} 实际0x{addrRet:X4}");

        //    var receiveData = Parse(data4);
        //    _cache.AddOrUpdate(receiveData.Item1, receiveData.Item2, (key, oldValue) => receiveData.Item2);
        //    // 缓存：key=寄存器地址4位HEX，value=uint
        //    //uint value = ((uint)data4[0] << 24) | ((uint)data4[1] << 16) | ((uint)data4[2] << 8) | data4[3];
        //    //var key = regAddr.ToString("X4");
        //    //_cache.AddOrUpdate(key, value, (_, __) => value);

        //    return data4;
        //}
        public async Task<uint?> ReadRegisterAsync(ushort regAddr, bool useCanB = false, byte dest = 0xA0, int timeoutMs = 20)
        {
            bool busB = useCanB ? useCanB : UseCanBDefault();
            EnsureConnected();

            uint reqId = ArbId.Build(busB, InfoType.ReadCmd, DT.Read, 0x00, dest);
            _lastTxId = reqId;

            byte dl = 0x02;
            byte dt = DT.Read;
            byte w0 = (byte)((regAddr >> 8) & 0xFF);
            byte w1 = (byte)(regAddr & 0xFF);
            byte sum = Sum8(dt, w0, w1);

            var cmd = new byte[] { dl, dt, w0, w1, sum };
            if (!await SendAsync(cmd)) throw new Exception("读命令发送失败");

            // 等应答（两帧）：InfoType=ReadReply，DT=0x3F，SRC=dest，DST=0x00，总线与请求一致
            //uint repId = ArbId.Build(useCanB, InfoType.ReadReply, DT.Reply, src: dest, dst: 0x00);
            //var (f0, f1) = await WaitTwoFramesAsync(repId, timeoutMs);

            //// 解析
            //ParseReplyTwoFrames(f0, f1, out var addrHi, out var addrLo, out var data4, out var sumRx);

            //// 校验
            //var calc = Sum8(DT.Reply, addrHi, addrLo, data4[0], data4[1], data4[2], data4[3]);
            //if (calc != sumRx) throw new Exception($"SUM错误，应答={sumRx:X2}, 计算={calc:X2}");

            //ushort addrRet = (ushort)((addrHi << 8) | addrLo);
            //if (addrRet != regAddr) throw new Exception($"寄存器地址不一致，期望0x{regAddr:X4} 实际0x{addrRet:X4}");

            //var receiveData = Parse(data4);
            //_cache.AddOrUpdate(receiveData.Item1, receiveData.Item2, (key, oldValue) => receiveData.Item2);
            // 缓存：key=寄存器地址4位HEX，value=uint
            //uint value = ((uint)data4[0] << 24) | ((uint)data4[1] << 16) | ((uint)data4[2] << 8) | data4[3];
            //var key = regAddr.ToString("X4");
            //_cache.AddOrUpdate(key, value, (_, __) => value);

            return 0;
        }
        public (string, object) Parse(byte[] data)
        {
            string hex = Utility.ToHexString(data);

            byte[] addressBytes = new byte[2];
            Array.Copy(data, 16, addressBytes, 0, 2);
            string addressHex = Utility.ToHexString(addressBytes);

            byte[] dataBytes = new byte[4];
            Array.Copy(data, 18, dataBytes, 0, 4);
            string dataStr = Utility.ToHexString(dataBytes);
            var decValue = Utility.ParseHexToUInt(dataStr);

            return (addressHex, decValue);
        }
        /// <summary> 写寄存器（4.5 两帧发送，不要求应答） </summary>
        public async Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 0xA0, int delayMs = 5)
        {
            bool busB = useCanB ? useCanB : UseCanBDefault();
            EnsureConnected();
            if (value4 == null || value4.Length != 4) throw new ArgumentException("value4 必须为4字节");

            uint id = ArbId.Build(busB, InfoType.WriteCmd, DT.Write, 0x00, dest);
            _lastTxId = id;

            byte dl = 0x06;
            byte dt = DT.Write;
            byte w0 = (byte)((regAddr >> 8) & 0xFF);
            byte w1 = (byte)(regAddr & 0xFF);
            byte seq0 = 0x00, seq1 = 0x01;
            byte sum = Sum8(dt, w0, w1, value4[0], value4[1], value4[2], value4[3]);

            // 帧1：[Seq0][DL=06][DT=4F][W0][W1][W2][W3][W4]
            var f1 = new byte[] { seq0, dl, dt, w0, w1, value4[0], value4[1], value4[2] };
            // 帧2：[Seq1][W5][SUM]
            var f2 = new byte[] { seq1, value4[3], sum };

            // 连续发送两帧
            var ok = await Task.Run(() => _can.Transmit(new[]
            {
                MakeTxFrame(id, f1),
                MakeTxFrame(id, f2)
            }));
            if (!ok) throw new Exception("写命令发送失败");

            if (delayMs > 0) await Task.Delay(delayMs);

            // 可选：写入缓存
            uint u = ((uint)value4[0] << 24) | ((uint)value4[1] << 16) | ((uint)value4[2] << 8) | value4[3];
            var key = regAddr.ToString("X4");
            _cache.AddOrUpdate(key, u, (_, __) => u);
        }

        // ====== 内部实现 ======

        // 最近一次设置的发送 ID（给 SendAsync 用）
        private uint _lastTxId = 0;

        private static byte[] Pad8(byte[] src)
        {
            var b = new byte[8];
            Array.Clear(b, 0, b.Length);
            Array.Copy(src, b, Math.Min(8, src.Length));
            return b;
        }

        private VCI_CAN_OBJ MakeTxFrame(uint id, byte[] data)
        {
            return new VCI_CAN_OBJ
            {
                ID = id,
                TimeFlag = 0,
                TimeStamp = 0,
                SendType = 0,
                RemoteFlag = 0,
                ExternFlag = 1,
                DataLen = (byte)Math.Min(8, data.Length),
                Data = Pad8(data),
                Reserved = new byte[3]
            };
        }

        private void EnsureConnected()
        {
            if (!IsConnected) throw new InvalidOperationException("CAN 未连接");
        }

        /// <summary>
        /// 接收循环：VCI_Receive 轮询，把帧交给 FrameParser 或默认协议解析，落地到 _cache
        /// </summary>
        private async Task RxLoopAsync(CancellationToken token)
        {
            // 用于组装两帧应答：key=完整应答ID(含总线/类型/DT/src/dst)，value=第一帧
            var halfReplies = new ConcurrentDictionary<uint, VCI_CAN_OBJ>();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var list = _can.Receive(50, 100); // 你在 ControlCAN 里已有 Receive(max, waitMs)
                    foreach (var f in list)
                    {
                        // 用户自定义解析器优先
                        if (FrameParser != null)
                        {
                            var res = FrameParser(f);
                            if (res.HasValue)
                            {
                                var (k, v) = res.Value;
                                if (!string.IsNullOrEmpty(k))
                                    _cache.AddOrUpdate(k.ToUpperInvariant(), v, (_, __) => v);
                                continue;
                            }
                        }

                        // 默认解析：只关心“读寄存器应答”的两帧，合并后写缓存
                        if (f.ExternFlag == 1 && f.RemoteFlag == 0)
                        {
                            // 只看 InfoType=0x03、DT=0x3F、DST=0x00 的帧
                            var (bus, mt, dt, src, dst) = ArbId.Parse(f.ID);
                            if (mt == InfoType.ReadReply && dt == DT.Reply && dst == 0x00)
                            {
                                if (f.DataLen == 8 && f.Data[0] == 0x00)
                                {
                                    // 第一帧
                                    halfReplies[f.ID] = f;
                                }
                                else if (f.DataLen == 3 && f.Data[0] == 0x01)
                                {
                                    // 第二帧
                                    if (halfReplies.TryRemove(f.ID, out var f0))
                                    {
                                        ParseReplyTwoFrames(f0, f, out var ah, out var al, out var d4, out var sum);
                                        // 校验
                                        var calc = Sum8(DT.Reply, ah, al, d4[0], d4[1], d4[2], d4[3]);
                                        if (calc == sum)
                                        {
                                            var key = $"{ah:X2}{al:X2}";
                                            uint val = ((uint)d4[0] << 24) | ((uint)d4[1] << 16) | ((uint)d4[2] << 8) | d4[3];
                                            _cache.AddOrUpdate(key, val, (_, __) => val);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略瞬时异常
                }

                await Task.Delay(1, token);
            }
        }

        private static void ParseReplyTwoFrames(VCI_CAN_OBJ f0, VCI_CAN_OBJ f1,
            out byte addrHi, out byte addrLo, out byte[] data4, out byte sum)
        {
            if (f0.DataLen != 8 || f0.Data[0] != 0x00) throw new Exception("应答帧1格式错误");
            if (f1.DataLen != 3 || f1.Data[0] != 0x01) throw new Exception("应答帧2格式错误");

            byte dl = f0.Data[1];
            byte dt = f0.Data[2];
            if (dl != 0x06 || dt != DT.Reply) throw new Exception("应答DL/DT不匹配");

            addrHi = f0.Data[3];
            addrLo = f0.Data[4];
            data4 = new byte[] { f0.Data[5], f0.Data[6], f0.Data[7], f1.Data[1] };
            sum = f1.Data[2];
        }

        private async Task<(VCI_CAN_OBJ f0, VCI_CAN_OBJ f1)> WaitTwoFramesAsync(uint expectId, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            VCI_CAN_OBJ? first = null, second = null;

            while (DateTime.UtcNow < deadline)
            {
                // 先尝试从当前循环缓存再拉
                foreach (var it in _can.Receive(50, 100))
                {
                    if (it.ID != expectId || it.ExternFlag != 1 || it.RemoteFlag != 0) continue;
                    if (it.DataLen == 8 && it.Data[0] == 0x00) first = it;
                    else if (it.DataLen == 3 && it.Data[0] == 0x01) second = it;

                    if (first.HasValue && second.HasValue) return (first.Value, second.Value);
                }
                await Task.Delay(1);
            }

            throw new TimeoutException("等待两帧应答超时");
        }

        // ====== 协议工具 ======

        private static byte Sum8(params byte[] bytes)
        {
            int s = 0; foreach (var b in bytes) s = (s + b) & 0xFF;
            return (byte)s;
        }

        private static class DT
        {
            public const byte Reset = 0x1F;
            public const byte Read = 0x2F;
            public const byte Reply = 0x3F;
            public const byte Write = 0x4F;
        }

        private enum InfoType : byte
        {
            ResetCmd = 0x01,
            ReadCmd = 0x02,
            ReadReply = 0x03,
            WriteCmd = 0x04,
        }

        /// <summary> 按协议把 29bit 扩展 ID 拼出来： [LT(1)][MT(4)][DT(8)][SRC(8)][DST(8)] </summary>
        private static class ArbId
        {
            public static uint Build(bool useCanB, InfoType mt, byte dt, byte src, byte dst)
            {
                uint id = 0;
                id |= (useCanB ? 1u : 0u) << 28;       // LT
                id |= ((uint)mt & 0xF) << 24;          // MT
                id |= ((uint)dt) << 16;                // DT
                id |= ((uint)src) << 8;                // SRC
                id |= dst;                              // DST
                return id;
            }

            public static (bool busB, InfoType mt, byte dt, byte src, byte dst) Parse(uint id)
            {
                bool b = ((id >> 28) & 0x1) == 1;
                var mt = (InfoType)((id >> 24) & 0xF);
                byte dt = (byte)((id >> 16) & 0xFF);
                byte src = (byte)((id >> 8) & 0xFF);
                byte dst = (byte)(id & 0xFF);
                return (b, mt, dt, src, dst);
            }
        }
    }
}
