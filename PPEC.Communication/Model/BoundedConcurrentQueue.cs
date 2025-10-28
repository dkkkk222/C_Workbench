using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public sealed class BoundedConcurrentQueue<T>
    {
        private readonly ConcurrentQueue<T> _q = new ConcurrentQueue<T>();
        private readonly int _capacity;
        private int _count;

        public BoundedConcurrentQueue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public int Count => _count;

        public void Enqueue(T item)
        {
            _q.Enqueue(item);
            int newCount = Interlocked.Increment(ref _count);
            while (newCount > _capacity && _q.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _count);
                newCount = _count;
            }
        }

        public bool TryDequeue(out T item)
        {
            var ok = _q.TryDequeue(out item);
            if (ok) Interlocked.Decrement(ref _count);
            return ok;
        }

        public T[] Snapshot() => _q.ToArray();

        // 兼容 C# 7.3 / .NET Framework 4.6.2 的实现
        public T[] TakeLast(int n)
        {
            if (n <= 0) return Array.Empty<T>();
            var arr = _q.ToArray();
            if (arr.Length <= n) return arr;

            int start = arr.Length - n;
            var result = new T[n];
            Array.Copy(arr, start, result, 0, n);
            return result;
        }

        public void Clear()
        {
            while (_q.TryDequeue(out _)) { }
            Interlocked.Exchange(ref _count, 0);
        }
    }
}
