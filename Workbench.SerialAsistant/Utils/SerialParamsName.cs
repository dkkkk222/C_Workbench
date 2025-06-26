using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant.Utils
{
    internal static class SerialParamsName
    {
        public const string None = "None";
        public const string One = "1";
        public const string OnePointFive = "1.5";
        public const string Two = "2";

        public const string Odd = "Odd";
        public const string Even = "Even";
        public const string Mark = "Mark";
        public const string Space = "Space";

        public const int BaudRate1 = 9600;
        public const int BaudRate2 = 19200;
        public const int BaudRate3 = 38400;
        public const int BaudRate4 = 57600;
        public const int BaudRate5 = 115200;

        public const string XOnXOff = "XOnXOff";
        public const string RequestToSend = "RTS/CTS";//"RequestToSend";
        public const string RequestToSendXOnXOff = "RequestToSendXOnXOff";
    }
}
