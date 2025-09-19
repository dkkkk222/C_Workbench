using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public sealed class SingleThreadInvoker : IDisposable
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<Action> _queue = new();
        private readonly ManualResetEventSlim _ready = new(false);

        public SingleThreadInvoker(string name = "CH347-IO")
        {
            _thread = new Thread(Loop) { IsBackground = true, Name = name };
            _thread.SetApartmentState(ApartmentState.MTA);
            _thread.Start();
            _ready.Wait();
        }

        private void Loop()
        {
            _ready.Set();
            foreach (var act in _queue.GetConsumingEnumerable())
            {
                try { act(); } catch { /* 交给上层 TaskCompletionSource */ }
            }
        }

        public Task<T> Invoke<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Add(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        public Task Invoke(Action action) => Invoke<object>(() => { action(); return null; });

        public void Dispose() => _queue.CompleteAdding();
    }
}
