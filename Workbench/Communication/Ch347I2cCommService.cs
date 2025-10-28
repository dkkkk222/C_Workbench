using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.Generic;
using PPEC.Communication; // 假设你已有该接口
using System.Linq;
using System.Text;
using System.Threading;
using Workbench.Models;
using PPEC.Communication.Model;

namespace Workbench.Communication
{
    /// <summary>
    /// CH347 官方头文件 CH347DLL.h 的 C# 对应 P/Invoke 映射
    /// - DLL 名：x86=CH347DLL.DLL；x64=CH347DLLA64.DLL
    /// - 结构体：mDeviceInforS => DeviceInfo（Pack=1，Ansi）
    /// - BOOL 映射：添加 MarshalAs(UnmanagedType.Bool)
    /// </summary>
    public static class Ch347Native
    {
        private const string Dll = "CH347DLLA64.DLL";
//#if X64
//        private const string Dll = "CH347DLLA64.DLL";
//#else
//        private const string Dll = "CH347DLL.DLL";
//#endif
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // ===== 通用（打开/关闭/信息/版本/超时） =====
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CH347OpenDevice(uint DevI); // 返回句柄

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347CloseDevice(uint iIndex);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347GetDeviceInfor(uint iIndex, out DeviceInfo DevInformation);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347GetVersion(uint iIndex,
                                                    out byte iDriverVer,
                                                    out byte iDLLVer,
                                                    out byte ibcdDevice,
                                                    out byte iChipType);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347SetTimeout(uint iIndex, uint iWriteTimeout, uint iReadTimeout);

        // ===== I2C =====
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347I2C_Set(uint iIndex, uint iMode);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347I2C_SetStretch(uint iIndex, [MarshalAs(UnmanagedType.Bool)] bool iEnable);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347I2C_SetDelaymS(uint iIndex, uint iDelay);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347I2C_SetDriverMode(uint iIndex, byte iMode); // 0=开漏,1=推挽

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347StreamI2C(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                   uint iReadLength, [Out] byte[] oReadBuffer);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347StreamI2C_RetACK(uint iIndex, uint iWriteLength, byte[] iWriteBuffer,
                                                          uint iReadLength, [Out] byte[] oReadBuffer,
                                                          out uint rAckCount);

        // 仅 CH347T 可用：设置第8位时钟低周期延时（微秒）
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CH347I2C_SetAckClk_DelayuS(uint iIndex, uint iDelay);

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

    /// <summary>
    /// 设备信息封装，便于 UI 展示
    /// </summary>
    public sealed class Ch347Device
    {
        public uint Index { get; set; }
        public Ch347Native.DeviceInfo Info { get; set; }
        public byte DriverVer { get; set; }
        public byte DllVer { get; set; }
        public byte BcdDevice { get; set; }
        public byte ChipType { get; set; }
        public override string ToString() => $"{Index}# {Info.FuncDescStr} | {Info.ProductString} | Mode={Info.ChipMode}";
    }

    /// <summary>
    /// 设备枚举：等价于 C Demo 的 EnumDevice()
    /// </summary>
    public static class Ch347DeviceEnumerator
    {
        public static List<Ch347Device> Enumerate(int maxIndex = 32, bool excludeMode3 = false)
        {            
            var list = new List<Ch347Device>();          
            for (uint i = 0; i < maxIndex; i++)
            {
                var h = Ch347Native.CH347OpenDevice(i);
                if (h != IntPtr.Zero && h != Ch347Native.INVALID_HANDLE_VALUE)
                {
                    if (Ch347Native.CH347GetDeviceInfor(i, out var info))
                    {
                        if (!(excludeMode3 && info.ChipMode == 3))
                        {
                            Ch347Native.CH347GetVersion(i, out var drv, out var dll, out var bcd, out var chip);
                            list.Add(new Ch347Device
                            {
                                Index = i,
                                Info = info,
                                DriverVer = drv,
                                DllVer = dll,
                                BcdDevice = bcd,
                                ChipType = chip
                            });
                        }
                    }
                    Ch347Native.CH347CloseDevice(i);
                }
            }
            return list;
        }
    }

    /// <summary>
    /// 基于 CH347 的 I2C 服务（严格按官方 DLL 签名；协议按你提供的电源管理芯片文档）
    /// 连接串："CH347:<index>:<addrBits>[:<speedCode>[:<stretch>[:<delayMs>[:<driverMode>[:<wTimeoutMs>[:<rTimeoutMs>[:<ackDelayUs>]]]]]]]"
    ///   index:       设备序号（0..）
    ///   addrBits:    b2b1b0（0..7），最终 7 位地址 = 0x48..0x4F
    ///   speedCode:   I2C 速率编码（默认 1=100kHz；000=20k,001=100k,010=400k,011=750k,100=50k,101=200k,110=1MHz）
    ///   stretch:     0/1 关闭/开启 SCL 时钟延展（默认 0）
    ///   delayMs:     下一次流操作前延时（默认 0）
    ///   driverMode:  0=开漏（默认），1=推挽
    ///   wTimeoutMs:  USB 写超时（默认 500）
    ///   rTimeoutMs:  USB 读超时（默认 500）
    ///   ackDelayUs:  仅 CH347T：第8位时钟低周期延时（微秒，默认 0 不设置）
    /// </summary>
    public class Ch347I2cCommService : IBaseCommService, IDisposable
    {
        private bool _opened;
        private uint _index;
        private int _addrBits; // 0..7
        private int _dev7;     // 0x48..0x4F
        private byte _addrW;   // (addr7<<1)|0
        private byte _addrR;   // (addr7<<1)|1

        // init options
        private uint _speedCode = 1;      // 100kHz
        private bool _stretch = false;    // SCL 拉伸
        private uint _delayMs = 0;        // 下一次流操作前延时
        private byte _driverMode = 0;     // 0=开漏
        private uint _wTimeoutMs = 500;
        private uint _rTimeoutMs = 500;
        private uint _ackDelayUs = 0;     // 仅 CH347T

        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

        public bool IsConnected => _opened;

        public string Delay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private static int Build7BitAddress(int addrBits)
        {
            if (addrBits < 0 || addrBits > 7) throw new ArgumentOutOfRangeException(nameof(addrBits));
            return (0x9 << 3) | (addrBits & 0x7); // 0x48..0x4F
        }

        /// <summary>
        /// 连接并完成 I2C 初始化
        /// </summary>
        public void Connect(string portName, int baudRate = 0, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            if (IsConnected) Close();
            if (string.IsNullOrWhiteSpace(portName) || !portName.StartsWith("CH347:", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("连接串应为 CH347:<index>:<addrBits>[:<speedCode>[:<stretch>[:<delayMs>[:<driverMode>[:<wTimeoutMs>[:<rTimeoutMs>[:<ackDelayUs>]]]]]]]", nameof(portName));

            var p = portName.Split(':');
            if (p.Length < 3) throw new ArgumentException("连接串至少需要 CH347:<index>:<addrBits>");

            _index = uint.Parse(p[1]);
            _addrBits = int.Parse(p[2]);
            _speedCode = (p.Length >= 4) ? uint.Parse(p[3]) : 1u;
            _stretch = (p.Length >= 5) ? (p[4] == "1") : false;
            _delayMs = (p.Length >= 6) ? uint.Parse(p[5]) : 0u;
            _driverMode = (p.Length >= 7) ? byte.Parse(p[6]) : (byte)0;
            _wTimeoutMs = (p.Length >= 8) ? uint.Parse(p[7]) : 500u;
            _rTimeoutMs = (p.Length >= 9) ? uint.Parse(p[8]) : 500u;
            _ackDelayUs = (p.Length >= 10) ? uint.Parse(p[9]) : 0u;

            _dev7 = Build7BitAddress(_addrBits);
            _addrW = (byte)((_dev7 << 1) | 0x00);
            _addrR = (byte)((_dev7 << 1) | 0x01);
            //可以读取的设备地址 A0 和A1。
            //_addrR = 160;
            //_addrW = 160;
            // 打开设备（返回句柄）
            var h = Ch347Native.CH347OpenDevice(_index);
            if (h == IntPtr.Zero || h == Ch347Native.INVALID_HANDLE_VALUE)
                throw new InvalidOperationException($"CH347OpenDevice({_index}) 失败，请检查连接/驱动。");
            _opened = true;

            // 设置 USB 读写超时
            if (!Ch347Native.CH347SetTimeout(_index, _wTimeoutMs, _rTimeoutMs))
                throw new InvalidOperationException("CH347SetTimeout 失败");

            // I2C 基础设置：速率/拉伸/驱动模式/延时
            if (!Ch347Native.CH347I2C_Set(_index, _speedCode))
                throw new InvalidOperationException($"CH347I2C_Set(mode={_speedCode}) 失败");
            if (!Ch347Native.CH347I2C_SetStretch(_index, _stretch))
                throw new InvalidOperationException($"CH347I2C_SetStretch({_stretch}) 失败");
            //if (!Ch347Native.CH347I2C_SetDriverMode(_index, _driverMode))
            //    throw new InvalidOperationException($"CH347I2C_SetDriverMode({_driverMode}) 失败");
            if (_delayMs > 0 && !Ch347Native.CH347I2C_SetDelaymS(_index, _delayMs))
                throw new InvalidOperationException($"CH347I2C_SetDelaymS({_delayMs}) 失败");

            // 仅 CH347T：可选设置第8位时钟低周期延时
            if (_ackDelayUs > 0)
                Ch347Native.CH347I2C_SetAckClk_DelayuS(_index, _ackDelayUs); // 不强制要求成功
        }

        public void Close()
        {
            try
            {
                if (_opened)
                {
                    Ch347Native.CH347CloseDevice(_index);
                    _opened = false;
                    _cache.Clear();
                }
            }
            catch 
            { 
                throw;
            }
        }

        public void Dispose() => Close();

        /// <summary>
        /// 裸写：data 必须以 (addrW) 作为首字节
        /// </summary>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected || data == null || data.Length == 0) return false;
            return await Task.Run(() => Ch347Native.CH347StreamI2C(_index, (uint)data.Length, data, 0, null));
        }

        // ================= 协议级 API =================

        /// <summary>
        /// 通信复位（写 0x3C, 0xFF；约 100ms 自复位），帧：S (addrW) 3C FF P
        /// </summary>
        public async Task<bool> ResetAsync(int resetHoldMs = 120)
        {
            var buf = new byte[] { _addrW, 0x3C, 0xFF };
            var ok = await Task.Run(() => Ch347Native.CH347StreamI2C(_index, (uint)buf.Length, buf, 0, null));
            if (ok && resetHoldMs > 0) await Task.Delay(resetHoldMs);
            return ok;
        }

        /// <summary>
        /// 写寄存器（2B 地址 + 4B 数据，均高字节在前）
        /// 帧：S (addrW) REG_H REG_L D3 D2 D1 D0 P
        /// </summary>
        public async Task<bool> WriteRegisterAsync(ushort regAddr, uint value)
        {
            if (!IsConnected) return false;
            var buf = new byte[1 + 2 + 4];
            buf[0] = _addrW;
            buf[1] = (byte)(regAddr >> 8);
            buf[2] = (byte)(regAddr & 0xFF);
            buf[3] = (byte)(value >> 24);
            buf[4] = (byte)((value >> 16) & 0xFF);
            buf[5] = (byte)((value >> 8) & 0xFF);
            buf[6] = (byte)(value & 0xFF);

            var ok = await Task.Run(() => Ch347Native.CH347StreamI2C(_index, (uint)buf.Length, buf, 0, null));
            if (ok)
            {
                var key = regAddr.ToString("X4");
                _cache.AddOrUpdate(key, value, (_, __) => value);
            }
            return ok;
        }

        /// <summary>
        /// 读寄存器（复合帧）：S (addrW) REG_H REG_L ACK Sr (addrR) D3 D2 D1 D0 NACK P
        /// </summary>
        public async Task<uint?> ReadRegisterAsync(ushort regAddr, bool _unused = false, byte _unused2 = 0xA0, int timeoutMs = 20)
        {
            if (!IsConnected) return null;
            var write = new byte[] { _addrW, (byte)(regAddr >> 8), (byte)(regAddr & 0xFF) };
            var read = new byte[4];
            bool ok = await Task.Run(() => Ch347Native.CH347StreamI2C(_index, (uint)write.Length, write, (uint)read.Length, read));
            if (!ok) return null;
            uint value = ((uint)read[0] << 24) | ((uint)read[1] << 16) | ((uint)read[2] << 8) | read[3];
            var key = regAddr.ToString("X4");
            _cache.AddOrUpdate(key, value, (_, __) => value);
            return value;
        }

        /// <summary>
        /// 低层排障：读写并返回 ACK 计数（NACK 定位）
        /// </summary>
        public bool WriteReadWithAck(byte[] write, int readLen, out byte[] read, out uint ackCount)
        {
            read = new byte[Math.Max(0, readLen)];
            if (write == null) write = Array.Empty<byte>();
            return Ch347Native.CH347StreamI2C_RetACK(_index, (uint)write.Length, write, (uint)read.Length, read, out ackCount);
        }

        // ============== 兼容你项目里的 IBaseCommService ==============

        public uint? Read1(string hexAddress)
        {
            if (string.IsNullOrWhiteSpace(hexAddress)) return null;
            if (!ushort.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                return null;
            var t = ReadRegisterAsync(reg);
            t.Wait();
            return t.Result;
        }

        public uint? Read(string hexAddress)
        {
            if (string.IsNullOrWhiteSpace(hexAddress)) return null;
            if (!ushort.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                return null;
            var res = TryGetCached(hexAddress, out object value);
            if (!res) return null;
            return (uint)value;
        }

        public bool TryGetCached(string hexAddress, out object value)
        {
            value = 0u;
            if (string.IsNullOrWhiteSpace(hexAddress)) return false;
            return _cache.TryGetValue(hexAddress.ToUpperInvariant(), out value);
        }

        public Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 160, int delayMs = 5)
        {
            if (value4 == null || value4.Length != 4)
                throw new ArgumentException("value4 必须是 4 字节");
            uint v = ((uint)value4[0] << 24) | ((uint)value4[1] << 16) | ((uint)value4[2] << 8) | value4[3];
            return WriteRegisterAsync(regAddr, v);
        }

        public Task<ControlAck> SendRemoteControlAsync(uint cmd, int timeoutMs = 50)
        {
            throw new NotImplementedException();
        }

        public Task<ControlAck> SendInjectionAsync(byte[] payload, int timeoutMs = 80)
        {
            throw new NotImplementedException();
        }
    }
}

/* ========================== 使用示例 ===========================
// 1) 枚举设备
var devs = Workbench.Communication.Ch347DeviceEnumerator.Enumerate(excludeMode3:false);
foreach (var d in devs) Console.WriteLine(d);

// 2) 连接并初始化 I2C
var svc = new Workbench.Communication.Ch347I2cCommService();
svc.Connect("CH347:0:3:1:0:0:0:500:500"); // index=0, addrBits=3(0x4B), 100kHz, 关拉伸, 无延时, 开漏, 超时各500ms

// 3) 通信复位
await svc.ResetAsync();

// 4) 写寄存器
await svc.WriteRegisterAsync(0x0170, 0x12345678);

// 5) 读寄存器
var val = await svc.ReadRegisterAsync(0x0170);
Console.WriteLine($"0x0170 = 0x{val:X8}");

// 6) 关闭
svc.Close();
================================================================ */

