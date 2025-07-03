using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class RegisterAddrInfo : BindableBase
    {
        public uint AddressDec { get; set; }     // 十进制地址

        private string _addressHex;
        public string AddressHex
        {
            get => _addressHex;
            set => SetProperty(ref _addressHex, value);
        }  // 4 位 HEX 字符串
        public string Category { get; set; }    // 主分类
        public string SubCategory { get; set; } // 子分类
        public string Name { get; set; }        // 寄存器名称
        public string RW { get; set; }          // R/W
        private string _resetValue;
        public string ResetValue
        {
            get => _resetValue;
            set
            {
                ResetDecValue = uint.Parse(value);
                SetProperty(ref _resetValue, value);
            }
        }
        public uint ResetDecValue { get; set; }  // 复位值

        private byte[] _value;
        /// <summary>
        /// 寄存器值
        /// </summary>
        public byte[] Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {

                    DecValue = BitConverter.ToUInt32(value, 0);
                    HexValue = "0x" + DecValue.ToString("X8");
                    Utility.FillBitFieldValues(AddressDec, BitFields);
                    RaisePropertyChanged(nameof(BitFields));
                }

            }
        }
        private uint _decValue;
        /// <summary>
        /// 十进制值
        /// </summary>
        public uint DecValue
        {
            get => _decValue;
            set
            {
                if (SetProperty(ref _decValue, value))
                {
                    //_hexValue = "0x" + value.ToString("X8");
                }
            }
        }
        private string _hexValue = "0x00000000";
        /// <summary>
        /// Hex值
        /// </summary>
        public string HexValue
        {
            get
            {
                return _hexValue;
            }
            set
            {
                if (SetProperty(ref _hexValue, value))
                {
                    //var ui = Utility.ParseHexToUInt(value);
                    //_decValue = ui;
                }
            }
        }

        private List<BitField> _bitFields = new List<BitField>();
        /// <summary>
        /// 位值
        /// </summary>
        public List<BitField> BitFields
        {
            get => _bitFields;
            set
            {
                SetProperty(ref _bitFields, value);
            }
        }

        private string _binaryStr;
        /// <summary>
        /// 二进制字符串
        /// </summary>
        public string BinaryStr
        {
            get => _binaryStr;
            set => SetProperty(ref _binaryStr, value);
        }

        private ObservableCollection<ObservableCollection<BitOption>> _binaryArray = new ObservableCollection<ObservableCollection<BitOption>>()
        {
            new ObservableCollection<BitOption>()
            {

            }
        };
        /// <summary>
        /// 二进制数组
        /// </summary>
        public ObservableCollection<ObservableCollection<BitOption>> BinaryArray
        {
            get => _binaryArray;
            set => SetProperty(ref _binaryArray, value);
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

        private uint _value;
        public uint Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    Result = Math.Round(double.Parse(FormParam.ParamA), 2) * value + Math.Round(double.Parse(FormParam.ParamB), 2);
                }
            }
        }
        private double _result;
        public double Result
        {
            get => _result;
            set
            {
                SetProperty(ref _result, value);
            }
        }

    }
    public class BitOption : BindableBase
    {
        private uint _value;

        /// <summary>
        /// 对应数值（已右移到最低位）
        /// </summary>
        public uint Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _display;
        /// <summary>
        /// 显示文本
        /// </summary>
        public string Display
        {
            get => _display;
            set => SetProperty(ref _display, value);
        }
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
