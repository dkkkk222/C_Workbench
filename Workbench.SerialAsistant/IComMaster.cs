using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant
{
    public interface IComMaster
    {
        bool Connect();
        void Dispose();
        void Send(byte[] bytes, string clientName = null);
    }
}
