using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Communication
{
    public interface IBaseCommService : IDisposable
    {
        bool IsConnected { get; }
        void Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<bool> SendAsync(byte[] data);
    }
}
