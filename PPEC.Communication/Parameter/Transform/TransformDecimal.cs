using PPEC.Communication.Parameter.Enum;
using PPEC.Communication.Parameter.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformDecimal : ITransform<decimal>
    {
        public decimal GetValue(ushort[] registers, IParamInfo info)
        {
            decimal ret = default; if (registers.Length < 1)
                return ret;

            byte[] value;
            if (registers.Length == 1)
            {
                throw new NotImplementedException();
            }
            switch (info.Endian)
            {
                case EndianEnum.ABCD:
                    value = BitConverter.GetBytes(registers[1]).Concat(BitConverter.GetBytes(registers[0])).ToArray();
                    break;
                case EndianEnum.CDAB:
                    value = BitConverter.GetBytes(registers[0]).Concat(BitConverter.GetBytes(registers[1])).ToArray();
                    break;
                default:
                    value = BitConverter.GetBytes(registers[0]).Concat(BitConverter.GetBytes(registers[1])).ToArray();
                    break;
            }
            var result = BitConverter.ToInt32(value, 0);

            var result1 = UtilityFunction.GetUnitReault<decimal>(result, info.Unit, info.MoveUnitRev, false);

            ret = UtilityFunction.ValueClip(result1, info.MaxValue, info.MinValue);
            return ret;
        }

        public byte[] GetRegistersBytes(decimal value, IParamInfo info)
        { return default; }
        public ushort[] GetRegisters(decimal value, IParamInfo info)
        {
            //value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);

            int ret = UtilityFunction.GetUnitReault(value, info.UnitRev, info.MoveUnitRev);

            ushort lowOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(ret), 0);
            ushort highOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(ret), 2);
            ushort[] retUshorts;
            switch (info.Endian)
            {
                case EndianEnum.ABCD:
                    retUshorts = new[] { highOrderValue, lowOrderValue };
                    break;
                case EndianEnum.CDAB:
                    retUshorts = new[] { lowOrderValue, highOrderValue };
                    break;
                default:
                    retUshorts = new[] { lowOrderValue, highOrderValue };
                    break;
            }

            return retUshorts;
        }
    }
}
