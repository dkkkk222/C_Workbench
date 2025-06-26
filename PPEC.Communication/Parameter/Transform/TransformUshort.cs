using PPEC.Communication.Parameter.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformUshort : ITransform<ushort>
    {
        public ushort GetValue(ushort[] registers, IParamInfo info)
        {
            ushort ret = default;
            if (registers.Length < 1)
                return ret;
            var tempValue = (ushort)(registers[0] * info.Unit);
            ret = UtilityFunction.ValueClip(tempValue, info.MaxValue, info.MinValue);
            return ret;
        }
        public byte[] GetRegistersBytes(ushort value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            value = (ushort)(value * info.UnitRev);
            var point = new byte[1] { (byte)value };
            return point;

        }
        public ushort[] GetRegisters(ushort value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            value = (ushort)(value * info.UnitRev);
            return new ushort[] { value };
        }
    }
}
