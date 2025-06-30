using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPEC.Communication.Enum;

namespace PPEC.Communication.Interface
{
    public interface ICommChannel : IDisposable
    {
        string Name { get; }

        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken token = default);
        Task DisconnectAsync();

        Task SendAsync(byte[] buffer, int offset = 0, int count = -1,
                       CancellationToken token = default);

        /// <summary>收到原始字节（在 ThreadPool 线程）</summary>
        event EventHandler<ReadOnlyMemory<byte>> BytesReceived;

        /// <summary>底层异常 / 掉线</summary>
        event EventHandler<Exception> ChannelFaulted;
        public event EventHandler<IUartMessage> MessageParsed;
    }
    public interface IUartMessage
    {
        UartDataType Type { get; }
    }
    public interface IProtocolHandler
    {
        void Feed(ReadOnlySpan<byte> raw);
        event EventHandler<IUartMessage> MessageParsed;
    }
}
