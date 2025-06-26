using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Model;

namespace PPEC.Communication.Common
{
    public static class RegisterCodec
    {
        // 将寄存器整值解析成人可读文本
        public static string Decode(uint regValue, RegisterMeta meta)
        {
            var sb = new StringBuilder();
            foreach (var bf in meta.BitFields)
            {
                uint fieldVal = (regValue >> bf.StartBit) &
                                ((uint)((1 << (bf.EndBit - bf.StartBit + 1)) - 1));

                var opt = bf.Options.FirstOrDefault(o => o.Value == fieldVal);
                sb.AppendLine($"{bf.Desc}: {(opt?.Display ?? fieldVal.ToString("X"))}");
            }
            return sb.ToString();
        }

        // 由选择项→写寄存器值（仅演示单个位域）
        public static uint Encode(BitField bf, uint rawRegValue, BitOption chosen)
        {
            uint mask = (uint)((1 << (bf.EndBit - bf.StartBit + 1)) - 1) << bf.StartBit;
            rawRegValue &= ~mask;                                // 清空位域
            rawRegValue |= (chosen.Value << bf.StartBit) & mask; // 写入
            return rawRegValue;
        }
    }
}
