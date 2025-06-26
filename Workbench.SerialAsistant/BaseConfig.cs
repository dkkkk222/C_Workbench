using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant
{
    public class BaseConfig
    {
        /// <summary>
        /// 自动换行
        /// </summary>
        public bool AutoFeedLine { get; set; }
        public bool IsLoopSend { get; set; }
        public bool IsCrc { get; set; }
        public int SendInterval { get; set; }
        public int TimeOut { get; set; }
        public bool IsShowSend { get; set; }
        public bool IsShowReceive { get; set; }
    }
}
