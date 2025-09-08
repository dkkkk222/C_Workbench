using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication.Model;
using Workbench.Db.Tables;
using Workbench.Utils;

namespace Workbench.Models
{
    #region —— WatchItem：包装监测信息 ——
    public sealed class WatchItem
    {
        public RegisterAddrInfo Param { get; }
        public DateTime StartUtc { get; }
        public TimeSpan Duration { get; }               // 记录时长；Infinite 表示永久

        public WatchItem(RegisterAddrInfo p, TimeSpan d)
        {
            Param = p ?? throw new ArgumentNullException(nameof(p));
            Duration = d;
            StartUtc = DateTime.Now;
        }

        public bool Expired() =>
            Duration != Timeout.InfiniteTimeSpan &&
            DateTime.UtcNow - StartUtc >= Duration;
    }
    #endregion
    public sealed class ParameterMonitorService
    {
        /* 线程安全监测池：Key = 参数 Id，Value = WatchItem */
        public readonly ConcurrentDictionary<string, WatchItem> _watching =
            new ConcurrentDictionary<string, WatchItem>();

        /* 循环控制 */
        private readonly object _loopLock = new object();
        private CancellationTokenSource _cts;
        private volatile bool _enabled = false;

        private readonly int _intervalMs;         // 每条下发间隔 (ms)
        public PpecProject CurrentProject { get; set; }

        public ParameterMonitorService(int intervalMs = 20)
        {
            if (intervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMs));
            _intervalMs = intervalMs;
        }

        #region —— 显式启停循环 ——
        public void Enable()
        {
            _enabled = true;
            TryStartLoop();
        }
        public void Disable()
        {
            _enabled = false;
            StopLoop();
        }
        #endregion

        #region —— 参数级控制 ——
        /// <summary>
        /// 开始记录：duration ≤0 ➜ 永久；否则到时自动停止
        /// </summary>
        public void StartRecord(RegisterAddrInfo param, TimeSpan? duration = null)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            var d = duration ?? Timeout.InfiniteTimeSpan;
            if (d <= TimeSpan.Zero) d = Timeout.InfiniteTimeSpan;

            _watching[param.Id] = new WatchItem(param, d);
            TryStartLoop();
        }

        public void StopRecord(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            _watching.TryRemove(id, out _);
            if (_watching.IsEmpty) StopLoop();
        }
        #endregion

        #region —— 后台循环 ——
        private void TryStartLoop()
        {
            if (!_enabled || _watching.IsEmpty) return;

            lock (_loopLock)
            {
                if (_cts != null && !_cts.IsCancellationRequested) return;
                _cts = new CancellationTokenSource();
                Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
            }
        }

        private void StopLoop()
        {
            lock (_loopLock)
            {
                _cts?.Cancel();
                _cts = null;
            }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            var sw = new Stopwatch();
            try
            {
                while (!token.IsCancellationRequested &&
                       _enabled &&
                       !_watching.IsEmpty)
                {
                    /* 快照，避免 await 期间字典变动干扰枚举器 */
                    foreach (var kv in _watching.ToArray())
                    {
                        var id = kv.Key;
                        var item = kv.Value;
                        sw.Restart();

                        /* 1️⃣ 下发 */
                        var cmd = UtilsFunc.GetReadCommandByAddress(
                                      item.Param.AddressHex,
                                      CurrentProject.CommunicationType);

                        switch (CurrentProject.CommunicationType)
                        {
                            case Constants.Modbus:
                                await CurrentProject.CommService.SendAsync(cmd.bytes);
                                break;
                            case Constants.I2C:
                                if (ushort.TryParse(item.Param.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                                {
                                    await CurrentProject.CommService.ReadRegisterAsync(reg);
                                }
                                //CurrentProject.CommService.Read(item.Param.AddressHex);
                                break;
                            case Constants.CAN:
                                if (ushort.TryParse(item.Param.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg1))
                                {
                                    await CurrentProject.CommService.ReadRegisterAsync(reg1);
                                }
                                break;
                            default:
                                await CurrentProject.CommService.SendAsync(cmd.bytes);
                                break;
                        }

                       

                        /* 2️⃣ 是否到期？到期就移除 */
                        if (item.Expired())
                        {
                            _watching[id].Param.IsStartRecord = false;
                            _watching.TryRemove(id, out _);
                        }
                            

                        /* 3️⃣ 补足间隔 */
                        var gap = _intervalMs - (int)sw.ElapsedMilliseconds;
                        if (gap > 0)
                            await Task.Delay(gap, token).ConfigureAwait(false);
                    }

                    /* 如果循环结束后集合为空 → 自动停 */
                    if (_watching.IsEmpty)
                        StopLoop();
                }
            }
            catch (OperationCanceledException) { /* 正常退出 */ }
            finally
            {
                lock (_loopLock) _cts = null;
            }
        }
        #endregion
    }
}
