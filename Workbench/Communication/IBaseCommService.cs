using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Model;

namespace Workbench.Communication
{
    public interface IBaseCommService : IDisposable
    {
        string Delay{get; set;}
        bool IsConnected { get; }
        void Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<bool> SendAsync(byte[] data);
        uint? Read(string hexAddress);
        Task<uint?> ReadRegisterAsync(ushort regAddr, bool param1 = false, byte param2 = 0xA0, int timeoutMs = 20);
        /// <summary>
        /// CAN发送
        /// </summary>
        Task WriteRegisterAsync(ushort regAddr, byte[] value4, bool useCanB = false, byte dest = 0xA0, int delayMs = 5);
        /// <summary>
        /// I2C发送
        /// </summary>
        Task<bool> WriteRegisterAsync(ushort regAddr, uint value4);
        void Close();

        Task<ControlAck> SendRemoteControlAsync(byte[] payload, int timeoutMs = 50);
        Task<ControlAck> SendInjectionAsync(byte[] payload, int timeoutMs = 80);
        /// <summary>
        /// 遥测查询
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <param name="projectTag"></param>
        /// <returns></returns>
        Task<byte[]> QueryTelemetryOnceAsync(int timeoutMs = 200, byte projectTag = 0xFF);
    }
}
