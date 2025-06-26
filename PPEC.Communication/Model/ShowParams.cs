using PPEC.Communication.Enum;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public class ShowParams : BindableBase
    {
        public string SetName { get; set; }
        public AddressName AddressName { get; set; }
        public string ParamName { get; set; }
        public string ShowName { get; set; }
        public string ShowReaultName { get; set; }
        public string ShowToolTip { get; set; }
        public int Precision { get; set; } = 2;
        public string ShowPrecision { get; set; } = "精度1";

        public RegisterType RegType { get; set; }
        public string DefaultValue { get; set; }

        public dynamic MaxValue { get; set; }
        public dynamic MinValue { get; set; }

        public bool _Change = false;
        public bool Change
        {
            get => _Change;
            set
            {
                _Change = value;
                RaisePropertyChanged();
            }
        }
        public double _ShowValue = 0;
        public double ShowValue
        {
            get => _ShowValue;
            set
            {
                Change = true;
                if (value > MaxValue)
                    value = MaxValue;
                if (value < MinValue)
                    value = MinValue;

                var tempValue = Math.Round(value, Precision);

                SetProperty(ref _ShowValue, tempValue);
            }
        }
        public string ShowSuffix { get; set; }

        public bool _IsShowGrid = true;
        public bool IsShowGrid
        {
            get => _IsShowGrid;
            set
            {
                //_IsShowGrid = value;
                SetProperty(ref _IsShowGrid, value);
            }
        }
    }
    public struct FrameUart
    {
        public UartDataType DataType { get; }
        public ReadOnlyMemory<byte> Payload { get; }
        public FrameUart(UartDataType dt, ReadOnlyMemory<byte> payload)
        {
            DataType = dt;
            Payload = payload;
        }
    }
}
