using System;
using System.Runtime.InteropServices;

namespace Workbench.Models
{
    /// <summary>
    /// CH347 官方头文件 CH347DLL.h 的 C# 对应 P/Invoke 映射
    /// 支持 x86/x64 运行时分派
    /// </summary>
    public static class Ch347Native
    {
        private const string DllX86 = "CH347DLL.DLL";
        private const string DllX64 = "CH347DLLA64.DLL";

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private static bool Is64 => IntPtr.Size == 8;

        // ===== 将所有 extern 声明放到两个私有类中 =====
        private static class NativeX86
        {
            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr CH347OpenDevice(uint DevI);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347CloseDevice(uint iIndex);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347GetDeviceInfor(uint iIndex, out DeviceInfo DevInformation);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347GetVersion(uint iIndex,
                                                        out byte iDriverVer,
                                                        out byte iDLLVer,
                                                        out byte ibcdDevice,
                                                        out byte iChipType);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347SetTimeout(uint iIndex, uint iWriteTimeout, uint iReadTimeout);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_Set(uint iIndex, uint iMode);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetStretch(uint iIndex, [MarshalAs(UnmanagedType.Bool)] bool iEnable);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetDelaymS(uint iIndex, uint iDelay);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetDriverMode(uint iIndex, byte iMode);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347StreamI2C(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                       uint iReadLength, [Out] byte[] oReadBuffer);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347StreamI2C_RetACK(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                              uint iReadLength, [Out] byte[] oReadBuffer,
                                                              out uint rAckCount);

            [DllImport(DllX86, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetAckClk_DelayuS(uint iIndex, uint iDelay);
        }

        private static class NativeX64
        {
            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr CH347OpenDevice(uint DevI);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347CloseDevice(uint iIndex);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347GetDeviceInfor(uint iIndex, out DeviceInfo DevInformation);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347GetVersion(uint iIndex,
                                                        out byte iDriverVer,
                                                        out byte iDLLVer,
                                                        out byte ibcdDevice,
                                                        out byte iChipType);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347SetTimeout(uint iIndex, uint iWriteTimeout, uint iReadTimeout);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_Set(uint iIndex, uint iMode);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetStretch(uint iIndex, [MarshalAs(UnmanagedType.Bool)] bool iEnable);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetDelaymS(uint iIndex, uint iDelay);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetDriverMode(uint iIndex, byte iMode);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347StreamI2C(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                       uint iReadLength, [Out] byte[] oReadBuffer);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347StreamI2C_RetACK(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                              uint iReadLength, [Out] byte[] oReadBuffer,
                                                              out uint rAckCount);

            [DllImport(DllX64, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CH347I2C_SetAckClk_DelayuS(uint iIndex, uint iDelay);
        }

        // ===== 外部托管包装：根据进程位数分派 =====
        public static IntPtr CH347OpenDevice(uint DevI) => Is64 ? NativeX64.CH347OpenDevice(DevI) : NativeX86.CH347OpenDevice(DevI);

        public static bool CH347CloseDevice(uint iIndex) => Is64 ? NativeX64.CH347CloseDevice(iIndex) : NativeX86.CH347CloseDevice(iIndex);

        public static bool CH347GetDeviceInfor(uint iIndex, out DeviceInfo DevInformation)
            => Is64 ? NativeX64.CH347GetDeviceInfor(iIndex, out DevInformation) : NativeX86.CH347GetDeviceInfor(iIndex, out DevInformation);

        public static bool CH347GetVersion(uint iIndex,
                                            out byte iDriverVer,
                                            out byte iDLLVer,
                                            out byte ibcdDevice,
                                            out byte iChipType)
            => Is64 ? NativeX64.CH347GetVersion(iIndex, out iDriverVer, out iDLLVer, out ibcdDevice, out iChipType)
                    : NativeX86.CH347GetVersion(iIndex, out iDriverVer, out iDLLVer, out ibcdDevice, out iChipType);

        public static bool CH347SetTimeout(uint iIndex, uint iWriteTimeout, uint iReadTimeout)
            => Is64 ? NativeX64.CH347SetTimeout(iIndex, iWriteTimeout, iReadTimeout) : NativeX86.CH347SetTimeout(iIndex, iWriteTimeout, iReadTimeout);

        public static bool CH347I2C_Set(uint iIndex, uint iMode)
            => Is64 ? NativeX64.CH347I2C_Set(iIndex, iMode) : NativeX86.CH347I2C_Set(iIndex, iMode);

        public static bool CH347I2C_SetStretch(uint iIndex, bool iEnable)
            => Is64 ? NativeX64.CH347I2C_SetStretch(iIndex, iEnable) : NativeX86.CH347I2C_SetStretch(iIndex, iEnable);

        public static bool CH347I2C_SetDelaymS(uint iIndex, uint iDelay)
            => Is64 ? NativeX64.CH347I2C_SetDelaymS(iIndex, iDelay) : NativeX86.CH347I2C_SetDelaymS(iIndex, iDelay);

        public static bool CH347I2C_SetDriverMode(uint iIndex, byte iMode)
            => Is64 ? NativeX64.CH347I2C_SetDriverMode(iIndex, iMode) : NativeX86.CH347I2C_SetDriverMode(iIndex, iMode);

        public static bool CH347StreamI2C(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                           uint iReadLength, byte[] oReadBuffer)
            => Is64 ? NativeX64.CH347StreamI2C(iIndex, iWriteLength, iWriteBuffer, iReadLength, oReadBuffer)
                    : NativeX86.CH347StreamI2C(iIndex, iWriteLength, iWriteBuffer, iReadLength, oReadBuffer);

        public static bool CH347StreamI2C_RetACK(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                  uint iReadLength, byte[] oReadBuffer, out uint rAckCount)
            => Is64 ? NativeX64.CH347StreamI2C_RetACK(iIndex, iWriteLength, iWriteBuffer, iReadLength, oReadBuffer, out rAckCount)
                    : NativeX86.CH347StreamI2C_RetACK(iIndex, iWriteLength, iWriteBuffer, iReadLength, oReadBuffer, out rAckCount);

        public static bool CH347I2C_SetAckClk_DelayuS(uint iIndex, uint iDelay)
            => Is64 ? NativeX64.CH347I2C_SetAckClk_DelayuS(iIndex, iDelay) : NativeX86.CH347I2C_SetAckClk_DelayuS(iIndex, iDelay);

        // ===== mDeviceInforS （#pragma pack(1), Ansi） =====
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct DeviceInfo
        {
            public byte iIndex;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string DevicePath; // MAX_PATH

            public byte UsbClass;    // 0:VENDOR 2:HID 3:VCP
            public byte FuncType;    // 0:UART 1:SPI_I2C 2:JTAG_I2C 3:JTAG_IIC_SPI

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string DeviceID;   // USB\VID_xxxx&PID_xxxx

            public byte ChipMode;    // 0:Mode0 1:Mode1 2:Mode2 3:Mode3 4:CH347F
            public IntPtr DevHandle;  // HANDLE
            public ushort BulkOutEndpMaxSize;
            public ushort BulkInEndpMaxSize;
            public byte UsbSpeedType; // 0:FS 1:HS 2:SS
            public byte CH347IfNum;   // 接口号
            public byte DataUpEndp;
            public byte DataDnEndp;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ProductString;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ManufacturerString;

            public uint WriteTimeout; // ms
            public uint ReadTimeout;  // ms

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string FuncDescStr;

            public byte FirewareVer;  // 固件版本(十六进制值)
        }
    }
}
