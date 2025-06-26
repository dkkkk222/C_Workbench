using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Enums
{
    public enum CommandValue
    {
        INIT = 0,
        BOOT = 1, //开机（PWM输出）
        SHUT_DOWN = 2, //关机（PWM停止）
        RESET = 3, //故障复位
        DISCONNECT_PRECHARGE_CIRCUIT = 4,//断开预充电电路
        CONNECT_PRECHARGE_CIRCUIT = 5, //启动预充电电路
        STORE_DATA_TO_FLASH = 0xAA,//存储数据到flash
        RESTORE_DEFAULT_PARAMETERS = 0xBB,//恢复默认参数
    }
}
