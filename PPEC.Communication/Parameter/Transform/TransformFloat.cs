using PPEC.Communication.Parameter.Enum;
using PPEC.Communication.Parameter.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformFloat : ITransform<float>
    {
        public float GetValue(ushort[] registers, IParamInfo info)
        {
            float ret = default;
            if (info.NumOfRegisters == 1)
                ret = registers[0] * info.Unit;
            else if (info.NumOfRegisters >= 2)
            {
                switch (info.Endian)
                {
                    case EndianEnum.ABCD:
                        ret = UtilityFunction.GetSingle(registers[0], registers[1]) * info.Unit;
                        break;
                    case EndianEnum.CDAB:
                        ret = UtilityFunction.GetSingle(registers[1], registers[0]) * info.Unit;
                        break;
                    default:
                        ret = UtilityFunction.GetSingle(registers[1], registers[0]) * info.Unit;
                        break;
                }
            }
            ret = UtilityFunction.ValueClip(ret, info.MaxValue, info.MinValue);
            return ret;
        }

        public ushort[] GetRegisters(float value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            value *= info.UnitRev;
            ushort[] points;
            if (info.NumOfRegisters <= 1)
            {
                points = new ushort[] { Convert.ToUInt16(value) };
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                var lowOrder = BitConverter.ToUInt16(bytes, 0);
                var highOrder = BitConverter.ToUInt16(bytes, 2);
                switch (info.Endian)
                {
                    case EndianEnum.ABCD:
                        points = new[] { highOrder, lowOrder };
                        break;
                    case EndianEnum.CDAB:
                        points = new[] { lowOrder, highOrder };
                        break;
                    default:
                        points = new[] { lowOrder, highOrder };
                        break;
                }
            }
            return points;
        }

        public byte[] GetRegistersBytes(float value, IParamInfo info)
        {
            value = UtilityFunction.ValueClip(value, info.MaxValue, info.MinValue);
            //value *= info.UnitRev;
            value = UtilityFunction.GetUnitReault<float>(value, info.UnitRev, info.MoveUnitRev);
            if (info.MoveUnitRev != null && info.MoveUnitRev != 0)
            {
                value = (int)value;
            }


            ushort[] points;
            byte[] points1;
            if (info.NumOfRegisters <= 1)
            {
                points = new ushort[] { Convert.ToUInt16(value) };

                points1 = new byte[1] { (byte)points[0] };
            }
            else if (info.NumOfRegisters == 2)
            {
                points = default;
                var temp = Convert.ToUInt16(value);
                var bytes = BitConverter.GetBytes(temp);
                var lowOrder = bytes[0];
                var highOrder = bytes[1];
                switch (info.Endian)
                {
                    case EndianEnum.ABCD:
                        points1 = new[] { highOrder, lowOrder };
                        break;
                    case EndianEnum.CDAB:
                        points1 = new[] { lowOrder, highOrder };
                        break;
                    default:
                        points1 = new[] { lowOrder, highOrder };
                        break;
                }
            }
            else
            {
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
                        //points1 = new[] { Order4, Order3, Order2, Order1 };
                        break;
                }
            }
            return points1;
        }
    }
}
