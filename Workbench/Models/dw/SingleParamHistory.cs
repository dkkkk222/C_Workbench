using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.dw
{
    public class SingleParamHistory
    {
        public string ReadWrite { get; set; }
        public string Address { get; set; }
        public string Hex { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string Datetime { get; set; }

    }

    public class SingleParamHistoryState
    {
        public const string Normal = "正常";
        public const string Error = "错误";
    }
}
