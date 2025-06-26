using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    public interface ITransportSerial : ITransport
    {
        void DiscardInBuffer();

        bool CheckFrame { get; set; }

        bool ChecksumsMatch(IMessage message, byte[] messageFrame);

        void IgnoreResponse();
    }
}
