using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class TcpClientDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public int Length { get; }
        public TcpClientDataReceivedEventArgs(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }
    }
}
