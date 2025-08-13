using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Model;
using LinqToDB.Data;
using LinqToDB;
using System.Threading;
using Workbench.Db.Tables;
using NPOI.HSSF.Util;

namespace Workbench.Db
{
    public sealed class IngestPipeline : IDisposable
    {
        private readonly BlockingCollection<Sample> _queue;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _consumer;
        private readonly string _sessionId;

        public IngestPipeline(string sessionId,int boundedCapacity = 100_000)
        {
            _queue = new BlockingCollection<Sample>(boundedCapacity);
            _sessionId = sessionId;// Guid.NewGuid().ToString();// StartSession();
            _consumer = Task.Factory.StartNew(ConsumeLoop, TaskCreationOptions.LongRunning);
        }

        public void Enqueue(Sample s) => _queue.Add(s);

        private static long NowMs() => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;

        private void ConsumeLoop()
        {
            const int batchSize = 2000;
            var batch = new List<Sample>(batchSize);

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (!_queue.TryTake(out var first, 50, _cts.Token)) continue;

                    batch.Clear();
                    batch.Add(first);
                    while (batch.Count < batchSize && _queue.TryTake(out var more))
                        batch.Add(more);

                    using var db = new DbContext();
                    using var tr = db.BeginTransaction();

                    // 1) 插入 frame
                    var frames = new List<FrameRow>(batch.Count);
                    foreach (var s in batch)
                    {
                        frames.Add(new FrameRow
                        {
                            SessionId = _sessionId,
                            TsUtcMs = s.TimestampUtcMs,
                        });
                    }
                    for (int i = 0; i < frames.Count; i++)
                    {
                        frames[i].FrameId = Convert.ToInt32(db.InsertWithIdentity(frames[i]));
                    }

                    // 2) 批量插入 value
                    var values = new List<ValueRow>(batch.Sum(b => b.Values?.Count ?? 0));
                    for (int i = 0; i < batch.Count; i++)
                    {
                        var s = batch[i];
                        var fid = frames[i].FrameId;

                        if (s.Values != null)
                        {
                            foreach (var kv in s.Values)
                            {
                                values.Add(new ValueRow
                                {
                                    FrameId = fid,
                                    ParamId = kv.Key,  // 直接用参数ID
                                    ResultParse = kv.Value,
                                    Result = kv.Value,
                                    //(s.RawHex != null && s.RawHex.TryGetValue(kv.Key, out var hx)) ? hx : null
                                });
                            }
                        }
                    }

                    db.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific }, values);

                    tr.Commit();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    // TODO: log
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _consumer.Wait(); } catch { }
            using var db = new DbContext(); 
            _cts.Dispose();
            _queue.Dispose();
        }
    }

}
