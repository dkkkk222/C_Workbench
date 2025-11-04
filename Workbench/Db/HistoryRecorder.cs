using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Workbench.Db.Tables;

namespace Workbench.Db
{
    public sealed class FrameRecord
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, double> Values { get; set; }  // 以数值为主
        public int Seq { get; set; } // 本周期内帧序号
    }
    public sealed class HistoryRecorderL2db : IDisposable
    {
        private readonly string _connStr;
        private readonly string _sessionId;
        private readonly BlockingCollection<FrameRecord> _queue;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _consumer;
        private volatile bool _recording;
        private int _seqCounter = 0;

        private const int BatchSize = 2000;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(200);

        // 参数字典缓存：name -> param_id
        private readonly Dictionary<string, int> _paramCache = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly object _paramLock = new object();

        public string SessionId => _sessionId;

        public HistoryRecorderL2db()
        {
            //_sessionId = sessionId;
            // 新 session
            using (var db = new DbContext())
            {
                _sessionId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
                db.Insert(new HistorySession
                {
                    SessionId = _sessionId,
                    StartedAt = DateTime.Now.ToString("o"),
                    EndedAt = null
                });
                // 预热参数缓存
                foreach (var p in db.ParamDicts)
                    _paramCache[p.Name] = p.ParamId;
            }

            _queue = new BlockingCollection<FrameRecord>(100_000);
            _consumer = Task.Factory.StartNew(ConsumeLoop, TaskCreationOptions.LongRunning);
        }

        public void Start() => _recording = true;
        public void Stop() => _recording = false;

        public void EnqueueFrame(DateTime ts, Dictionary<string, double> values)
        {
            if (!_recording) return;
            var seq = Interlocked.Increment(ref _seqCounter);
            _queue.Add(new FrameRecord { Timestamp = ts, Values = values, Seq = seq });
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            try { _consumer.Wait(); } catch { /* ignore */ }

            using (var db = new DbContext())
            {
                db.HistorySessions
                  .Where(s => s.SessionId == _sessionId)
                  .Set(s => s.EndedAt, DateTime.Now.ToString("o"))
                  .Update();
            }
        }

        private void ConsumeLoop()
        {
            var buffer = new List<FrameRecord>(BatchSize);
            var last = DateTime.UtcNow;

            try
            {
                foreach (var item in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    buffer.Add(item);
                    var need = buffer.Count >= BatchSize || (DateTime.UtcNow - last) >= FlushInterval;
                    if (need)
                    {
                        Flush(buffer);
                        buffer.Clear();
                        last = DateTime.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (buffer.Count > 0) Flush(buffer);
            }
        }

        private void Flush(List<FrameRecord> batch)
        {
            if (batch.Count == 0) return;

            // 1) 需要的参数名集合
            var needParamNames = batch.SelectMany(b => b.Values.Keys).Distinct(StringComparer.Ordinal).ToList();
            EnsureParamIds(needParamNames); // 填充 _paramCache

            // 2) 组装批量
            var frames = new List<HistoryFrame>(batch.Count);
            var values = new List<HistoryValue>(batch.Count * 64); // 预估

            foreach (var f in batch)
            {
                frames.Add(new HistoryFrame
                {
                    SessionId = _sessionId,
                    TsTicks = f.Timestamp.Ticks,
                    Seq = f.Seq
                });

                foreach (var kv in f.Values)
                {
                    int pid = _paramCache[kv.Key];
                    values.Add(new HistoryValue
                    {
                        SessionId = _sessionId,
                        TsTicks = f.Timestamp.Ticks,
                        Seq = f.Seq,
                        ParamId = pid,
                        NumValue = kv.Value
                    });
                }
            }

            // 3) 单事务 BulkCopy
            using (var db = new DbContext())
            using (var tran = db.BeginTransaction())
            {
                var bcOpt = new BulkCopyOptions
                {
                    BulkCopyType = BulkCopyType.MultipleRows, // SQLite 稳定方案
                    MaxBatchSize = 1000
                };

                db.BulkCopy(bcOpt, frames);
                db.BulkCopy(bcOpt, values);

                tran.Commit();
            }
        }

        private void EnsureParamIds(List<string> names)
        {
            var miss = new List<string>();
            lock (_paramLock)
            {
                foreach (var n in names)
                    if (!_paramCache.ContainsKey(n)) miss.Add(n);
            }
            if (miss.Count == 0) return;

            using (var db = new DbContext())
            using (var tran = db.BeginTransaction())
            {
                // 再次过滤（并发安全）
                var reallyMiss = new List<string>();
                lock (_paramLock)
                {
                    foreach (var n in miss)
                        if (!_paramCache.ContainsKey(n)) reallyMiss.Add(n);
                }
                if (reallyMiss.Count > 0)
                {
                    // 先查库里是否已有（避免并发重复）
                    var exist = db.ParamDicts.Where(p => reallyMiss.Contains(p.Name)).ToList();
                    lock (_paramLock)
                    {
                        foreach (var e in exist) _paramCache[e.Name] = e.ParamId;
                    }
                    var toInsert = reallyMiss.Except(exist.Select(e => e.Name), StringComparer.Ordinal).ToList();
                    foreach (var name in toInsert)
                    {
                        var id = db.InsertWithInt32Identity(new ParamDict
                        {
                            Name = name,
                            TypeCode = 0
                        });
                        lock (_paramLock) _paramCache[name] = id;
                    }
                }
                tran.Commit();
            }
        }
    }
}
