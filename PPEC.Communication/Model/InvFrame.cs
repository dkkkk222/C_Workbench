using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;

namespace PPEC.Communication.Model
{
    public sealed class InvRealtimeMessage : IUartMessage
    {
        public ushort Vout { get; set; }      // 输出电压
        public ushort Iout { get; set; }      // 输出电流
        public ushort Phase { get; set; }     // 实时移相角
        public ushort PI { get; set; }        // PI 积分值
        public ushort FreqVout { get; set; }  // 频率输出电压
        public ushort FreqIout { get; set; }  // 频率输出电流
        public DateTime Timestamp { get; set; }

        public UartDataType Type => UartDataType.InvRealtime;
    }
}
