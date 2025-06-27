using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public class RegisterAddrInfo
    {
        public int AddressDec { get; set; }     // 十进制地址
        public string AddressHex { get; set; }  // 4 位 HEX 字符串
        public string Category { get; set; }    // 主分类
        public string SubCategory { get; set; } // 子分类
        public string Name { get; set; }        // 寄存器名称
        public string RW { get; set; }          // R/W
        public string ResetValue { get; set; }  // 复位值
    }
    public class BitField
    {
        public int StartBit { get; set; }   // 低位
        public int EndBit { get; set; }   // 高位
        public int Length => EndBit - StartBit + 1;

        public string Desc { get; set; }

        /* 以下为新字段 —— 任选其一有值 */
        public List<BitOption> Options { get; } = new List<BitOption>(); // 离散取值
        public uint? RangeMin { get; set; }   // 连续范围最小值
        public uint? RangeMax { get; set; }   // 连续范围最大值

        public string ExtraNote { get; set; } // “先写后清除”等操作提示
    }
    public class BitOption
    {
        public uint Value { get; set; }         // 对应数值（已右移到最低位）
        public string Display { get; set; }     // 显示文本
    }
    public class RegisterMeta
    {
        public RegisterAddrInfo AddrInfo { get; set; }
        public List<BitField> BitFields { get; } = new List<BitField>();
    }
}
