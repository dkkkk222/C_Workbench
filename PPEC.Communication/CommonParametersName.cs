using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public static class CommonParametersName
    {
        public static string SerialPortReadName = "读-";
        public static string SerialPortWriteName = "写-";
        public static byte[] ChipStateQuery = { 0x01, 0x03, 0x00, 0x7B, 0x00, 0x01, 0xF4, 0x13 };

        public static int StaticSettingTimer = 300;

        public static string IsShowLoading = "Visible";
        public static string IsCollapsedLoading = "Collapsed";

        public static string IsShow = "Visible";
        public static string IsCollapsed = "Collapsed";

        public static string Connected = "连接";
        public static string DisConnected = "断开";

        public static string Start = "启动";
        public static string NStart = "未启动";
        public static string Stop = "停止";

        public static string StartStatesColor = "Green";
        public static string StopStatesColor = "Red";

        public static string ConnectedIMG = "/Resources/Black/Connect.png";
        public static string DisConnectedIMG = "/Resources/Black/Disconnection.png";

        public static string IsStartedImageSource = "/Resources/Black/Start.png";
        public static string IsStopImageSource = "/Resources/Black/Stop.png";

        public static string ShowPrecision = "精度";

        public static string SaveProjectName = "保存工程:";
        public static string RemoveProjectName = "移除工程:";
        public static string StaticSymbol = "__";
    }
}
