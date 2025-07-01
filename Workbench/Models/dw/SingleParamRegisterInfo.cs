using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.dw
{
    public class SingleParamRegisterInfo
    {
        public string Bit { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// 数据读取
        /// </summary>
        public string DataPull { get; set; }

        /// <summary>
        /// 数据读取解析
        /// </summary>
        public string DataPullResolve { get; set; }

        /// <summary>
        /// 数据下发
        /// </summary>
        public string DataPush { get; set; }

        /// <summary>
        /// 数据下发解析
        /// </summary>
        public string DataPushResolve { get; set; }


    }
}
