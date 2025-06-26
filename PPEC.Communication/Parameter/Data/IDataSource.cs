using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Data
{
    public interface IDataSource<TPoint>
    {
        /// <summary>
        /// Read a series of points.
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <returns></returns>
        TPoint[] ReadPoints(ushort startAddress, ushort numberOfPoints);

        /// <summary>
        /// Write a series of points.
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="points"></param>
        void WritePoints(ushort startAddress, TPoint[] points);
    }
}
