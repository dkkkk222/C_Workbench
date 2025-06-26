using PPEC.Communication.Parameter.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformShort : ITransform<short>
    {
        public short GetValue(ushort[] registers, IParamInfo info)
        {
            short ret = default;
            if (registers.Length < 1)
                return ret;
            var ret1 = (short)(BitConverter.ToInt16(BitConverter.GetBytes(registers[0]), 0) * info.Unit);
            ret = UtilityFunction.ValueClip(ret1, info.MaxValue, info.MinValue);
            return ret;
        }
        public byte[] GetRegistersBytes(short value, IParamInfo info)
        {
            var ret = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            ret = (short)(ret * info.UnitRev);
            byte[] byteArray = BitConverter.GetBytes(ret);
            //var point = new byte[1] { (byte)value };
            return byteArray;
        }
        public ushort[] GetRegisters(short value, IParamInfo info)
        {
            var ret = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            ret = (short)(ret * info.UnitRev);
            byte[] byteArray = BitConverter.GetBytes(ret);
            ushort[] unsignedShortArray = new ushort[byteArray.Length / sizeof(ushort)];
            for (int i = 0; i < unsignedShortArray.Length; i++)
            {
                unsignedShortArray[i] = BitConverter.ToUInt16(byteArray, i * sizeof(ushort));
            }

            return unsignedShortArray;
        }
    }
}
