using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication;
using Workbench.Models;

namespace Workbench.Communication
{
    /// <summary>
    /// 使用 zlgcan.dll 的新版 CAN 通信服务（接口保持对齐你原先的 I2C/Serial 版本）
    /// 端口字符串格式兼容旧版：
    ///   "CAN:<DevType>:<DevId>:<CanId>:<BaudIndex>"
    ///   例："CAN:21:0:0:1" -> USBCAN-2E-U, Dev0, CAN0, 500kbps(index=1)
    ///   也支持 KV 格式：
    ///   "CAN;DevType=21;DevId=0;CanId=0;Baud=1;Timeout=400"
    /// </summary>
    public class CanCommService1 : IBaseCommService, IDisposable
    {
        // ====== 设备/通道句柄（zlgcan） ======
        private IntPtr _devHandle = IntPtr.Zero;
        private readonly Dictionary<uint, IntPtr> _chnHandles = new(); // key = CanId(0/1/...)
        private uint _currentCanId = 0;

        private CancellationTokenSource _cts;
        private Task _rxLoop;

        private CanConnectOptions _opts;

        public bool IsConnected => _devHandle != IntPtr.Zero && _chnHandles.TryGetValue(_currentCanId, out var h) && h != IntPtr.Zero;

        /// <summary> key：4位HEX寄存器地址（比如 "0170"）；value：解析后的十进制数据（uint 或其它 object） </summary>
        private readonly ConcurrentDictionary<string, object> _cache = new();

        /// <summary> 可选自定义解析器（把 接收到的一帧 -> (key,value)）；不设置则用默认协议解析（两帧读应答合并） </summary>
        public Func<ZLGCAN.ZCAN_Receive_Data, (string key, object value)?> FrameParser { get; set; }

        // 最近一次设置的发送 ID（给 SendAsync 用；ID 是 29bit 原始ID，不含 EFF/RTR/ERR 标志位）
        private uint _lastTxRawId = 0;

        // ====== 公共：Connect / Close / Dispose / Send / Read ======

        public void Connect(CanConnectOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (IsConnected) Close();

            _opts = options;

            // 1) 打开设备（dev_index 用 DevId 对应第几个设备）
            _devHandle = ZLGCAN.ZCAN_OpenDevice(_opts.DevType, _opts.DevId, 0);
            if (_devHandle == IntPtr.Zero) throw new Exception("ZCAN_OpenDevice 失败");

            // 2) 初始化/打开通道
            _currentCanId = _opts.CanId;
            var ch = StartChannel(_currentCanId, _opts.BaudIndex);
            if (ch == IntPtr.Zero) throw new Exception("ZCAN_InitCAN/ZCAN_StartCAN 失败");

            // 3) 接收循环
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

                // 关闭通道（Reset）
                foreach (var kv in _chnHandles)
                {
                    if (kv.Value != IntPtr.Zero)
                        ZLGCAN.ZCAN_ResetCAN(kv.Value);
                }
                _chnHandles.Clear();

                if (_devHandle != IntPtr.Zero)
                {
                    ZLGCAN.ZCAN_CloseDevice(_devHandle);
                }
            }
            finally
            {
                _devHandle = IntPtr.Zero;
                _opts = null;
                _cache.Clear();
            }
        }

        public void Dispose() => Close();

        public void SwitchChannel(uint newCanId)
        {
            EnsureConnectedDevice();
            if (_currentCanId == newCanId) return;

            // 当前通道 reset
            if (_chnHandles.TryGetValue(_currentCanId, out var old) && old != IntPtr.Zero)
            {
                ZLGCAN.ZCAN_ResetCAN(old);
            }

            // 切换到新通道（若没初始化则初始化）
            var ch = StartChannel(newCanId, _opts?.BaudIndex ?? 1);
            if (ch == IntPtr.Zero) throw new Exception("切换通道失败（Init/StartCAN）");

            _currentCanId = newCanId;
        }

        public void SwitchBaudIndex(int baudIndex)
        {
            EnsureConnectedDevice();
            if (_opts != null && _opts.BaudIndex == baudIndex) return;

            // 重新配置当前通道波特率：reset -> set baud -> init/start
            if (_chnHandles.TryGetValue(_currentCanId, out var h) && h != IntPtr.Zero)
            {
                ZLGCAN.ZCAN_ResetCAN(h);
                _chnHandles.Remove(_currentCanId);
            }

            var ch = StartChannel(_currentCanId, baudIndex);
            if (ch == IntPtr.Zero) throw new Exception("重设波特率失败（Init/StartCAN）");

            if (_opts != null) _opts.BaudIndex = baudIndex;
        }

        private bool UseCanBDefault() => _opts != null && _opts.CanId == 0;

        private readonly SemaphoreSlim _txLock = new SemaphoreSlim(1, 1);
        private readonly int maxRetry = 10;
        private readonly int backoffMs = 1;
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected || data == null) return false;

            await _txLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var rawId29 = _lastTxRawId & 0x1FFFFFFF;
                uint can_id = MakeCanId(rawId29, eff: 1, rtr: 0, err: 0);

                var tx = new ZLGCAN.ZCAN_Transmit_Data
                {
                    frame = new ZLGCAN.can_frame
                    {
                        can_id = can_id,
                        can_dlc = (byte)Math.Min(8, data.Length),
                        __pad = 0,
                        __res0 = 0,
                        __res1 = 0,
                        data = Pad8(data)
                    },
                    transmit_type = 0 // 正常发送
                };

                int size = Marshal.SizeOf(typeof(ZLGCAN.ZCAN_Transmit_Data));
                IntPtr p = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(tx, p, false);

                    var ch = GetCurrentChannel();
                    for (int i = 0; i < maxRetry; i++)
                    {
                        uint sent = ZLGCAN.ZCAN_Transmit(ch, p, 1);
                        if (sent >= 1) return true;
                        await Task.Delay(backoffMs).ConfigureAwait(false); // 让硬件腾队列
                    }
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(p);
                }
            }
            finally
            {
                _txLock.Release();
            }
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
            EnsureConnected();
            bool busB = useCanB ? useCanB : UseCanBDefault();

            uint id = ArbId.Build(busB, InfoType.ResetCmd, DT.Reset, src: 0x00, dst: dest);
            _lastTxRawId = id;

            byte dl = 0x02;
            byte dt = DT.Reset;
            byte w0 = 0x3C, w1 = 0xFF;
            byte sum = Sum8(dt, w0, w1);

            var data = new byte[] { dl, dt, w0, w1, sum };
            bool ok = await SendAsync(data);
            if (ok && delayMs > 0) await Task.Delay(delayMs);
            return ok;
        }

        /// <summary>
        /// 读寄存器（演示：发送读命令；应答在接收线程中两帧合并并写入 _cache）
        /// 返回值仅表示命令是否成功发出；实际值从 Read/TryGetCached 获取。
        /// </summary>
        public async Task<uint?> ReadRegisterAsync(ushort regAddr, bool useCanB = false, byte dest = 0xA0, int timeoutMs = 50)
        {
            EnsureConnected();
            bool busB = useCanB ? useCanB : UseCanBDefault();

            uint reqId = ArbId.Build(busB, InfoType.ReadCmd, DT.Read, 0x00, dest);
            _lastTxRawId = reqId;

            byte dl = 0x02;
            byte dt = DT.Read;
            byte w0 = (byte)((regAddr >> 8) & 0xFF);//(byte)02; //
            byte w1 = (byte)(regAddr & 0xFF);//(byte)204;// 
            byte sum = Sum8(dt, w0, w1);

            var cmd = new byte[] { dl, dt, w0, w1, sum };
            if (!await SendAsync(cmd)) throw new Exception("读命令发送失败");

            // 可选：等待缓存里出现（简单轮询等待）
            //var key = regAddr.ToString("X4");
            //var deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            //while (DateTime.Now < deadline)
            //{
            //    if (TryGetCached(key, out var val))
            //    {
            //        try { return Convert.ToUInt32(val); } catch { }
            //    }
            //    await Task.Delay(1);
            //}
            return null; // 超时情况下返回 null
        }

        public Task<bool> WriteRegisterAsync(ushort regAddr, uint value4)
        {
            throw new NotImplementedException();
        }

        /// <summary> 写寄存器（4.5 两帧发送，不要求应答），并写入缓存 </summary>
        public async Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 0xA0, int delayMs = 5)
        {
            EnsureConnected();
            if (value4 == null || value4.Length != 4) throw new ArgumentException("value4 必须为4字节");
            bool busB = useCanB ? useCanB : UseCanBDefault();

            uint id = ArbId.Build(busB, InfoType.WriteCmd, DT.Write, 0x00, dest);
            _lastTxRawId = id;

            byte dl = 0x06;
            byte dt = DT.Write;
            byte w0 = (byte)((regAddr >> 8) & 0xFF);
            byte w1 = (byte)(regAddr & 0xFF);
            byte seq0 = 0x00, seq1 = 0x01;
            byte sum = Sum8(dt, w0, w1, value4[0], value4[1], value4[2], value4[3]);

            // 帧1：[Seq0][DL=06][DT=4F][W0][W1][W2][W3][W4]
            var f1 = ( new byte[] { seq0, dl, dt, w0, w1, value4[0], value4[1], value4[2] });
            // 帧2：[Seq1][W5][SUM]
            var f2 = (new byte[] { seq1, value4[3], sum });

            // 连续发送两帧
            var ok1 = await SendAsync(f1);
           
            var ok2 = ok1 && await SendAsync(f2);
            if (!ok2) throw new Exception("写命令发送失败");

            if (delayMs > 0) await Task.Delay(delayMs);

            // 缓存
            uint u = ((uint)value4[0] << 24) | ((uint)value4[1] << 16) | ((uint)value4[2] << 8) | value4[3];
            var key = regAddr.ToString("X4");
            _cache.AddOrUpdate(key, u, (_, __) => u);
        }

        // 便于你继续复用的解析（如你有 Utility.ParseHexToUInt 等）
        public (string, object) Parse(byte[] data)
        {
            // 占位：保持你原先的解析接口签名（如果有自定义用途）
            // 这里保留，不在本版本中主动调用。
            return ("0000", 0u);
        }

        // ====== 内部实现 ======

        private static byte[] Pad8(byte[] src)
        {
            var b = new byte[8];
            Array.Clear(b, 0, b.Length);
            Array.Copy(src, b, Math.Min(8, src.Length));
            return b;
        }

        private void EnsureConnected()
        {
            if (!IsConnected) throw new InvalidOperationException("CAN 未连接");
        }

        private void EnsureConnectedDevice()
        {
            if (_devHandle == IntPtr.Zero) throw new InvalidOperationException("设备未打开");
        }

        private IntPtr GetCurrentChannel()
        {
            if (_chnHandles.TryGetValue(_currentCanId, out var h) && h != IntPtr.Zero) return h;
            throw new InvalidOperationException("当前通道未初始化/未打开");
        }

        /// <summary>
        /// 初始化并打开指定通道（设置波特率→Init→Start），返回通道句柄。
        /// </summary>
        private IntPtr StartChannel(uint canIdx, int baudIndex)
        {
            if (_devHandle == IntPtr.Zero) return IntPtr.Zero;

            // 设置波特率（字符串）
            var bitrate = GetBitrateString(baudIndex); // 例如 "500000"
            var path = $"{canIdx}/baud_rate";
            var ret = ZLGCAN.ZCAN_SetValue(_devHandle, path, bitrate);
            if (ret != 1) throw new Exception($"设置波特率失败：{path}={bitrate}");

            // InitCAN
            //var init = new ZLGCAN.ZCAN_CHANNEL_INIT_CONFIG
            //{
            //    can_type = 0, // 0 - CAN
            //    config = new ZLGCAN._ZCAN_CHANNEL_INIT_CONFIG
            //    {
            //        can = new ZLGCAN._ZCAN_CHANNEL_CAN_INIT_CONFIG
            //        {
            //            acc_code = 0x0,
            //            acc_mask = 0x1FFFFFFF, // 允许所有（对于 USBCAN-8E-U 要求设置）
            //            filter = 0,
            //            timing0 = 0, // 由 baud_rate 负责
            //            timing1 = 0,
            //            mode = 0,    // 正常模式
            //            reserved = 0
            //        },
            //        canfd = new ZLGCAN._ZCAN_CHANNEL_CANFD_INIT_CONFIG()
            //    }
            //};

            ZLGCAN.ZCAN_CHANNEL_INIT_CONFIG InitConfig = new ZLGCAN.ZCAN_CHANNEL_INIT_CONFIG(); // 结构体
            InitConfig.can_type = 0;                // 0 - CAN (USBCAN 只能选择CAN模式)
            InitConfig.config.can.mode = 0;         // 0 - 正常模式，1 - 只听模式
            InitConfig.config.can.acc_code = 0x0;           // USBCAN-8E-U必须设置acc_code、acc_mask
            InitConfig.config.can.acc_mask = 0x1FFFFFFF;

            var p = Marshal.AllocHGlobal(Marshal.SizeOf(InitConfig));
            IntPtr ch = IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(InitConfig, p, false);
                ch = ZLGCAN.ZCAN_InitCAN(_devHandle, canIdx, p);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
            if (ch == IntPtr.Zero) throw new Exception("ZCAN_InitCAN 失败");

            // StartCAN
            if (ZLGCAN.ZCAN_StartCAN(ch) != 1) throw new Exception("ZCAN_StartCAN 失败");

            // ClearBuffer（可选）
            ZLGCAN.ZCAN_ClearBuffer(ch);

            _chnHandles[canIdx] = ch;
            return ch;
        }

        /// <summary>
        /// 接收循环：ZCAN_GetReceiveNum + ZCAN_Receive，FrameParser 优先，其次默认两帧读应答合并
        /// </summary>
        private async Task RxLoopAsync(CancellationToken token)
        {
            // 组装两帧应答：key=完整应答ID(包含扩展ID的29bit)，value=第一帧
            var halfReplies = new ConcurrentDictionary<uint, ZLGCAN.ZCAN_Receive_Data>();

            // 用于多通道轮询：一般你只开一个通道；若多通道这里会轮询每个句柄
            var chnIds = new List<uint>();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    chnIds.Clear();
                    chnIds.AddRange(_chnHandles.Keys);

                    foreach (var canId in chnIds)
                    {
                        if (!_chnHandles.TryGetValue(canId, out var ch) || ch == IntPtr.Zero) continue;

                        uint pending = ZLGCAN.ZCAN_GetReceiveNum(ch, 0);
                        if (pending == 0)
                        {
                            continue;
                        }

                        var max = (uint)Math.Min(100, pending);
                        var size = Marshal.SizeOf(typeof(ZLGCAN.ZCAN_Receive_Data));
                        var pBuf = Marshal.AllocHGlobal(size * (int)max);

                        try
                        {
                            uint got = ZLGCAN.ZCAN_Receive(ch, pBuf, max, 10);
                            for (int i = 0; i < got; i++)
                            {
                                var rec = (ZLGCAN.ZCAN_Receive_Data)Marshal.PtrToStructure(
                                    pBuf + i * size,
                                    typeof(ZLGCAN.ZCAN_Receive_Data));

                                // 可选：用户自定义解析优先
                                if (FrameParser != null)
                                {
                                    var res = FrameParser(rec);
                                    if (res.HasValue)
                                    {
                                        var (k, v) = res.Value;
                                        if (!string.IsNullOrEmpty(k))
                                            _cache.AddOrUpdate(k.ToUpperInvariant(), v, (_, __) => v);
                                        continue;
                                    }
                                }

                                // 默认“读寄存器应答两帧合并”解析（保持与旧版一致）
                                var isExt = ((rec.frame.can_id & (1u << 31)) != 0);
                                var isRemote = ((rec.frame.can_id & (1u << 30)) != 0);
                                if (!isExt || isRemote) continue; // 只处理扩展数据帧

                                // 取 29bit 原始ID
                                uint rawId29 = rec.frame.can_id & 0x1FFFFFFF;
                                var (bus, mt, dt, src, dst) = ArbId.Parse(rawId29);
                                if (mt == InfoType.ReadReply && dt == DT.Reply && dst == 0x00)
                                {
                                    var dlc = rec.frame.can_dlc;
                                    if (dlc == 8 && rec.frame.data[0] == 0x00)
                                    {
                                        // 第一帧
                                        halfReplies[rawId29] = rec;
                                    }
                                    else if (dlc == 3 && rec.frame.data[0] == 0x01)
                                    {
                                        // 第二帧
                                        if (halfReplies.TryRemove(rawId29, out var f0))
                                        {
                                            ParseReplyTwoFrames(f0, rec, out var ah, out var al, out var d4, out var sum);
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
                        finally
                        {
                            Marshal.FreeHGlobal(pBuf);
                        }
                    }
                }
                catch
                {
                    // 忽略瞬时异常，避免杀死接收循环
                }

                await Task.Delay(1, token);
            }
        }

        private static void ParseReplyTwoFrames(ZLGCAN.ZCAN_Receive_Data f0, ZLGCAN.ZCAN_Receive_Data f1,
            out byte addrHi, out byte addrLo, out byte[] data4, out byte sum)
        {
            if (f0.frame.can_dlc != 8 || f0.frame.data[0] != 0x00) throw new Exception("应答帧1格式错误");
            if (f1.frame.can_dlc != 3 || f1.frame.data[0] != 0x01) throw new Exception("应答帧2格式错误");

            byte dl = f0.frame.data[1];
            byte dt = f0.frame.data[2];
            if (dl != 0x06 || dt != DT.Reply) throw new Exception("应答DL/DT不匹配");

            addrHi = f0.frame.data[3];
            addrLo = f0.frame.data[4];
            data4 = new byte[] { f0.frame.data[5], f0.frame.data[6], f0.frame.data[7], f1.frame.data[1] };
            sum = f1.frame.data[2];
        }

        // ====== 工具 ======

        private static byte Sum8(params byte[] bytes)
        {
            int s = 0; foreach (var b in bytes) s = (s + b) & 0xFF;
            return (byte)s;
        }

        private static uint MakeCanId(uint id29, int eff, int rtr, int err)
        {
            uint ueff = (uint)((eff != 0) ? 1 : 0); // 1 扩展帧
            uint urtr = (uint)((rtr != 0) ? 1 : 0); // 1 远程帧
            uint uerr = (uint)((err != 0) ? 1 : 0); // 错误帧（一般 0）
            return (id29 & 0x1FFFFFFFu) | (ueff << 31) | (urtr << 30) | (uerr << 29);
        }

        /// <summary>
        /// 你的旧注释中提到“如 1=500kbps”，这里提供一个常用映射；如需更多档位，可自行扩展。
        /// </summary>
        private static string GetBitrateString(int baudIndex)
        {
            // 你也可以直接让 UI 传入字符串波特率，或把索引与实际数值放到配置中
            switch (baudIndex)
            {
                case 0: return "500000";    // 500k（默认）
                case 1: return "250000";    // 250k
                case 2: return "125000";    // 125k
                case 3: return "100000";    // 100k
                case 4: return "800000";    // 800k
                case 5: return "20000";     // 20k（如需）
                case 6: return "10000";     // 10k
                case 7: return "1000000";   // 1M
                default: return "500000";
            }
        }

        // ====== 协议定义，与旧版保持一致 ======
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

