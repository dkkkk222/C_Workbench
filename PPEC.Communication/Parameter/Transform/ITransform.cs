using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public interface ITransform<T>
    {
        T GetValue(ushort[] registers, IParamInfo info);
        ushort[] GetRegisters(T value, IParamInfo info);
        byte[] GetRegistersBytes(T value, IParamInfo info);
    }
}
