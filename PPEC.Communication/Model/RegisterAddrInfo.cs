using System;
using System.Collections.Generic;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class RegisterAddrInfo : BindableBase
    {
        public uint AddressDec { get; set; }     // 十进制地址

        public string _AddressHex;
        public string AddressHex
        {
            get => _AddressHex;
            set => SetProperty(ref _AddressHex, value);
        }  // 4 位 HEX 字符串
        public string Category { get; set; }    // 主分类
        public string SubCategory { get; set; } // 子分类
        public string Name { get; set; }        // 寄存器名称
        public string RW { get; set; }          // R/W
        public string _ResetValue;
        public string ResetValue
        {
            get => _ResetValue;
            set
            {
                ResetDecValue = uint.Parse(value);
                SetProperty(ref _ResetValue, value);
            }
        }
        public uint ResetDecValue { get; set; }  // 复位值

        public byte[] _Value;
        /// <summary>
        /// 寄存器值
        /// </summary>
        public byte[] Value
        {
            get => _Value;
            set
            {
                if (SetProperty(ref _Value, value))
                {

                    DecValue = BitConverter.ToUInt32(value, 0);
                    HexValue = "0x" + DecValue.ToString("X8");
                    Utility.FillBitFieldValues(AddressDec, BitFields);
                    RaisePropertyChanged(nameof(BitFields));
                }

            }
        }
        public uint _DecValue = 0;
        /// <summary>
        /// 十进制值
        /// </summary>
        public uint DecValue
        {
            get => _DecValue;
            set
            {
                if (SetProperty(ref _DecValue, value))
                {
                    HexValue = "0x" + value.ToString("X8");
                }
            }
        }
        public string _HexValue = "";
        /// <summary>
        /// Hex值
        /// </summary>
        public string HexValue
        {
            get
            {
                return _HexValue;
            }
            set
            {
                if (SetProperty(ref _HexValue, value))
                {
                    DecValue = Utility.ParseHexToUInt(value);
                }
            }
        }

        public List<BitField> _BitFields = new List<BitField>();
        /// <summary>
        /// 位值
        /// </summary>
        public List<BitField> BitFields
        {
            get => _BitFields;
            set
            {
                SetProperty(ref _BitFields, value);
            }
        }
    }
    public class BitField : BindableBase
    {
        public int StartBit { get; set; }   // 低位
        public int EndBit { get; set; }   // 高位
        public int Length => EndBit - StartBit + 1;//位长度

        public string Desc { get; set; }

        /* 以下为新字段 —— 任选其一有值 */
        public List<BitOption> Options { get; set; } = new List<BitOption>(); // 离散取值
        public uint? RangeMin { get; set; }   // 连续范围最小值
        public uint? RangeMax { get; set; }   // 连续范围最大值

        public string ExtraNote { get; set; } // “先写后清除”等操作提示
        public FormulaParam FormParam { get; set; }

        public uint _Value;
        public uint Value
        {
            get => _Value;
            set
            {
                if (SetProperty(ref _Value, value))
                {
                    Result = Math.Round(double.Parse(FormParam.ParamA), 2) * value + Math.Round(double.Parse(FormParam.ParamB), 2);
                }
            }
        }
        public double _Result;
        public double Result
        {
            get => _Result;
            set
            {
                SetProperty(ref _Result, value);
            }
        }

    }
    public class BitOption
    {
        public uint Value { get; set; }         // 对应数值（已右移到最低位）
        public string Display { get; set; }     // 显示文本
    }
    public class FormulaParam
    {
        public string ParamName { get; set; }
        public string ParamA { get; set; }
        public string ParamB { get; set; }
        public string UnitName { get; set; }
    }
    public class RegisterMeta : BindableBase
    {
        public RegisterAddrInfo AddrInfo { get; set; }
    }
}
