using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public struct RegValue
    {
        public ushort Address;  // 地址
        public uint Raw;      // 原始 32 位
        public byte Width;    // 位宽: 16 或 32
        /* 方便的只读属性 */
        public ushort U16 => (ushort)(Raw & 0xFFFF);
        public int I16 => (short)U16;
        public int I32 => unchecked((int)Raw);

        public string Hex => Width == 16
                ? "0x" + U16.ToString("X4")
                : "0x" + Raw.ToString("X8");
        public string Bin => Width == 16
                ? Convert.ToString(U16, 2).PadLeft(16, '0')
                : Convert.ToString(Raw, 2).PadLeft(32, '0');
        public string Dec => Width == 16 ? U16.ToString() : Raw.ToString();
    }
}
