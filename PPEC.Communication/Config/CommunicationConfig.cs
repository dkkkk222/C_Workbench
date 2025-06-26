using PPEC.Communication.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Config
{
    public class CommunicationConfig
    {
        public SerialPortConfig SerialConfig { get; set; }
        public NetworkConfig NetWorkConfig { get; set; }
        public PortType PortType { get; set; }
    }
}
