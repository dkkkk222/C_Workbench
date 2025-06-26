using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Workbench.Utils.ControlCANHelper64;
using Workbench.Models.BootLoader;

namespace Workbench.Utils.CAN
{
    public class ControlCAN
    {
        private CANCommConfig _canConfig;
        public ControlCAN(CANCommConfig canConfig)
        {
            _canConfig = canConfig;
        }
        public uint GetCurrentCanId()
        {
            return _canConfig.CanInd;
        }
        public bool OpenCAN64()
        {
            ControlCANHelper64.VCI_BOARD_INFO[] vbi2 = new ControlCANHelper64.VCI_BOARD_INFO[50];
            uint num1 = Device64.VCI_FindUsbDevice2(ref vbi2[0]);
            if (Device64.VCI_OpenDevice(_canConfig.DeviceType, _canConfig.DeviceInd, 0) == 0)
            {
                return false;
            }
            var config = GetInitConfig(_canConfig.DeviceType);
            if (config == null) return false;
            var pInitConfig = config.Value;
            //VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
            //config.AccCode = System.Convert.ToUInt32("0x00000000" + textBox_AccCode.Text, 16);
            //config.AccMask = System.Convert.ToUInt32("0xFFFFFFFF" + textBox_AccMask.Text, 16);
            //config.Timing0 = System.Convert.ToByte("0x01" + textBox_Time0.Text, 16);
            //config.Timing1 = System.Convert.ToByte("0x1C" + textBox_Time1.Text, 16);
            //config.Filter = (Byte)(comboBox_Filter.SelectedIndex + 1);
            //config.Mode = (Byte)comboBox_Mode.SelectedIndex;
            Device64.VCI_InitCAN(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref pInitConfig);
            return true;
        }

        /// <summary>
        /// 打开CAN设备
        /// </summary>
        /// <returns></returns>
        public bool OpenCAN()
        {
            if (Device.VCI_OpenDevice(_canConfig.DeviceType, _canConfig.DeviceInd, 0) == 0)
            {
                return false;
            }
            return true;
        }

        public bool StartCan64(uint canId = 0)
        {
            if (Device64.VCI_StartCAN(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd) == 0)
            {
                return false;
            }
            ClearBuffer64();
            return true;
        }
        /// <summary>
        /// 启动某一路CAN
        /// </summary>
        public bool StartCan(uint canId = 0)
        {
            if (canId != 0)
            {
                _canConfig.CanInd = canId;
            }

            var config = GetInitConfig(_canConfig.DeviceType);
            if (config == null) return false;
            var pInitConfig = config.Value;

            if (!InitCAN(pInitConfig))
            {
                return false;
            }

            if (Device.VCI_StartCAN(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd) == 0)
            {
                //MessageBox.Show("StartCAN失败", "错误",
                //        MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                case Device64.VCI_PCI5010U:
                case Device64.VCI_PCI5020U:
                case Device64.VCI_USBCAN_E_U:
                case Device64.VCI_USBCAN_2E_U:
                    // 设置波特率
                    if (!SetBaudRate(GetBaudTimingCase2(_canConfig.CanBaudRateIndex))) return null;
                    config.Mode = 0;
                    break;
                default:
                    config.AccCode = Convert.ToUInt32("0x" + "00000000", 16);
                    config.AccMask = Convert.ToUInt32("0x" + "FFFFFFFF", 16);
                    var timings = GetBaudTiming(_canConfig.CanBaudRateIndex);
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
            List<byte> result = new List<byte>();
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

        private uint GetBaudTimingCase2(int index)
        {
            uint result = 0x160023;
            switch (index)
            {
                case 0:
                    result = 0x060003;
                    break;
                case 1:
                    result = 0x060004;
                    break;
                case 2:
                    result = 0x060007;
                    break;
                case 3:
                    result = 0x1C0008;
                    break;
                case 4:
                    result = 0x1C0011;
                    break;
                case 5:
                    result = 0x160023;
                    break;
                case 6:
                    result = 0x1C002C;
                    break;
                case 7:
                    result = 0x1600B3;
                    break;
                case 8:
                    result = 0x1C00E0;
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
                if (Device.VCI_SetReference(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, 0, ptr) == 0)
                {
                    //MessageBox.Show("SetReference设置波特率失败", "错误",
                    //            MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            //VCI_INIT_CONFIG initConfig = new VCI_INIT_CONFIG();
            //initConfig.Mode = modeId;//正常模式
            //if (Device.VCI_InitCAN(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref initConfig) == 0)
            //{
            //    return false;
            //}
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
                if (Device.VCI_SetReference(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, 4, ptr) == 0)
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

            //if (Device.VCI_InitCAN(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref obj) == 0)
            //{
            //    //MessageBox.Show("InitCAN失败", "错误",
            //    //        MessageBoxButton.OK, MessageBoxImage.Exclamation);
            //    return false;
            //}
            return true;
        }

        /// <summary>
        /// 查看缓存区是否存在未读取数据
        /// </summary>
        /// <returns></returns>
        public bool HasReceive()
        {
            bool hasReceive = true;

            var num = Device.VCI_GetReceiveNum(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd);
            if (num == 1)
            {
                //var frame = new VCI_CAN_OBJ();
                //frame.ID = 0x00;
                //frame.Data = new byte[8];
                //frame.TimeFlag = 0;
                //frame.TimeStamp = 0;
                //frame.RemoteFlag = 0;
                //frame.ExternFlag = 1;
                //frame.DataLen = 8;
                //if (!Transmit(frame)) hasReceive = false;
            }
            else
            {
                hasReceive = num != 0;
            }
            return hasReceive;
        }

        public bool Transmit(VCI_CAN_OBJ frame)
        {
            //VCI_CAN_OBJ[] frames = new VCI_CAN_OBJ[1];
            //frames[0] = new VCI_CAN_OBJ
            //{
            //    ID = frame.ID,
            //    TimeFlag = frame.TimeFlag,
            //    TimeStamp = frame.TimeStamp,
            //    SendType = frame.SendType,
            //    RemoteFlag = frame.RemoteFlag,
            //    ExternFlag = frame.ExternFlag,
            //    Data = frame.Data,
            //    DataLen = frame.DataLen,
            //    Reserved = frame.Reserved
            //};
            //return Device.VCI_Transmit(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, frames, (uint)frames.Length) != 0;
            return false;
        }
        public bool Transmit64(VCI_CAN_OBJ frame)
        {

            if (Device64.VCI_Transmit(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref frame, 1) == 0)
                return false;
            return true;
        }

        public bool Transmit64(List<VCI_CAN_OBJ> listFrame)
        {
            for (int i = 0; i < listFrame.Count(); i++)
            {
                var frame = listFrame[i];
                if (Device64.VCI_Transmit(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref frame, 1) == 0)
                { }
            }
            //if (Device64.VCI_Transmit(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref frame, 1) == 0)
            //    return false;
            return true;
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
                //obj[i] = new VCI_CAN_OBJ
                //{
                //    ID = frames[i].ID,
                //    TimeFlag = frames[i].TimeFlag,
                //    TimeStamp = frames[i].TimeStamp,
                //    SendType = frames[i].SendType,
                //    RemoteFlag = frames[i].RemoteFlag,
                //    ExternFlag = frames[i].ExternFlag,
                //    Data = frames[i].Data,
                //    DataLen = frames[i].DataLen,
                //    Reserved = frames[i].Reserved
                //};
            }
            return false;// Device.VCI_Transmit(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, obj, (uint)length) != 0;
        }

        public bool ClearBuffer64()
        {
            if (Device64.VCI_ClearBuffer(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd) == 0)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 清除接收缓冲区数据
        /// </summary>
        /// <returns></returns>
        public bool ClearBuffer()
        {
            if (Device.VCI_ClearBuffer(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd) == 0)
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
            if (Device.VCI_CloseDevice(_canConfig.DeviceType, _canConfig.DeviceInd) == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <returns></returns>
        public List<VCI_CAN_OBJ> Receive()
        {
            List<VCI_CAN_OBJ> list = new List<VCI_CAN_OBJ>();
            uint con_maxlen = 50;
            IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * (int)con_maxlen);

            uint res = Device.VCI_Receive(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, pt, con_maxlen, 100);

            for (uint i = 0; i < res; i++)
            {
                VCI_CAN_OBJ obj = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((uint)pt + (i * Marshal.SizeOf(typeof(VCI_CAN_OBJ)))), typeof(VCI_CAN_OBJ));

                list.Add(obj);
            }

            Marshal.FreeHGlobal(pt);
            return list;
        }

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];
        unsafe public List<VCI_CAN_OBJ> Receive64()
        {
            List<VCI_CAN_OBJ> list = new List<VCI_CAN_OBJ>();
            UInt32 res = new UInt32();

            res = Device64.VCI_Receive(_canConfig.DeviceType, _canConfig.DeviceInd, _canConfig.CanInd, ref m_recobj[0], 1000, 100);

            if (res == 0xFFFFFFFF) res = 0;
            String str = "";
            for (UInt32 i = 0; i < res; i++)
            {

                str += "  帧ID:0x" + System.Convert.ToString(m_recobj[i].ID, 16);

                if (m_recobj[i].RemoteFlag == 0)
                {
                    byte len = (byte)(m_recobj[i].DataLen % 9);

                    //var frame = ConfigListCAN[m_recobj[i].ID];

                    fixed (VCI_CAN_OBJ* m_recobj1 = &m_recobj[i])
                    {
                        VCI_CAN_OBJ obj = *m_recobj1;
                        list.Add(obj);
                        //byte[] byteArray = new byte[8];
                        //byteArray[0] = m_recobj1->Data[0];
                        //byteArray[1] = m_recobj1->Data[1];
                        //byteArray[2] = m_recobj1->Data[2];
                        //byteArray[3] = m_recobj1->Data[3];
                        //byteArray[4] = m_recobj1->Data[4];
                        //byteArray[5] = m_recobj1->Data[5];
                        //byteArray[6] = m_recobj1->Data[6];
                        //byteArray[7] = m_recobj1->Data[7];
                        //Marshal.Copy((IntPtr)m_recobj1->Data, byteArray, 0, 8);

                    }
                }
            }
            return list;
        }
    }
}
