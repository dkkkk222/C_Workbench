using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Utils
{
    public class Constants
    {
        public const string OldSERIAL_PORT = "串口";
        //串口
        public const string SERIAL_PORT = "UART";
        //I2C
        public const string I2C = "I2C";

        //CAN口
        public const string CAN_PORT = "CAN";

        //Modbus
        public const string Modbus = "UART";//"Modbus";

        //CAN
        public const string CAN = "CAN";

        //暗色主题
        public const string DarkTheme = "DarkTheme";

        //浅色主题
        public const string LightTheme = "LightTheme";

        //config.json文件路径
        public const string CONFIG_FILE_PATH = "config.json";

        //连接
        public const string Connect = "连接";

        //断开
        public const string Disconnect = "断开";

        //Select
        public const string Select = "Select";
        //TextBox
        public const string TextBox = "TextBox";

        public const string ConnectIcon = "\uE67e";
        public const string DisConnectIcon = "\uE64d";
        public const string ConnectStr = "连接";
        public const string DisConnectStr = "断开";
    }
}
