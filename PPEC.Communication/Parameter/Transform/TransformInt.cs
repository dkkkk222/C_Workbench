using PPEC.Communication.Parameter.Enum;
using PPEC.Communication.Parameter.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformInt : ITransform<Int32>
    {
        public int GetValue(ushort[] registers, IParamInfo info)
        {
            int ret = default; if (registers.Length < 1)
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
            ret = UtilityFunction.ValueClip(result, info.MaxValue, info.MinValue);
            return ret;
        }
        public byte[] GetRegistersBytes(int value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            byte[] points1;
            var bytes = BitConverter.GetBytes(value);
            var Order1 = bytes[0];
            var Order2 = bytes[1];
            var Order3 = bytes[2];
            var Order4 = bytes[3];
            switch (info.Endian)
            {
                case EndianEnum.ABCD:
                    points1 = new[] { Order1, Order2, Order3, Order4 };
                    break;
                case EndianEnum.CDAB:
                    Array.Reverse(bytes);
                    points1 = bytes;
                    //points1 = new[] { Order4, Order3, Order2, Order1 };
                    break;
                default:
                    Array.Reverse(bytes);
                    points1 = bytes;
                    break;
            }
            return points1;
        }
        public ushort[] GetRegisters(int value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);

            ushort lowOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(value), 0);
            ushort highOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(value), 2);
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
