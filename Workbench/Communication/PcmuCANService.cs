using log4net;
using MathNet.Numerics.RootFinding;
using PPEC.Communication;
using PPEC.Communication.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Utils;
using System.Windows;

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
    public class PcmuCANService : CanCommService1, IBaseCommService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PcmuUartService));

        // —— 类型码（大端） ——
        private const ushort TYPE_REMOTE_CTRL = 0x000A; // 遥控指令
        private const ushort TYPE_REMOTE_CTRL_ACK = 0x000F; // 遥控应答
        private const ushort TYPE_INJECTION = 0x0014; // 注数
        //private const ushort TYPE_INJECTION_ACK = 0x0019; // 注数应答
        private const ushort TYPE_TLM_QUERY = 0x001E; // 遥测查询
        private const ushort TYPE_TLM_RESPONSE = 0x0023; // 遥测数据应答

        // —— 固定字段 ——
        private static readonly byte[] HEADER = { 0x03, 0x5F };
        private const byte SRC = 0x00;
        private const byte DEST = 0x0A;
        private const byte TYPE_RESERVED = 0xFF;
        private static readonly byte[] CHN_FF7 = Enumerable.Repeat((byte)0xFF, 7).ToArray();

        // —— 发送节流 ——
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        /// <summary>最小发送间隔（ms），协议要求≥10ms，默认12ms</summary>
        public int MinSendIntervalMs { get; set; } = 12;

        // —— 粘包/拆包缓存 ——
        private readonly List<byte> _rxBuffer = new List<byte>(4096);
        private readonly object _rxLock = new object();

        // —— 等待表（按“应答类型码”） ——
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>> _waiters
            = new ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>>();

        // —— 遥测队列（前端拉取） —— 
        private readonly BoundedConcurrentQueue<TelemetryRecord> _tlmQueue = new(capacity: 1000);
        public ObservableCollection<TelemetrySliceField>? _tlmSlices; // 若配置了，则按位切片解析

        // —— 事件 ——
        public event EventHandler<TelemetryEventArgs>? TelemetryReceived; // 原始payload（0023）
        public event EventHandler<TelemetryRecord>? TelemetryParsed;      // 解析后的记录

        // ====== 设备/通道句柄（zlgcan） ======
        private IntPtr _devHandle = IntPtr.Zero;
        private readonly Dictionary<uint, IntPtr> _chnHandles = new(); // key = CanId(0/1/...)
        private uint _currentCanId = 0;

        private CancellationTokenSource _cts;
        private Task _rxLoop;

        private CanConnectOptions _opts;

        private int SelectCount;
        public string Delay
        {
            get; set;
        }
        public bool IsConnected => _devHandle != IntPtr.Zero && _chnHandles.TryGetValue(_currentCanId, out var h) && h != IntPtr.Zero;

        /// <summary> key：4位HEX寄存器地址（比如 "0170"）；value：解析后的十进制数据（uint 或其它 object） </summary>
        private readonly ConcurrentDictionary<string, object> _cache = new();

        /// <summary> 可选自定义解析器（把 接收到的一帧 -> (key,value)）；不设置则用默认协议解析（两帧读应答合并） </summary>
        public Func<ZLGCAN.ZCAN_Receive_Data, (string key, object value)?> FrameParser { get; set; }

        // 最近一次设置的发送 ID（给 SendAsync 用；ID 是 29bit 原始ID，不含 EFF/RTR/ERR 标志位）
        private uint _lastTxRawId = 0;

        private bool UseCanBDefault() => _opts != null && _opts.CanId == 0;

        private readonly SemaphoreSlim _txLock = new SemaphoreSlim(1, 1);
        private readonly int maxRetry = 10;
        private readonly int backoffMs = 1;

        #region 基础工具（端序/CRC）

        private static byte[] U16_BE(ushort v) => new byte[] { (byte)(v >> 8), (byte)v };
        private static byte[] U16_LE(ushort v) => new byte[] { (byte)v, (byte)(v >> 8) };
        private static ushort CRC16_CCITT_FALSE(byte[] data)
        {
            return Crc16CcittFalse.CalculateCrc(data);
            ushort crc = 0xFFFF;
            if (data != null)
            {
                foreach (byte b in data)
                {
                    crc ^= (ushort)(b << 8);
                    for (int i = 0; i < 8; i++)
                        crc = (ushort)(((crc & 0x8000) != 0) ? ((crc << 1) ^ 0x1021) : (crc << 1));
                }
            }
            return crc;
        }

        private static ushort ReadU16_BE(byte[] buf, int off) =>
            (ushort)((buf[off] << 8) | buf[off + 1]);

        #endregion

        public void Connect_CAN_Telemetry(CanConnectOptions options)
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

        public void Connect_CAN_Telemetry(string portName, int baudRate = 0, System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
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

            Connect_CAN_Telemetry(opt); // 复用强类型版本
        }

        private async Task SendTLM(bool useCanB, byte dest, int timeoutMs, byte TLMTag)
        {
            EnsureConnected();
            bool busB = useCanB ? useCanB : UseCanBDefault();

            // ≥10ms 节流
            //await Task.Delay(MinSendIntervalMs).ConfigureAwait(false);
            uint id = ArbId.Build(InfoType.TLM_QUERY, DT.TLM_QUERY, 0x00, dest);
            _lastTxRawId = id;

            byte D1 = 0x03;
            byte D2 = 0x05;
            byte TMT = TLMTag;
            byte W0 = 0xA1;
            byte W1 = 0xB2;
            byte sum = Sum8(D1, D2, TMT, W0, W1);
            var frame = (new byte[] { D1, D2, TMT, W0, W1, sum });
            var ok1 = await SendAsync(frame);
            if (!ok1) throw new Exception("写命令发送失败");
            using var cts = new CancellationTokenSource(timeoutMs);

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

        // 新增：重组状态结构体与容器（放在类内字段区域）
        private class ReassemblyState
        {
            public int ExpectedLength; // 总有效字节数（DL）
            public MemoryStream Buffer = new MemoryStream();
            public object Lock = new object();
        }

        private readonly ConcurrentDictionary<uint, ReassemblyState> _reassemblies = new ConcurrentDictionary<uint, ReassemblyState>();

        /// <summary>
        /// 接收循环：ZCAN_GetReceiveNum + ZCAN_Receive
        /// 实现多帧重组协议：每帧8字节，data[0]=seq（首帧=0x00，尾帧=0xAA），首帧 data[1] 为 DL（有效字节总数），后续字节为数据。
        /// 当重组完成时，把拼接结果当作连续字节块交给 HandleCanDataBytes（与 UART 路径统一）。
        /// 同时保留原先的 FrameParser / 两帧读应答合并逻辑。
        /// </summary>
        private async Task RxLoopAsync(CancellationToken token)
        {
            // 组装两帧应答（旧的两帧寄存器读应答）：key=完整应答ID，value=第一帧
            var halfReplies = new ConcurrentDictionary<uint, ZLGCAN.ZCAN_Receive_Data>();

            // 用于多通道轮询
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
                        if (pending == 0) continue;

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

                                // 用户自定义解析优先
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

                                // 只处理扩展数据帧（与旧逻辑保持一致）
                                var isExt = ((rec.frame.can_id & (1u << 31)) != 0);
                                var isRemote = ((rec.frame.can_id & (1u << 30)) != 0);
                                if (!isExt || isRemote) continue;

                                // 取 29bit 原始ID，用于区分会话/消息流
                                uint rawId29 = rec.frame.can_id & 0x1FFFFFFF;
                                var (bus, mt, dt, src, dst) = ArbId.Parse(rawId29);

                                // 保留旧的两帧寄存器读应答合并（与 CanCommService1 保持一致）
                                if (mt == InfoType.TLM_RESPONSE && dt == DT.TLM_RESPONSE && dst == 0x00)
                                {
                                    var dlc = rec.frame.can_dlc;
                                    if (dlc == 8 && rec.frame.data[0] == 0x00)
                                    {
                                        // 第一帧（兼容旧实现）
                                        halfReplies[rawId29] = rec;
                                        // 仍继续下面的多帧重组逻辑（将数据也纳入流）
                                    }
                                    else if (dlc == 8 && rec.frame.data[0] != 0x00)
                                    {
                                        if (halfReplies.TryRemove(rawId29, out var f0))
                                        {
                                            try
                                            {
                                                ParseReplyTwoFrames(f0, rec, out var ah, out var al, out var d4, out var sum);
                                                var calc = Sum8(DT.TLM_RESPONSE, ah, al, d4[0], d4[1], d4[2], d4[3]);
                                                if (calc == sum)
                                                {
                                                    var key = $"{ah:X2}{al:X2}";
                                                    uint val = ((uint)d4[0] << 24) | ((uint)d4[1] << 16) | ((uint)d4[2] << 8) | d4[3];
                                                    _cache.AddOrUpdate(key, val, (_, __) => val);
                                                }
                                            }
                                            catch
                                            {
                                                // 忽略解析错误
                                            }
                                        }
                                    }
                                }

                                // ---------- 多帧重组协议实现 ----------
                                // 期望每个 CAN 帧最长 8 字节，布局： [seq][d1][d2]...[d7]
                                // 首帧 seq==0x00, 首帧的 d1 为 DL（总有效字节数），首帧贡献 d2..d7
                                // 中间帧贡献 d1..d7
                                // 末帧 seq==0xAA，贡献 d1..d7（末帧实际有效字节由首帧 DL 决定）
                                try
                                {
                                    int dlc = rec.frame.can_dlc;
                                    if (dlc <= 0) continue;
                                    byte seq = rec.frame.data[0];

                                    // 处理首帧
                                    if (seq == 0x00)
                                    {
                                        // 创建或重置会话
                                        var state = new ReassemblyState();
                                        byte dl = 0;
                                        if (dlc >= 2) dl = rec.frame.data[1];
                                        state.ExpectedLength = dl;

                                        // 追加首帧剩余字节（从 index=2 到 dlc-1）
                                        if (dlc > 2)
                                        {
                                            int start = 2;
                                            int count = dlc - start;
                                            lock (state.Lock)
                                            {
                                                state.Buffer.Write(rec.frame.data, start, count);
                                            }
                                        }

                                        _reassemblies[rawId29] = state;

                                        // 检查是否已完成（当有效长度非常小）
                                        lock (state.Lock)
                                        {
                                            if (state.ExpectedLength > 0 && state.Buffer.Length >= state.ExpectedLength)
                                            {
                                                var all = state.Buffer.ToArray();
                                                var payload = new byte[state.ExpectedLength];
                                                Array.Copy(all, 0, payload, 0, state.ExpectedLength);
                                                _reassemblies.TryRemove(rawId29, out _);
                                                HandleCanDataBytes(payload);
                                            }
                                        }
                                    }
                                    else if (seq == 0xAA)
                                    {
                                        // 终止帧：如果有会话则追加并结束；否则把这帧当作普通字节追加
                                        if (_reassemblies.TryGetValue(rawId29, out var st))
                                        {
                                            if (dlc > 1)
                                            {
                                                int start = 1;
                                                int count = dlc - start;
                                                lock (st.Lock)
                                                {
                                                    st.Buffer.Write(rec.frame.data, start, count);
                                                }
                                            }

                                            // 完成并提交（截取到 ExpectedLength）
                                            lock (st.Lock)
                                            {
                                                int expected = st.ExpectedLength > 0 ? st.ExpectedLength : (int)st.Buffer.Length;
                                                var all = st.Buffer.ToArray();
                                                int take = Math.Min(expected, all.Length);
                                                var payload = new byte[take];
                                                Array.Copy(all, 0, payload, 0, take);
                                                _reassemblies.TryRemove(rawId29, out _);
                                                HandleCanDataBytes(payload);
                                            }
                                        }
                                        else
                                        {
                                            // 无会话，直接把本帧数据（从 index=1）交给解析
                                            if (dlc > 1)
                                            {
                                                var tmp = new byte[dlc - 1];
                                                Array.Copy(rec.frame.data, 1, tmp, 0, dlc - 1);
                                                HandleCanDataBytes(tmp);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 中间帧
                                        if (_reassemblies.TryGetValue(rawId29, out var st))
                                        {
                                            if (dlc > 1)
                                            {
                                                int start = 1;
                                                int count = dlc - start;
                                                lock (st.Lock)
                                                {
                                                    st.Buffer.Write(rec.frame.data, start, count);
                                                }
                                            }

                                            lock (st.Lock)
                                            {
                                                if (st.ExpectedLength > 0 && st.Buffer.Length >= st.ExpectedLength)
                                                {
                                                    var all = st.Buffer.ToArray();
                                                    var payload = new byte[st.ExpectedLength];
                                                    Array.Copy(all, 0, payload, 0, st.ExpectedLength);
                                                    _reassemblies.TryRemove(rawId29, out _);
                                                    HandleCanDataBytes(payload);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // 未见到首帧就收到中间帧：按兼容策略直接把本帧数据追加到解析流
                                            if (dlc > 1)
                                            {
                                                var tmp = new byte[dlc - 1];
                                                Array.Copy(rec.frame.data, 1, tmp, 0, dlc - 1);
                                                HandleCanDataBytes(tmp);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Warn("多帧重组出错", ex);
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pBuf);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("RxLoopAsync 瞬时异常", ex);
                }

                await Task.Delay(1, token);
            }
        }

        /// <summary>
        /// 把单个 CAN 帧的 data[0..dlc-1] 追加到 _rxBuffer 并尝试解析应用层帧（与 UART 的 ParseIncomingChunk 逻辑一致）
        /// </summary>
        private void AppendCanFrameDataToRxBuffer(ZLGCAN.ZCAN_Receive_Data rec)
        {
            int dlc = rec.frame.can_dlc;
            if (dlc <= 0) return;
            var bytes = new byte[dlc];
            Array.Copy(rec.frame.data, bytes, dlc);
            HandleCanDataBytes(bytes);
        }

        /// <summary>
        /// 将 CAN 接收到的字节追加到 _rxBuffer 并按 UART 一致的帧格式做粘包/拆包/CRC校验及分发
        /// 帧结构（与 UART 保持一致）：
        /// [HEADER(2)][SRC(1)][DEST(1)][CHN_FF7(7)][type(BE,2)][TYPE_RESERVED(1)][len(LE or BE,2)][payload][crc(BE)]
        /// </summary>
        private void HandleCanDataBytes(byte[] chunk)
        {
            if (chunk == null || chunk.Length == 0) return;

            try
            {
                lock (_rxLock)
                {
                    _rxBuffer.AddRange(chunk);

                    while (true)
                    {
                        // 找帧头
                        int idx = -1;
                        for (int i = 0; i <= _rxBuffer.Count - 2; i++)
                        {
                            if (_rxBuffer[i] == HEADER[0] && _rxBuffer[i + 1] == HEADER[1]) { idx = i; break; }
                        }
                        if (idx < 0)
                        {
                            if (_rxBuffer.Count > 8192) _rxBuffer.Clear();
                            break;
                        }
                        if (idx > 0) _rxBuffer.RemoveRange(0, idx); // 丢弃噪声

                        // 最小长度：16固定 + CRC2 = 18（与 UART 协议一致）
                        if (_rxBuffer.Count < 18) break;

                        // type 在偏移 11（HEADER(2)+SRC(1)+DEST(1)+CHN_FF7(7) = 11），与 UART 保持一致
                        ushort typeBe = ReadU16_BE(_rxBuffer.ToArray(), 11);
                        // 长度：优先 LE（示例 04 00），失败再试 BE（兼容对端实现差异）
                        ushort lenLE = (ushort)(_rxBuffer[14] | (_rxBuffer[15] << 8));
                        ushort lenBE = (ushort)((_rxBuffer[14] << 8) | _rxBuffer[15]);

                        bool TryConsume(ushort len)
                        {
                            int need = 18 + len; // 16固定 + N + 2CRC
                            if (_rxBuffer.Count < need) return false;

                            // payload & CRC
                            byte[] pl = new byte[len];
                            if (len > 0) _rxBuffer.CopyTo(16, pl, 0, len);
                            ushort crcBe = ReadU16_BE(_rxBuffer.ToArray(), 16 + len);
                            ushort calc = CRC16_CCITT_FALSE(pl);
                            if (crcBe != calc) return false;

                            // 消费帧
                            _rxBuffer.RemoveRange(0, need);
                            DispatchFrame(typeBe, pl);
                            return true;
                        }

                        if (TryConsume(lenLE)) continue;
                        if (TryConsume(lenBE)) continue;
                        // 数据不够，等待下次
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        //public int SelectCount = 0;
        public int ReceiveCountSuc = 0;
        public int ReceiveCountFault = 0;

        private bool DispatchFrame(ushort typeBe, byte[] payload)
        {
            try
            {
                ushort code = (payload != null && payload.Length >= 4) ? ReadU16_BE(payload, 2) : (ushort)0;
                bool isFailure = code == 0xFFFF; // 0xFFFF 表示失败
                if (isFailure)
                {
                    ReceiveCountFault++;
                    if (ReceiveCountFault > 255) ReceiveCountFault = 0;
                    return false;
                }
                switch (typeBe)
                {
                    //case TYPE_REMOTE_CTRL_ACK: // 000F
                    //case TYPE_INJECTION_ACK:   // 0019
                        //{
                        //    if (_waiters.TryRemove(typeBe, out var tcs))
                        //        tcs.TrySetResult(payload);
                        //    break;
                        //}
                    case TYPE_TLM_RESPONSE:    // 0023
                        {
                            // 解析 → 入队（可配置位切片）
                            var rec = ParseTelemetryPayload(payload);
                            _tlmQueue.Enqueue(rec);
                            TelemetryParsed?.Invoke(this, rec);
                            TelemetryReceived?.Invoke(this, new TelemetryEventArgs(payload));

                            ReceiveCountSuc++;
                            if (ReceiveCountSuc > 255) ReceiveCountSuc = 0;

                            if (_waiters.TryRemove(typeBe, out var tcs))
                                tcs.TrySetResult(payload);
                            break;
                        }
                    default:
                        _log.Debug($"收到未处理类型 0x{typeBe:X4}, Len={payload?.Length ?? 0}");
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn("DispatchFrame 出错", ex);
                return false;
            }
        }

        #region 遥测解析（优先位切片；否则仅存Raw）

        private TelemetryRecord ParseTelemetryPayload(byte[] payload)
        {
            payload ??= Array.Empty<byte>();

            // 1) 若配置了“位切片解析”，优先使用
            if (_tlmSlices != null && _tlmSlices.Count > 0)
            {
                var values = ParseSlices(payload);
                return new TelemetryRecord
                {
                    Timestamp = DateTime.UtcNow,
                    RawPayload = payload,
                    Values = values
                };
            }

            // 2) 未配置解析表：仅缓存Raw
            return new TelemetryRecord
            {
                Timestamp = DateTime.UtcNow,
                RawPayload = payload,
                Values = new Dictionary<string, double> { { "len", payload.Length } }
            };
        }

        private Dictionary<string, double> ParseSlices(byte[] payload)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in _tlmSlices!)
            {
                try
                {
                    // 拼接参与字节 → 抽取位段 → 转目标类型 → 缩放
                    ulong acc = AssembleBytes(payload, f.StartByte, f.ByteCount, f.Order);
                    ulong raw = ExtractBits(acc, f.BitStart, f.BitLength);
                    object v = CastToTarget(raw, f.BitLength, f.As);

                    var str = UtilsFunc.BitsToHex(raw, f.BitLength);
                    double result = 0;

                    // 线性变换（数值型）
                    if (v is sbyte sb)
                    {
                        result = sb * f.Scale;
                    }
                    else if (v is byte ub)
                    {
                        result = ub * f.Scale;
                    }
                    else if (v is short s16)
                    {
                        result = s16 * f.Scale;
                    }
                    else if (v is ushort u16)
                    {
                        result = u16 * f.Scale;
                    }
                    else if (v is int s32)
                    {
                        result = s32 * f.Scale;
                    }
                    else if (v is uint u32)
                    {
                        result = u32 * f.Scale;
                    }
                    else if (v is float f32)
                    {
                        result = f32 * f.Scale;
                    }
                    else
                    {
                        f.ShowResult = v.ToString();
                    }

                    // 处理 ParamA/B/C 等公式，保持与 UART 实现一致（保守解析）
                    switch (f.ParamC)
                    {
                        case "0":// "YEqX":
                            f.ShowResult = result.ToString();
                            dict[f.Name] = result;
                            f.SourceData = result;
                            break;
                        case "1": //"YEqAX":
                            if (double.TryParse(f.ParamA, out var a1))
                            {
                                f.ShowResult = (result * a1).ToString();
                                dict[f.Name] = (result * a1);
                                f.SourceData = result * a1;
                            }
                            else
                            {
                                f.ShowResult = result.ToString();
                                dict[f.Name] = result;
                                f.SourceData = result;
                            }
                            break;
                        case "2":// "YEqABminusAX":
                            if (double.TryParse(f.ParamA, out var a2) && double.TryParse(f.ParamB, out var b2))
                            {
                                if (f.ParamSign == "-")
                                    f.SourceData = Math.Round(b2 * a2 - result * a2, 3);
                                else
                                    f.SourceData = Math.Round(b2 * a2 + result * a2, 3);
                                f.ShowResult = f.SourceData.ToString();
                            }
                            //dict[f.Name] = f.SourceData is not double dv ? result : dv;
                            break;
                        case "3":// "YEqAXpmB":
                            if (double.TryParse(f.ParamA, out var a3) && double.TryParse(f.ParamB, out var b3))
                            {
                                if (f.ParamSign == "-")
                                    f.SourceData = Math.Round(result * a3 - b3, 3);
                                else
                                    f.SourceData = Math.Round(result * a3 + b3, 3);
                                f.ShowResult = f.SourceData.ToString();
                            }
                            dict[f.Name] = (f.SourceData is double dv) ? dv : result;
                            break;
                        case "4":// "Unknown":
                            var returnd = ParseExcelDataToDictionary(f.ShowStr);
                            string ss = "未知";
                            var lookupKey = "0x" + result.ToString();
                            if (returnd.TryGetValue(lookupKey, out var mapped))
                            {
                                ss = mapped;
                            }
                            f.SourceData = result;
                            f.ShowResult = ss;
                            dict[f.Name] = result;
                            break;
                        default:
                            f.ShowResult = result.ToString();
                            dict[f.Name] = result;
                            f.SourceData = result;
                            break;
                    }

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        f.ShowHexStr = str;
                    });
                }
                catch (Exception ex)
                {
                    _log.Warn($"ParseSlices 错误: field={f?.Name}", ex);
                }
            }
            return dict;
        }


        // 兼容性解析器：把类似 "0x01=On;0x02=Off" 或多行/逗号/制表符格式解析为字典
        private static Dictionary<string, string> ParseExcelDataToDictionary(string input)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(input)) return dict;

            // 尝试按行分割，否则按分号分割
            var lines = input
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(s => s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            foreach (var line in lines)
            {
                // 支持多种分隔符：= , \t :
                string key = null;
                string val = null;

                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    key = parts[0].Trim();
                    val = parts[1].Trim();
                }
                else
                {
                    parts = line.Split(new[] { ',' }, 2);
                    if (parts.Length == 2)
                    {
                        key = parts[0].Trim();
                        val = parts[1].Trim();
                    }
                    else
                    {
                        parts = line.Split(new[] { '\t', ':' }, 2);
                        if (parts.Length == 2)
                        {
                            key = parts[0].Trim();
                            val = parts[1].Trim();
                        }
                        else
                        {
                            // 如果都没有分隔符，取第一个 token 为 key，其余为 value
                            var tokens = line.Split(new[] { ' ' }, 2);
                            if (tokens.Length >= 2)
                            {
                                key = tokens[0].Trim();
                                val = tokens[1].Trim();
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(key))
                {
                    // 保持原样 key，便于外部使用 "0x..." 的查找习惯
                    if (!dict.ContainsKey(key))
                        dict[key] = val ?? string.Empty;
                }
            }

            return dict;
        }

        // —— 位切片工具 ——

        private static ulong AssembleBytes(byte[] src, int startByte, int byteCount, ByteOrder order)
        {
            if (startByte < 0 || byteCount <= 0 || startByte + byteCount > src.Length)
                throw new ArgumentOutOfRangeException(nameof(startByte));
            ulong acc = 0UL;
            if (order == ByteOrder.BE)
            {
                for (int i = 0; i < byteCount; i++)
                    acc = (acc << 8) | src[startByte + i];
            }
            else
            {
                for (int i = 0; i < byteCount; i++)
                    acc |= ((ulong)src[startByte + i]) << (8 * i);
            }
            return acc;
        }

        private static ulong ExtractBits(ulong acc, int bitStart, int bitLength)
        {
            if (bitLength <= 0 || bitLength > 64) throw new ArgumentOutOfRangeException(nameof(bitLength));
            return (acc >> bitStart) & (bitLength == 64 ? ulong.MaxValue : ((1UL << bitLength) - 1UL));
        }

        private static object CastToTarget(ulong raw, int width, TargetType asType)
        {
            switch (asType)
            {
                case TargetType.U8: return (byte)raw;
                case TargetType.I8:
                    {
                        int signBit = width - 1;
                        long v = ((raw & (1UL << signBit)) != 0) ? (long)raw - (1L << width) : (long)raw;
                        return unchecked((sbyte)v);
                    }
                case TargetType.U16: return (ushort)raw;
                case TargetType.I16:
                    {
                        int signBit = width - 1;
                        long v = ((raw & (1UL << signBit)) != 0) ? (long)raw - (1L << width) : (long)raw;
                        return unchecked((short)v);
                    }
                case TargetType.U32:
                case TargetType.UInt: return (uint)raw;
                case TargetType.I32:
                case TargetType.Int:
                    {
                        int signBit = width - 1;
                        long v = ((raw & (1UL << signBit)) != 0) ? (long)raw - (1L << width) : (long)raw;
                        return unchecked((int)v);
                    }
                case TargetType.Float32:
                    {
                        if (width != 32) throw new InvalidOperationException("Float32 需要 BitLength=32");
                        uint u = (uint)raw;
                        return BitConverter.ToSingle(BitConverter.GetBytes(u), 0);
                    }
                default: return (uint)raw;
            }
        }

        #endregion

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
            public const byte TLM_QUERY = 0x5F;
            public const byte TLM_RESPONSE = 0x6F;
            public const byte REMOTE_CTRL = 0x7F;
            public const byte REMOTE_CTRL_ACK = 0x8F;
        }

        private enum InfoType : byte
        {
            TLM_QUERY = 0x05,     //遥测指令
            TLM_RESPONSE = 0x06,     //遥测指令应答
            REMOTE_CTRL = 0x07,     //遥控指令
            REMOTE_CTRL_ACK = 0x08,     //遥控指令应答
        }

        /// <summary> 按协议把 29bit 扩展 ID 拼出来： [LT(1)][MT(4)][DT(8)][SRC(8)][DST(8)] </summary>
        /// 这里LT固定为1
        private static class ArbId
        {
            public static uint Build(InfoType mt, byte dt, byte src, byte dst)
            {
                uint id = 0;
                id |= 1u << 28;       // LT
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
        public Task<byte[]> QueryTelemetryOnceAsync(int timeoutMs = 200, byte projectTag = 0xFF)
        {
            SelectCount++;
            if (SelectCount > 255)
            {
                SelectCount = 0;
            }
            SendAsync([projectTag, 0x0A, 0x04, 0x1E]);
            return null;
        }
    }
}