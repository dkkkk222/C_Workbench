using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.dw
{
    public class Sequence
    {
        public string Name { get; set; }

        /// <summary>
        /// 参数下发数量
        /// </summary>
        public int ParamPushNum { get; set; }

        /// <summary>
        /// 参数下发间隔
        /// </summary>
        public int ParamPushInterval { get; set; }

        /// <summary>
        /// 下发状态
        /// </summary>
        public string ParamPushState { get; set; }
    }
}
