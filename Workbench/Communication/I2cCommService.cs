using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Device.I2c;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication;
using static LinqToDB.Common.Configuration;

namespace Workbench.Communication
{
    // <summary>
    /// I2C 通讯服务（按协议：地址高4位 1001，低3位为 b2 b1 b0）
    /// 端口字符串格式： "I2C:<busId>:<addrBits>"
    /// 例： "I2C:1:3"  -> busId=1, b2b1b0=0b011 -> 7位地址 0x4B
    /// </summary>
    public class I2cCommService : IBaseCommService
    {
        private I2cDevice _device;

        public bool IsConnected => _device != null;

        public string Delay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>可选：与串口版保持习惯一致的缓存（最近一次读/写的寄存器值）</summary>
        private ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

        private int _busId;
        private int _addrBits;       // 0..7
        private int _dev7bitAddress; // 0x48..0x4F

        private static int Build7BitAddress(int addrBits /*0..7*/)
        {
            if (addrBits < 0 || addrBits > 7) throw new ArgumentOutOfRangeException(nameof(addrBits));
            // 0b1001 << 3 == 0b1001_000 == 0x48
            return (0x9 << 3) | (addrBits & 0x7); // 0x48..0x4F
        }

        /// <summary>
        /// 解析 "I2C:<busId>:<addrBits>"；其他串口参数忽略
        /// </summary>
        public void Connect(string portName, int baudRate = 0, System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
                            int dataBits = 8, System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One)
        {
            if (IsConnected) Close();

            if (string.IsNullOrWhiteSpace(portName) || !portName.StartsWith("I2C:", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("I2C 连接字符串格式应为 I2C:<busId>:<addrBits>，例如 I2C:1:3", nameof(portName));

            // 简单解析
            // I2C:1:3
            var parts = portName.Split(':');
            if (parts.Length != 3) throw new ArgumentException("I2C 连接字符串格式错误，应为 I2C:<busId>:<addrBits>");

            _busId = int.Parse(parts[1]);
            _addrBits = int.Parse(parts[2]);

            _dev7bitAddress = Build7BitAddress(_addrBits);

            var settings = new I2cConnectionSettings(_busId, _dev7bitAddress);
            _device = I2cDevice.Create(settings);

            // 提示：部分 USB-I2C 适配器需要其厂商驱动把设备映射为系统 I2C 总线，
            // 否则应使用其厂商 SDK。这里基于 System.Device.I2c 的统一抽象。
        }

        public void Close()
        {
            try
            {
                _device?.Dispose();
                _device = null;
                _cache.Clear();
            }
            catch
            {
                throw;
            }
        }

        public void Dispose() => Close();

        /// <summary>
        /// 裸写（直接把 data 作为写负载，API 会自动发送器件地址+写位）
        /// </summary>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected || data == null) return false;

            try
            {
                await Task.Run(() => _device.Write(data)); 
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ====== 协议级 API（推荐上层直接用这些）======

        /// <summary>
        /// 通信复位：写入 0x3C, 0xFF；芯片自复位约100ms
        /// </summary>
        public async Task<bool> ResetAsync(int resetHoldMs = 120)
        {
            var payload = new byte[] { 0x3C, 0xFF };
            var ok = await SendAsync(payload);
            if (ok && resetHoldMs > 0)
            {
                await Task.Delay(resetHoldMs);
            }
            return ok;
        }

        /// <summary>
        /// 写寄存器（2字节地址 + 4字节数据，均高字节在前）
        /// </summary>
        public async Task<bool> WriteRegisterAsync(ushort regAddr, uint value)
        {
            if (!IsConnected) return false;

            var buf = new byte[6];
            // 地址（B1 高位，B0 低位）
            buf[0] = (byte)((regAddr >> 8) & 0xFF);
            buf[1] = (byte)(regAddr & 0xFF);
            // 数据（B3..B0，高字节在前）
            buf[2] = (byte)((value >> 24) & 0xFF);
            buf[3] = (byte)((value >> 16) & 0xFF);
            buf[4] = (byte)((value >> 8) & 0xFF);
            buf[5] = (byte)(value & 0xFF);

            var ok = await SendAsync(buf);
            if (ok)
            {
                var key = regAddr.ToString("X4");
                _cache.AddOrUpdate(key, value, (_, __) => value);
            }
            return ok;
        }       

        /// <summary>
        /// 读寄存器（先写2字节地址，再读4字节数据），自动生成重复起始
        /// </summary>
        public async Task<uint?> ReadRegisterAsync(ushort regAddr, bool param1 = false, byte param2 = 0xA0, int timeoutMs = 20)
        {
            if (!IsConnected) return null;

            var addr = new byte[2];
            addr[0] = (byte)((regAddr >> 8) & 0xFF);
            addr[1] = (byte)(regAddr & 0xFF);

            var data = new byte[4];

            try
            {
                await Task.Run(() => _device.WriteRead(addr, data)); // 复合帧：S+Addr(W)+AddrB1+AddrB0+Sr+Addr(R)+Data*4+NACK+P
            }
            catch
            {
                return null;
            }

            uint value = ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3];
          
            //var key = regAddr.ToString("X4");

            #region 解析
            var receiveData = Parse(data);
            _cache.AddOrUpdate(receiveData.Item1, receiveData.Item2, (key, oldValue) => receiveData.Item2);
            #endregion
            //_cache.AddOrUpdate(key, value, (_, __) => value);
            return value;
        }
        public  (string,object) Parse(byte[] data)
        {
            string hex = Utility.ToHexString(data);

            byte[] addressBytes = new byte[2];
            Array.Copy(data, 16, addressBytes, 0, 2);
            string addressHex = Utility.ToHexString(addressBytes);

            byte[] dataBytes = new byte[4];
            Array.Copy(data, 18, dataBytes, 0, 4);
            string dataStr = Utility.ToHexString(dataBytes);
            var decValue = Utility.ParseHexToUInt(dataStr);

            return (addressHex, decValue);
        }

        // ====== 为兼容你现有的 IBaseCommService.Read(hexAddress) ======

        /// <summary>
        /// 按你的接口：传入 16进制地址（如 "0170"），返回 32位无符号数据
        /// </summary>
        public uint? Read(string hexAddress)
        {
            if (string.IsNullOrWhiteSpace(hexAddress)) return null;

            if (!ushort.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                return null;

            var t = ReadRegisterAsync(reg);
            t.Wait(); // 同步桥接
            return t.Result;
        }

        // 可选：为了与串口端统一，你也可以暴露一个 TryGetCache
        public bool TryGetCached(string hexAddress, out object value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(hexAddress)) return false;
            if (_cache.TryGetValue(hexAddress.ToUpperInvariant(), out var v))
            {
                value = v;
                return true;
            }
            return false;
        }

        public Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 160, int delayMs = 5)
        {
            throw new NotImplementedException();
        }
    }
}
