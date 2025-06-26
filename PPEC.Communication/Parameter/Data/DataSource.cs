using PPEC.Communication.Parameter.Utility.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Data
{
    public class DataSource<TPoint> : IDataSource<TPoint>
    {
        //Only create this if referenced.
        private readonly Lazy<TPoint[]> _points;

        private readonly object _syncRoot = new object();

        public DataSource()
        {
            _points = new Lazy<TPoint[]>(() => new TPoint[ushort.MaxValue]);
        }

        public TPoint[] ReadPoints(ushort startAddress, ushort numberOfPoints)
        {
            lock (_syncRoot)
            {
                return _points.Value
                    .Slice(startAddress, numberOfPoints)
                    .ToArray();
            }
        }

        public void WritePoints(ushort startAddress, TPoint[] points)
        {
            lock (_syncRoot)
            {
                for (ushort index = 0; index < points.Length; index++)
                {
                    _points.Value[startAddress + index] = points[index];
                }
            }
        }
    }
}
