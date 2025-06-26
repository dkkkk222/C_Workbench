using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Data
{
    public class CANModelParam
    {
        /// <summary>
        /// 显示值
        /// </summary>
        public object DisplayValue { get; set; } = 0;
        /// <summary>
        /// 收到的帧Byte[]
        /// </summary>
        public byte[] Recevie { get; set; }
        /// <summary>
        /// 发送的byte[]
        /// </summary>
        public byte[] ValueData { get; set; }
    }
}
