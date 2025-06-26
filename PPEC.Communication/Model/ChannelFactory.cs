using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;

namespace PPEC.Communication.Model
{
    public static class ChannelFactory
    {
        public static ICommChannel Create(ConnectPortType t, string id)
        {
            switch (t)
            {
                case ConnectPortType.UART: return new SerialCommChannel(id);       // id = “COM3”
                case ConnectPortType.CAN: return new CanCommChannel(uint.Parse(id)); // id = “0”
                case ConnectPortType.I2C: return new I2cCommChannel(byte.Parse(id)); // id = “160”(0xA0)
                default: throw new NotSupportedException();
            }
        }
    }
}
