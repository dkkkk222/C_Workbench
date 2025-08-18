using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.CAN;
using System.Windows;
using NPOI.SS.Formula.Functions;

namespace Workbench.Models
{
    /// <summary>
    /// 供 UI 传参用：用户选择的设备类型/设备号/通道/波特率
    /// DevType: 参见 Device.VCI_USBCAN_2E_U(21) 等
    /// DevId:   同型号的第几个设备 (0,1,2…)
    /// CanId:   通道号 0/1
    /// BaudIndex: 你的 ControlCAN.GetBaudTimingCase2 的索引（如 1=500kbps）
    /// </summary>
    public sealed class CanConnectOptions
    {
        public uint DevType { get; set; }
        public uint DevId { get; set; }
        public uint CanId { get; set; }
        public int BaudIndex { get; set; } = 1; // 默认 500kbps（按你的映射）
        public uint? SendTimeoutMs { get; set; }  // 可选：发送超时
    }

    //1.ZLGCAN系列接口卡信息的数据类型。
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_BOARD_INFO
    {
        public UInt16 hw_Version;
        public UInt16 fw_Version;
        public UInt16 dr_Version;
        public UInt16 in_Version;
        public UInt16 irq_Num;
        public byte can_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Reserved;
    }


    /////////////////////////////////////////////////////
    //2.定义CAN信息帧的数据类型。
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_CAN_OBJ
    {
        public uint ID;
        public uint TimeStamp;
        public byte TimeFlag;
        public byte SendType;
        public byte RemoteFlag;//是否是远程帧，0为数据帧，1为远程帧
        public byte ExternFlag;//是否是扩展帧，0为标准帧，1为扩展帧
        public byte DataLen;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;
    }
    ////2.定义CAN信息帧的数据类型。
    //[StructLayout(LayoutKind.Sequential)]
    //public struct VCI_CAN_OBJ 
    //{
    //    public UInt32 ID;
    //    public UInt32 TimeStamp;
    //    public byte TimeFlag;
    //    public byte SendType;
    //    public byte RemoteFlag;//是否是远程帧
    //    public byte ExternFlag;//是否是扩展帧
    //    public byte DataLen;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    //    public byte[] Data;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    //    public byte[] Reserved;

    //    public void Init()
    //    {
    //        Data = new byte[8];
    //        Reserved = new byte[3];
    //    }
    //}

    //3.定义CAN控制器状态的数据类型。
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_CAN_STATUS
    {
        public byte ErrInterrupt;
        public byte regMode;
        public byte regStatus;
        public byte regALCapture;
        public byte regECCapture;
        public byte regEWLimit;
        public byte regRECounter;
        public byte regTECounter;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved;
    }

    //4.定义错误信息的数据类型。
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_ERR_INFO
    {
        public UInt32 ErrCode;
        public byte Passive_ErrData1;
        public byte Passive_ErrData2;
        public byte Passive_ErrData3;
        public byte ArLost_ErrData;
    }

    //5.定义初始化CAN的数据类型
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_INIT_CONFIG
    {
        public UInt32 AccCode;
        public UInt32 AccMask;
        public UInt32 Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CHGDESIPANDPORT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] szpwd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] szdesip;
        public Int32 desport;

        public void Init()
        {
            szpwd = new byte[10];
            szdesip = new byte[20];
        }
    }

    public struct Device
    {
        public const int VCI_PCI5121 = 1;
        public const int VCI_PCI9810 = 2;
        public const int VCI_USBCAN1 = 3;
        public const int VCI_USBCAN2 = 4;
        public const int VCI_USBCAN2A = 4;
        public const int VCI_PCI9820 = 5;
        public const int VCI_CAN232 = 6;
        public const int VCI_PCI5110 = 7;
        public const int VCI_CANLITE = 8;
        public const int VCI_ISA9620 = 9;
        public const int VCI_ISA5420 = 10;
        public const int VCI_PC104CAN = 11;
        public const int VCI_CANETUDP = 12;
        public const int VCI_CANETE = 12;
        public const int VCI_DNP9810 = 13;
        public const int VCI_PCI9840 = 14;
        public const int VCI_PC104CAN2 = 15;
        public const int VCI_PCI9820I = 16;
        public const int VCI_CANETTCP = 17;
        public const int VCI_PEC9920 = 18;
        public const int VCI_PCI5010U = 19;
        public const int VCI_USBCAN_E_U = 20;
        public const int VCI_USBCAN_2E_U = 21;
        public const int VCI_PCI5020U = 22;
        public const int VCI_EG20T_CAN = 23;
        public const int VCI_PCIE9221 = 24;
        public const int VCI_CANDTU200 = 32;

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ReadErrInfo(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_ERR_INFO pErrInfo);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ReadCANStatus(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_STATUS pCANStatus);

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_GetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);
        //[DllImport("controlcan.dll")]
        //public static extern UInt32 VCI_SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, IntPtr pData);

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        public static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, VCI_CAN_OBJ[] pSend, UInt32 Len);

        //[DllImport("controlcan.dll")]
        //static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pReceive, UInt32 Len, Int32 WaitTime);
        [DllImport("controlcan.dll", CharSet = CharSet.Ansi)]
        public static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, IntPtr pReceive, UInt32 Len, Int32 WaitTime);
    };
    // 函数调用返回状态值
    public enum STATUS
    {
        STATUS_OK = 1,
        STATUS_ERR = 0,
    };

    // 函数调用返回状态值
    public enum CMD
    {
        CMD_DESIP = 0,
        CMD_DESPORT = 1,
        CMD_SRCPORT = 2,
        CMD_CHGDESIPANDPORT = 2,
        CMD_TCP_TYPE = 4,//tcp 工作方式，服务器:1 或是客户端:0
    };

    public enum TCPTYPE
    {
        TCP_CLIENT = 0,
        TCP_SERVER = 1,
    }

    public enum REF
    {
        REFERENCE_BAUD = 1,
        REFERENCE_SET_TRANSMIT_TIMEOUT = 2,
        REFERENCE_ADD_FILTER = 3,
        REFERENCE_SET_FILTER = 4,
    };
    public class CANDevice
    {
        public uint DevType { get; set; }
        public uint DevID { get; set; }
        public uint CANID { get; set; }
        public int BoudRateIndex { get; set; }
        public uint SendTimeout { get; set; }

        public CANDevice(uint devType, uint devID, uint canID = 0, int baudRateIndex = 0, uint sendTimeout = 400)
        {
            this.DevType = devType;
            this.DevID = devID;
            this.CANID = canID;
            this.BoudRateIndex = baudRateIndex;
            this.SendTimeout = sendTimeout;
        }
    }


    public class ControlCAN
    {
        private CANDevice _objCANDevice;

        public ControlCAN(CANDevice canDevice)
        {
            _objCANDevice = canDevice;
        }

        public uint GetCurrentCanId()
        {
            return _objCANDevice.CANID;
        }

        /// <summary>
        /// 打开CAN设备
        /// </summary>
        /// <returns></returns>
        public bool OpenCAN()
        {
            if (Device.VCI_OpenDevice(_objCANDevice.DevType, _objCANDevice.DevID, 0) == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 启动某一路CAN
        /// </summary>
        public bool StartCan(uint canId = 0)
        {
            if (canId != 0)
            {
                _objCANDevice.CANID = canId;
            }

            var config = GetInitConfig(_objCANDevice.DevType);
            if (config == null) return false;
            var pInitConfig = config.Value;

            if (!InitCAN(pInitConfig))
            {
                return false;
            }

            if (Device.VCI_StartCAN(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID) == 0)
            {
                MessageBox.Show("StartCAN失败", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            //if (!SetSendTimeout()) {
            //    throw new Exception("发送超时设置失败!");
            //}
            ClearBuffer();

            return true;
        }

        private VCI_INIT_CONFIG? GetInitConfig(uint deviceType)
        {
            VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
            switch (deviceType)
            {
                case Device.VCI_PCI5010U:
                case Device.VCI_PCI5020U:
                case Device.VCI_USBCAN_E_U:
                case Device.VCI_USBCAN_2E_U:
                    // 设置波特率
                    if (!SetBaudRate(GetBaudTimingCase2(_objCANDevice.BoudRateIndex))) return null;
                    config.Mode = 0;
                    break;
                default:
                    config.AccCode = Convert.ToUInt32("0x" + "00000000", 16);
                    config.AccMask = Convert.ToUInt32("0x" + "FFFFFFFF", 16);
                    var timings = GetBaudTiming(_objCANDevice.BoudRateIndex);
                    config.Timing0 = timings[0];
                    config.Timing1 = timings[1];
                    config.Filter = 1;
                    config.Mode = 0;
                    break;
            }
            return config;
        }

        private List<byte> GetBaudTiming(int index)
        {
            List<byte> result = new();
            switch (index)
            {
                case 0:
                    result.Add(Convert.ToByte("0x00", 16));
                    result.Add(Convert.ToByte("0x14", 16));
                    break;
                case 1:
                    result.Add(Convert.ToByte("0x00", 16));
                    result.Add(Convert.ToByte("0x16", 16));
                    break;
                case 2:
                    result.Add(Convert.ToByte("0x00", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 3:
                    result.Add(Convert.ToByte("0x01", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 4:
                    result.Add(Convert.ToByte("0x03", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 5:
                    result.Add(Convert.ToByte("0x04", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 6:
                    result.Add(Convert.ToByte("0x09", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 7:
                    result.Add(Convert.ToByte("0x18", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
                case 8:
                    result.Add(Convert.ToByte("0x31", 16));
                    result.Add(Convert.ToByte("0x1C", 16));
                    break;
            }
            return result;
        }

        //private uint GetBaudTimingCase2(int index) {
        //    uint result = 0x160023;
        //    switch (index) {
        //        case 0:
        //            result = 0x060003;//1000K
        //            break;
        //        case 1:
        //            result = 0x060004;//800K
        //            break;
        //        case 2:
        //            result = 0x060007;//500K
        //            break;
        //        case 3:
        //            result = 0x1C0008;//250K
        //            break;
        //        case 4:
        //            result = 0x1C0011;
        //            break;
        //        case 5:
        //            result = 0x160023;
        //            break;
        //        case 6:
        //            result = 0x1C002C;
        //            break;
        //        case 7:
        //            result = 0x1600B3;
        //            break;
        //        case 8:
        //            result = 0x1C00E0;
        //            break;
        //    }
        //    return result;
        //}

        private uint GetBaudTimingCase2(int index)
        {
            uint result = 0x160023;
            switch (index)
            {
                case 0:
                    result = 0x1C01C1;//5K
                    break;
                case 1:
                    result = 0x1C00E0;//10K
                    break;
                case 2:
                    result = 0x1600B3;//20K
                    break;
                case 3:
                    result = 0x1C002C;//50K
                    break;
                case 4:
                    result = 0x160023;//100k
                    break;
                case 5:
                    result = 0x1C0011;//125k
                    break;
                case 6:
                    result = 0x1C0008;//250K
                    break;
                case 7:
                    result = 0x060007;//500K
                    break;
                case 8:
                    result = 0x060004;//800K
                    break;
                case 9:
                    result = 0x060003;//1000K
                    break;
            }
            return result;
        }

        /// <summary>
        /// 设置波特率
        /// </summary>
        /// <param name="baudRate">uint类型</param>
        /// <returns></returns>
        public bool SetBaudRate(uint baudRate)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(baudRate));
            try
            {
                Marshal.WriteInt32(ptr, (int)baudRate);
                if (Device.VCI_SetReference(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, 0, ptr) == 0)
                {
                    MessageBox.Show("SetReference设置波特率失败", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return true;
        }

        /// <summary>
        /// 设置工作模式，需提供工作模式Id
        /// 必须先设置波特率再设置工模式
        /// </summary>
        /// <param name="modeId"> =0 表示正常模式（相当于正常节点）， =1 表示只听模式（只接收，不影响总线）</param>
        /// <returns></returns>
        public bool SetWorkingMode(byte modeId = 0)
        {
            VCI_INIT_CONFIG initConfig = new VCI_INIT_CONFIG();
            initConfig.Mode = modeId;//正常模式
            if (Device.VCI_InitCAN(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, ref initConfig) == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///设置发送超时时间
        /// </summary>
        public bool SetSendTimeout(uint timeout = 2000)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(timeout));
            try
            {
                Marshal.WriteInt32(ptr, (int)timeout);
                if (Device.VCI_SetReference(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, 4, ptr) == 0)
                {
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return true;
        }

        /// <summary>
        /// 设置CAN相关参数
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool InitCAN(VCI_INIT_CONFIG config)
        {
            VCI_INIT_CONFIG obj = new VCI_INIT_CONFIG
            {
                AccCode = config.AccCode,
                AccMask = config.AccMask,
                Mode = config.Mode,
                Filter = config.Filter,
                Timing0 = config.Timing0,
                Timing1 = config.Timing1,
                Reserved = config.Reserved
            };

            if (Device.VCI_InitCAN(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, ref obj) == 0)
            {
                MessageBox.Show("InitCAN失败", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 查看缓存区是否存在未读取数据
        /// </summary>
        /// <returns></returns>
        public bool HasReceive()
        {
            return Device.VCI_GetReceiveNum(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID) != 0;
        }

        public bool Transmit(VCI_CAN_OBJ frame)
        {
            VCI_CAN_OBJ[] frames = new VCI_CAN_OBJ[1];
            frames[0] = new VCI_CAN_OBJ
            {
                ID = frame.ID,
                TimeFlag = frame.TimeFlag,
                TimeStamp = frame.TimeStamp,
                SendType = frame.SendType,
                RemoteFlag = frame.RemoteFlag,
                ExternFlag = frame.ExternFlag,
                Data = frame.Data,
                DataLen = frame.DataLen,
                Reserved = frame.Reserved
            };
            return Device.VCI_Transmit(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, frames, (uint)frames.Length) != 0;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="frames">帧结构体数组</param>
        /// 举例：VCI_CAN_OBJ[] frames=new VCI_CAN_OBJ[2];//将发送两帧数据
        /// frames[0].ID=0x00000001;//第一帧ID
        /// frames[0].SendType=0;//正常发送
        /// frames[0].RemoteFlag=0;//数据帧
        /// frames[0].ExternFlag=0;//标准帧
        /// frames[0].DataLen=1;//数据长度
        /// frames[0].Data[0]=0x56;//数据
        /// frames[1]~
        /// <returns></returns>
        public bool Transmit(VCI_CAN_OBJ[] frames)
        {
            int length = frames.Length;
            VCI_CAN_OBJ[] obj = new VCI_CAN_OBJ[length];
            for (int i = 0; i < length; i++)
            {
                obj[i] = new VCI_CAN_OBJ
                {
                    ID = frames[i].ID,
                    TimeFlag = frames[i].TimeFlag,
                    TimeStamp = frames[i].TimeStamp,
                    SendType = frames[i].SendType,
                    RemoteFlag = frames[i].RemoteFlag,
                    ExternFlag = frames[i].ExternFlag,
                    Data = frames[i].Data,
                    DataLen = frames[i].DataLen,
                    Reserved = frames[i].Reserved
                };
            }
            return Device.VCI_Transmit(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, obj, (uint)length) != 0;
        }

        /// <summary>
        /// 清除接收缓冲区数据
        /// </summary>
        /// <returns></returns>
        public bool ClearBuffer()
        {
            if (Device.VCI_ClearBuffer(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID) == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关闭CAN
        /// </summary>
        /// <returns></returns>
        public bool CloseCAN()
        {
            if (Device.VCI_CloseDevice(_objCANDevice.DevType, _objCANDevice.DevID) == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <returns></returns>
        public List<VCI_CAN_OBJ> Receive(uint con_maxlen, int waitMs)
        {
            List<VCI_CAN_OBJ> list = new();
            //uint con_maxlen = 50;
            IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * (int)con_maxlen);

            uint res = Device.VCI_Receive(_objCANDevice.DevType, _objCANDevice.DevID, _objCANDevice.CANID, pt, con_maxlen, waitMs);

            for (uint i = 0; i < res; i++)
            {
                VCI_CAN_OBJ obj = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((uint)pt + (i * Marshal.SizeOf(typeof(VCI_CAN_OBJ)))), typeof(VCI_CAN_OBJ))!;

                list.Add(obj);
            }

            Marshal.FreeHGlobal(pt);
            return list;
        }

    }
}
