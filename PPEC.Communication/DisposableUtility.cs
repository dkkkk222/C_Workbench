using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    internal static class DisposableUtility
    {
        public static void Dispose<T>(ref T item)
            where T : class, IDisposable
        {
            if (item == null)
            {
                return;
            }

            item.Dispose();
            item = default(T);
        }
    }
}
