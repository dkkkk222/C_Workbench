using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Workbench.Utils.UtilsFunc;

namespace Workbench.Utils.Common
{
    public class SerialPortHelper
    {
        public static List<string> GetPortNames()
        {
            return SerialPort.GetPortNames().OrderBy(t => t, new NaturalStringComparer()).ToList();
        }
    }
}
