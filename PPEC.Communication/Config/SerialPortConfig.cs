using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Config
{
    public class SerialPortConfig
    {
        public string SerialProtName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public int BufferSize { get; set; }
        public Handshake FlowControl { get; set; }
        public int ReadTimeOut { get; set; }
        public int WriteTimeOut { get; set; }
        public int? Retries { get; set; }
    }
}
