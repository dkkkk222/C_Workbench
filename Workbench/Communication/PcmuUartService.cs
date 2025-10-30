using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using PPEC.Communication.Model;
using Workbench.Utils;

namespace Workbench.Communication
{
    public class PcmuUartService : SerialPortService, IBaseCommService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PcmuUartService));

        // —— 类型码（大端） ——
        private const ushort TYPE_REMOTE_CTRL = 0x000A; // 遥控指令
        private const ushort TYPE_REMOTE_CTRL_ACK = 0x000F; // 遥控应答
        private const ushort TYPE_INJECTION = 0x0014; // 注数
        private const ushort TYPE_INJECTION_ACK = 0x0019; // 注数应答
        private const ushort TYPE_TLM_QUERY = 0x001E; // 遥测查询
        private const ushort TYPE_TLM_RESPONSE = 0x0023; // 遥测数据应答

        // —— 固定字段 ——
        private static readonly byte[] HEADER = { 0xD2, 0x8C };
        private const byte SRC = 0x00;
        private const byte DEST = 0x0A;
        private const byte TYPE_RESERVED = 0xFF;
        private static readonly byte[] CHN_FF7 = Enumerable.Repeat((byte)0xFF, 7).ToArray();

        // —— 发送节流 ——
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        private readonly Stopwatch _sw = Stopwatch.StartNew();
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
        private List<TelemetrySliceField>? _tlmSlices; // 若配置了，则按位切片解析

        // —— 事件 ——
        public event EventHandler<TelemetryEventArgs>? TelemetryReceived; // 原始payload（0023）
        public event EventHandler<TelemetryRecord>? TelemetryParsed;      // 解析后的记录

        public PcmuUartService()
        {
            // 使用你父类提供的 DataParser 钩子，交给我们解析
            base.DataParser = ParseIncomingChunk;
        }

        #region 对外API（发送 + 等待）

        /// <summary>
        /// 发送“遥控指令(000A)”，payload=4字节（BE），等待“遥控应答(000F)”
        /// 返回：AAAA成功 / FFFF失败（其他保留码按RawCode返回）
        /// </summary>
        public new async Task<ControlAck> SendRemoteControlAsync(byte[] payload, int timeoutMs = 50)
        {
            //var payload = new byte[] { (byte)(cmdBE >> 24), (byte)(cmdBE >> 16), (byte)(cmdBE >> 8), (byte)cmdBE };
            var resp = await SendAndWaitAsync(TYPE_REMOTE_CTRL, payload, TYPE_REMOTE_CTRL_ACK, timeoutMs).ConfigureAwait(false);
            if (resp == null || resp.Length < 4) return new ControlAck(false, 0, resp ?? Array.Empty<byte>());
            ushort code = ReadU16_BE(resp, 2); // 00 00 AA AA -> 取后两个字节
            bool ok = code == 0xAAAA;
            return new ControlAck(ok, code, resp);
        }

        /// <summary>
        /// 发送“注数(0014)”，payload长度N≤256，等待“注数应答(0019)”
        /// </summary>
        public new async Task<ControlAck> SendInjectionAsync(byte[] payload, int timeoutMs = 80)
        {
            var resp = await SendAndWaitAsync(TYPE_INJECTION, payload, TYPE_INJECTION_ACK, timeoutMs).ConfigureAwait(false);
            if (resp == null || resp.Length < 4) return new ControlAck(false, 0, resp ?? Array.Empty<byte>());
            ushort code = ReadU16_BE(resp, 2); // 00 00 AA AA -> 取后两个字节
            bool ok = code == 0xAAAA;
            return new ControlAck(ok, code, resp);
        }

        /// <summary>
        /// 发送“遥测查询(001E)”并等待“遥测应答(0023)”，返回有效数据域payload
        /// </summary>
        public Task<byte[]> QueryTelemetryOnceAsync(int timeoutMs = 200)
            => SendAndWaitAsync(TYPE_TLM_QUERY, new byte[] { 0x00, 0x0A, 0x04, 0x1E }, TYPE_TLM_RESPONSE, timeoutMs);

        #endregion

        #region 队列&配置（前端使用）

        /// <summary>配置“按位切片”的遥测解析规则；不配置则仅缓存RawPayload</summary>
        public void ConfigureTelemetrySlices(IEnumerable<TelemetrySliceField> slices)
            => _tlmSlices = slices?.ToList();

        /// <summary>取一条（先进先出）</summary>
        public TelemetryRecord? TryDequeueTelemetry()
            => _tlmQueue.TryDequeue(out var rec) ? rec : null;

        /// <summary>快照（数组）</summary>
        public TelemetryRecord[] GetTelemetrySnapshot() => _tlmQueue.Snapshot();

        /// <summary>最近N条</summary>
        public TelemetryRecord[] GetLastTelemetry(int n) => _tlmQueue.TakeLast(n);

        /// <summary>清空队列</summary>
        public void ClearTelemetry() => _tlmQueue.Clear();

        #endregion

        #region 核心发送/等待/构帧

        private async Task<byte[]> SendAndWaitAsync(ushort typeCode, byte[] payload,
                                                    ushort expectedRespType, int timeoutMs)
        {
            // 先注册等待者，避免“先响后挂”的竞态
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_waiters.TryAdd(expectedRespType, tcs))
                throw new InvalidOperationException($"已有等待 0x{expectedRespType:X4} 的任务未完成");

            await _sendGate.WaitAsync().ConfigureAwait(false);
            try
            {
                // ≥10ms 节流
                await Task.Delay(MinSendIntervalMs).ConfigureAwait(false);

                var frame = BuildFrame(typeCode, payload);
                bool sent = await base.SendAsync(frame).ConfigureAwait(false);
                if (!sent)
                {
                    _waiters.TryRemove(expectedRespType, out _);
                    throw new IOException("串口发送失败");
                }

                using var cts = new CancellationTokenSource(timeoutMs);
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                _log.Warn($"等待 0x{expectedRespType:X4} 超时({timeoutMs}ms)");
                return null;
            }
            finally
            {
                _waiters.TryRemove(expectedRespType, out _);
                _sw.Restart();
                _sendGate.Release();
            }
        }

        /// <summary>
        /// 构造数据帧：
        /// [D2 8C][00][0A][FF*7][type(BE)][FF][len(LE)][payload][crc(BE)]
        /// 注意：CRC 仅对“有效数据域”payload 计算（CCITT-FALSE）
        /// 长度按文档示例使用 Little-Endian（4 -> 04 00）
        /// </summary>
        private static byte[] BuildFrame(ushort typeBe, byte[] payload)
        {
            payload ??= Array.Empty<byte>();
            if (payload.Length > 0xFFFF) throw new ArgumentOutOfRangeException(nameof(payload));

            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            // 帧头
            w.Write(HEADER);           // 2
            w.Write(SRC);              // +1
            w.Write(DEST);             // +1
            w.Write(CHN_FF7);          // +7
            w.Write(U16_BE(typeBe));   // +2
            w.Write(TYPE_RESERVED);    // +1
            w.Write(U16_LE((ushort)payload.Length)); // +2 (LE)
            if (payload.Length > 0) w.Write(payload);

            ushort crc = CRC16_CCITT_FALSE(payload);
            w.Write(U16_BE(crc));      // +2

            w.Flush();
            return ms.ToArray();
        }

        #endregion

        #region 接收解析（DataParser钩子 → 粘包/拆包/CRC/分发）

        private (string key, object value) ParseIncomingChunk(byte[] chunk)
        {
            if (chunk == null || chunk.Length == 0) return ("", null);

            lock (_rxLock)
            {
                _rxBuffer.AddRange(chunk);

                while (true)
                {
                    // 找帧头
                    int idx = -1;
                    for (int i = 0; i <= _rxBuffer.Count - 2; i++)
                    {
                        if (_rxBuffer[i] == 0xD2 && _rxBuffer[i + 1] == 0x8C) { idx = i; break; }
                    }
                    if (idx < 0)
                    {
                        if (_rxBuffer.Count > 8192) _rxBuffer.Clear();
                        break;
                    }
                    if (idx > 0) _rxBuffer.RemoveRange(0, idx); // 丢弃噪声

                    // 最小长度：16固定 + CRC2 = 18
                    if (_rxBuffer.Count < 18) break;

                    ushort typeBe = ReadU16_BE(_rxBuffer.ToArray(), 11);
                    // 长度：优先LE（示例 04 00），失败再试BE（兼容对端实现差异）
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

            // 我们不把数据写进父类 ReceiveCache（避免污染），故返回空键
            return ("", null);
        }

        private void DispatchFrame(ushort typeBe, byte[] payload)
        {
            try
            {
                switch (typeBe)
                {
                    case TYPE_REMOTE_CTRL_ACK: // 000F
                    case TYPE_INJECTION_ACK:   // 0019
                        {
                            if (_waiters.TryRemove(typeBe, out var tcs))
                                tcs.TrySetResult(payload);
                            break;
                        }
                    case TYPE_TLM_RESPONSE:    // 0023
                        {
                            // 解析 → 入队（可配置位切片）
                            var rec = ParseTelemetryPayload(payload);
                            _tlmQueue.Enqueue(rec);
                            TelemetryParsed?.Invoke(this, rec);
                            TelemetryReceived?.Invoke(this, new TelemetryEventArgs(payload));

                            if (_waiters.TryRemove(typeBe, out var tcs))
                                tcs.TrySetResult(payload);
                            break;
                        }
                    default:
                        _log.Debug($"收到未处理类型 0x{typeBe:X4}, Len={payload?.Length ?? 0}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Warn("DispatchFrame 出错", ex);
            }
        }

        #endregion

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
                Values = new Dictionary<string, object> { { "len", payload.Length } }
            };
        }

        private Dictionary<string, object> ParseSlices(byte[] payload)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in _tlmSlices!)
            {
                // 拼接参与字节 → 抽取位段 → 转目标类型 → 缩放
                ulong acc = AssembleBytes(payload, f.StartByte, f.ByteCount, f.Order);
                ulong raw = ExtractBits(acc, f.BitStart, f.BitLength);
                object v = CastToTarget(raw, f.BitLength, f.As);

                // 线性变换（数值型）
                if (v is sbyte sb) dict[f.Name] = sb * f.Scale + f.Offset;
                else if (v is byte ub) dict[f.Name] = ub * f.Scale + f.Offset;
                else if (v is short s16) dict[f.Name] = s16 * f.Scale + f.Offset;
                else if (v is ushort u16) dict[f.Name] = u16 * f.Scale + f.Offset;
                else if (v is int s32) dict[f.Name] = s32 * f.Scale + f.Offset;
                else if (v is uint u32) dict[f.Name] = u32 * f.Scale + f.Offset;
                else if (v is float f32) dict[f.Name] = f32 * f.Scale + f.Offset;
                else dict[f.Name] = v; // 其他保持原样
            }
            return dict;
        }

        // —— 位切片工具 —— //
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

        #region 基础工具（端序/CRC）

        private static byte[] U16_BE(ushort v) => new byte[] { (byte)(v >> 8), (byte)v };
        private static byte[] U16_LE(ushort v) => new byte[] { (byte)v, (byte)(v >> 8) };

        private static ushort ReadU16_BE(byte[] buf, int off) =>
            (ushort)((buf[off] << 8) | buf[off + 1]);

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

        #endregion
    }
}
