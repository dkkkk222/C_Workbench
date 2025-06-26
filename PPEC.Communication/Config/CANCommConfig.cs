using PPEC.Communication.CAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Config
{
    public class CANCommConfig
    {
        public UInt32 DeviceType { get; set; } = Device.VCI_USBCAN2A; // Device.VCI_USBCAN_2E_U; // 设备类型号

        public UInt32 DeviceInd { get; set; } = 0;

        public UInt32 CanInd { get; set; } = 0;

        public int CanBaudRateIndex { get; set; } = 3;

        public uint SendTimeout { get; set; }

    }
}
