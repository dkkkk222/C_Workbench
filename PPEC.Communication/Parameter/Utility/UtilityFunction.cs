using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Utility
{
    public static class UtilityFunction
    {
        /// <summary>
        ///     Converts four UInt16 values into a IEEE 64 floating point format.
        /// </summary>
        /// <param name="b3">Highest-order ushort value.</param>
        /// <param name="b2">Second-to-highest-order ushort value.</param>
        /// <param name="b1">Second-to-lowest-order ushort value.</param>
        /// <param name="b0">Lowest-order ushort value.</param>
        /// <returns>IEEE 64 floating point value.</returns>
        public static double GetDouble(ushort b3, ushort b2, ushort b1, ushort b0)
        {
            byte[] value = BitConverter.GetBytes(b0)
                .Concat(BitConverter.GetBytes(b1))
                .Concat(BitConverter.GetBytes(b2))
                .Concat(BitConverter.GetBytes(b3))
                .ToArray();

            return BitConverter.ToDouble(value, 0);
        }

        /// <summary>
        ///     Converts two UInt16 values into a IEEE 32 floating point format.
        /// </summary>
        /// <param name="highOrderValue">High order ushort value.</param>
        /// <param name="lowOrderValue">Low order ushort value.</param>
        /// <returns>IEEE 32 floating point value.</returns>
        public static float GetSingle(ushort highOrderValue, ushort lowOrderValue)
        {
            byte[] value = BitConverter.GetBytes(lowOrderValue)
                .Concat(BitConverter.GetBytes(highOrderValue))
                .ToArray();

            return BitConverter.ToSingle(value, 0);
        }

        /// <summary>
        ///     Converts two UInt16 values into a UInt32.
        /// </summary>
        public static uint GetUInt32(ushort highOrderValue, ushort lowOrderValue)
        {
            byte[] value = BitConverter.GetBytes(lowOrderValue)
                .Concat(BitConverter.GetBytes(highOrderValue))
                .ToArray();

            return BitConverter.ToUInt32(value, 0);
        }

        /// <summary>
        ///     Converts an array of bytes to an ASCII byte array.
        /// </summary>
        /// <param name="numbers">The byte array.</param>
        /// <returns>An array of ASCII byte values.</returns>
        public static byte[] GetAsciiBytes(params byte[] numbers)
        {
            return Encoding.UTF8.GetBytes(numbers.SelectMany(n => n.ToString("X2")).ToArray());
        }

        /// <summary>
        ///     Converts an array of UInt16 to an ASCII byte array.
        /// </summary>
        /// <param name="numbers">The ushort array.</param>
        /// <returns>An array of ASCII byte values.</returns>
        public static byte[] GetAsciiBytes(params ushort[] numbers)
        {
            return Encoding.UTF8.GetBytes(numbers.SelectMany(n => n.ToString("X4")).ToArray());
        }

        public static T ValueClip<T>(T value, dynamic max, dynamic min)
        {
            T ret = value;
            if (max != null)
            {
                ret = ret > max ? (T)max : ret;
            }
            if (min != null)
            {
                ret = ret < min ? (T)min : ret;
            }
            return ret;
        }

        public static T GetUnitReault<T>(T ret, dynamic unit, dynamic moveUnit = null, bool md = true)
        {
            T returnRet = default;
            if (moveUnit == null || moveUnit == 0)
            {
                returnRet = (T)(ret * unit);
            }
            else
            {
                if (md)
                {
                    returnRet = (T)(ret * (1 << moveUnit));
                }
                else
                {
                    returnRet = (T)(ret / (1 << moveUnit));
                }

            }
            return returnRet;
        }

        public static int GetUnitReault(decimal ret, dynamic unit, dynamic moveUnit = null, bool md = true)
        {
            int returnRet = default;
            if (moveUnit == null || moveUnit == 0)
            {
                returnRet = (int)(ret * unit);
            }
            else
            {
                if (md)
                {
                    returnRet = (int)(ret * (1 << moveUnit));
                }
                else
                {
                    returnRet = (int)(ret / (1 << moveUnit));
                }

            }
            return returnRet;
        }
    }
}
