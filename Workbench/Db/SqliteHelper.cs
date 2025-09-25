using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;                 // 需要 Sum/ToList 等
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Workbench.Db.Tables;
using PPEC.Communication.Model;

namespace Workbench.Db
{
    public sealed class IngestPipeline : IDisposable
    {
        private readonly BlockingCollection<Sample> _queue;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _consumer;
        private readonly string _sessionId;

        // 批量参数
        private const int BatchSize = 2000;
        private const int FlushIntervalMs = 200;   // 除了数量阈值，增加时间阈值，降低尾部延迟

        public IngestPipeline(string sessionId, int boundedCapacity = 100_000)
        {
            _sessionId = sessionId;
            _queue = new BlockingCollection<Sample>(boundedCapacity);
            _consumer = Task.Factory.StartNew(ConsumeLoop, TaskCreationOptions.LongRunning);
        }

        public void Enqueue(Sample s) => _queue.Add(s);

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            try { _consumer.Wait(); } catch { /* ignore */ }
            _cts.Dispose();
            _queue.Dispose();
        }

        private static long NowMs() => (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;

        private void ConsumeLoop()
        {
            var batch = new List<Sample>(BatchSize);
            var lastFlushAt = NowMs();

            try
            {
                foreach (var s in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    batch.Add(s);

                    var needFlushByCount = batch.Count >= BatchSize;
                    var needFlushByTime = NowMs() - lastFlushAt >= FlushIntervalMs;

                    if (needFlushByCount || needFlushByTime)
                    {
                        FlushBatch(batch);
                        batch.Clear();
                        lastFlushAt = NowMs();
                    }
                }

                // 完成前把尾巴刷掉
                if (batch.Count > 0) FlushBatch(batch);
            }
            catch (OperationCanceledException) { /* 正常退出 */ }
        }

        private void FlushBatch(List<Sample> batch)
        {
            if (batch.Count == 0) return;

            using var db = new DbContext();
            using var tr = db.BeginTransaction();

            // 1) 插入 frames（逐条 InsertWithIdentity 以拿到自增 FrameId）
            var frames = new List<FrameRow>(batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                frames.Add(new FrameRow
                {
                    SessionId = _sessionId,
                    TsUtcMs = batch[i].TimestampUtcMs,
                });
            }

            for (int i = 0; i < frames.Count; i++)
            {
                // SQLite 下 InsertWithIdentity 性能尚可；需要逐个拿 ID 时最稳妥
                frames[i].FrameId = Convert.ToInt32(db.InsertWithIdentity(frames[i]));
            }

            // 2) 批量插入 values
            // 预估容量：如果你不想依赖 LINQ，请用下面的手写版
            int estimated = batch.Sum(b => b.Values?.Count ?? 0);

            // // 手写累计版（去掉上面一行，并保留 using System.Linq; 也行）
            // int estimated = 0;
            // for (int i = 0; i < batch.Count; i++)
            //     estimated += batch[i].Values?.Count ?? 0;

            var values = new List<ValueRow>(Math.Max(estimated, 4));

            for (int i = 0; i < batch.Count; i++)
            {
                var s = batch[i];
                var fid = frames[i].FrameId;

                if (s.Values == null) continue;

                foreach (var kv in s.Values)
                {
                    // —— 关键点：不要用 DBNull.Value，统一转换为 null —— //
                    // 假设 ValueRow.Result / ResultParse 是 double?（可空）。
                    // 如果你是 object 类型，也同样给 null。
                    //string parsed = ToNullableDouble(kv.Value);
                    
                    values.Add(new ValueRow
                    {
                        FrameId = fid,
                        ParamId = kv.Key,
                        ResultParse = kv.Value,         // null -> DB NULL
                        Result = kv.Value,         // 你的需求若不同可改
                        // RawHex    = ... 如需
                    });
                }
            }

            if (values.Count > 0)
            {
                db.BulkCopy(new BulkCopyOptions
                {
                    BulkCopyType = BulkCopyType.ProviderSpecific,
                    MaxBatchSize = 2000
                }, values);
            }

            tr.Commit();
        }

        // 将多种来源的数值统一为 double?；无效/空 -> null（交给 LinqToDB 变成 DB NULL）
        private static double? ToNullableDouble(object value)
        {
            if (value == null) return null;

            switch (value)
            {
                case double d: return double.IsNaN(d) ? (double?)null : d;
                case float f: return float.IsNaN(f) ? (double?)null : (double)f;
                case int i: return i;
                case long l: return l;
                case short s: return s;
                case byte b: return b;
                case decimal m: return (double)m;
                case string str:
                    if (string.IsNullOrWhiteSpace(str)) return null;
                    if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var dv))
                        return double.IsNaN(dv) ? (double?)null : dv;
                    return null;
                default:
                    try
                    {
                        var dv2 = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return double.IsNaN(dv2) ? (double?)null : dv2;
                    }
                    catch { return null; }
            }
        }
    }
}
