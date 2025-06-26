using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;

namespace PPEC.Communication
{
    public static class ProtocolFactory
    {
        public static IProtocolHandler Create(ConnectPortType t)
        {
            switch (t)
            {
                case ConnectPortType.UART: return new UartProtocolHandler(); // = UartParser + Decoder
                //case ConnectPortType.CAN: return new CanProtocolHandler();  // 你稍后实现
                default: throw new NotSupportedException();
            }
        }

        public static ICommChannel CreateChannel(ConnectPortType t, string portOrIdx)
        {
            switch (t)
            {
                case ConnectPortType.UART: return new SerialCommChannel(portOrIdx);
                case ConnectPortType.CAN: return new CanCommChannel(Convert.ToUInt32(portOrIdx)); // 例: 0/1
                default: throw new NotSupportedException();
            }
        }
    }
}
