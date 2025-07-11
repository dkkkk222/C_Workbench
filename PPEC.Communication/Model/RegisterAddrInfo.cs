using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class RegisterAddrInfo : BindableBase
    {
        private string _id;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

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

        public bool AllowRead => RW.Contains("R");
        public bool AllowWrite => RW.Contains("W");

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

        private string _binaryStr = string.Concat(Enumerable.Range(0, 32).Select(t => "0"));
        /// <summary>
        /// 二进制字符串
        /// </summary>
        [JsonIgnore]
        public string BinaryStr
        {
            get => _binaryStr;
            set
            {
                SetProperty(ref _binaryStr, value);
                foreach (var bf in BitFields)
                {
                    if (string.IsNullOrEmpty(value))
                        bf.ReadBinary = string.Empty;
                    else
                        bf.ReadBinary = Utility.GetBitRange(value, bf.EndBit, bf.Length);
                }
            }
        }

        private ObservableCollection<ObservableCollection<BitOption>> _binaryArray = new ObservableCollection<ObservableCollection<BitOption>>()
        {
            new ObservableCollection<BitOption>()
            {
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0}
            },
            new ObservableCollection<BitOption>()
            {
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0}
            },
            new ObservableCollection<BitOption>()
            {
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0}
            },
            new ObservableCollection<BitOption>()
            {
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0},
                new BitOption{Value=0}
            }
        };
        /// <summary>
        /// 二进制数组
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ObservableCollection<BitOption>> BinaryArray
        {
            get => _binaryArray;
            set => SetProperty(ref _binaryArray, value);
        }

        private ObservableCollection<BitOption> _binaryList = new ObservableCollection<BitOption>()
        {
            new BitOption{Value=0,Display="31"},
            new BitOption{Value=0,Display="30"},
            new BitOption{Value=0,Display="29"},
            new BitOption{Value=0,Display="28"},
            new BitOption{Value=0,Display="27"},
            new BitOption{Value=0,Display="26"},
            new BitOption{Value=0,Display="25"},
            new BitOption{Value=0,Display="24"},
            new BitOption{Value=0,Display="23"},
            new BitOption{Value=0,Display="22"},
            new BitOption{Value=0,Display="21"},
            new BitOption{Value=0,Display="20"},
            new BitOption{Value=0,Display="19"},
            new BitOption{Value=0,Display="18"},
            new BitOption{Value=0,Display="17"},
            new BitOption{Value=0,Display="16"},
            new BitOption{Value=0,Display="15"},
            new BitOption{Value=0,Display="14"},
            new BitOption{Value=0,Display="13"},
            new BitOption{Value=0,Display="12"},
            new BitOption{Value=0,Display="11"},
            new BitOption{Value=0,Display="10"},
            new BitOption{Value=0,Display="9"},
            new BitOption{Value=0,Display="8"},
            new BitOption{Value=0,Display="7"},
            new BitOption{Value=0,Display="6"},
            new BitOption{Value=0,Display="5"},
            new BitOption{Value=0,Display="4"},
            new BitOption{Value=0,Display="3"},
            new BitOption{Value=0,Display="2"},
            new BitOption{Value=0,Display="1"},
            new BitOption{Value=0,Display="0"},
        };

        [JsonIgnore]
        public ObservableCollection<BitOption> BinaryList
        {
            get => _binaryList;
            set => SetProperty(ref _binaryList, value);
        }

        private int _recordTime = 1;
        /// <summary>
        /// 记录时间
        /// </summary>
        public int RecordTime
        {
            get => _recordTime;
            set => SetProperty(ref _recordTime, value);
        }

        private bool _isAddToPlot;
        /// <summary>
        /// 是否添加到波形
        /// </summary>
        public bool IsAddToPlot
        {
            get => _isAddToPlot;
            set => SetProperty(ref _isAddToPlot, value);
        }
    }
    public class BitField : BindableBase
    {
        public int StartBit { get; set; }   // 低位
        public int EndBit { get; set; }   // 高位
        public int Length => EndBit - StartBit + 1;//位长度

        public string Name { get; set; }

        public string Desc { get; set; }


        private string _fieldType;
        public string FieldType
        {
            get => _fieldType;
            set => SetProperty(ref _fieldType, value);
        }

        /* 以下为新字段 —— 任选其一有值 */
        //public List<BitOption> Options { get; set; } = new List<BitOption>();

        private ObservableCollection<BitOption> _options = new ObservableCollection<BitOption>();
        public ObservableCollection<BitOption> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

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

        private string _readBinary;
        /// <summary>
        /// 数据读取
        /// </summary>
        [JsonIgnore]
        public string ReadBinary
        {
            get => _readBinary;
            set => SetProperty(ref _readBinary, value);
        }

        private string _writeBinary = "";
        /// <summary>
        /// 数据下发
        /// </summary>
        [JsonIgnore]
        public string WriteBinary
        {
            get => _writeBinary;
            set => SetProperty(ref _writeBinary, value);
        }

        private string _selectedValue;
        [JsonIgnore]
        public string SelectedValue
        {
            get => _selectedValue;
            set => SetProperty(ref _selectedValue, value);
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

        private string _key;
        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
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
