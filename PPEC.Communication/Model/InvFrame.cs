using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public sealed class InvRealtimeMessage : IUartMessage
    {
        public short Vout { get; set; }      // 输出电压
        public short Iout { get; set; }      // 输出电流
        public short Phase { get; set; }     // 实时移相角
        public short PI { get; set; }        // PI 积分值
        public short FreqVout { get; set; }  // 频率输出电压
        public short FreqIout { get; set; }  // 频率输出电流
        public short TempValue { get; set; }
        public DateTime Timestamp { get; set; }

        public UartDataType Type => UartDataType.InvRealtime;
    }

    public sealed class InvRealtimeData : BindableBase
    {
        public double _Vout;
        public double Vout 
        {
            get=> _Vout;
            set=>SetProperty(ref _Vout, value);
        }      // 输出电压
        public double _Iout;
        public double Iout
        {
            get => _Iout;
            set => SetProperty(ref _Iout, value);
        }    // 输出电流
        public double Phase { get; set; }     // 实时移相角
        public double PI { get; set; }        // PI 积分值
        public double FreqVout { get; set; }  // 频率输出电压
        public double FreqIout { get; set; }  // 频率输出电流
        public double TempValue { get; set; }
    }
}
