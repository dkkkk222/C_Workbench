using PPEC.Communication.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public interface IMaster : IDisposable
    {
        /// <summary>
        ///     Transport used by this master.
        /// </summary>
        ITransport Transport { get; }
        void WriteSingle(byte slaveAddress, AddressCollection startAddress, ushort data);
        void WriteMultiple(byte slaveAddress, AddressCollection startAddress, ushort[] data);
        Task WriteMultipleAsync(byte slaveAddress, AddressCollection startAddress, ushort[] data, CancellationToken cancellationToken);
        ushort[] ReadMultiple(byte slaveAddress, AddressCollection startAddress, ushort numberOfPoints);
        Task<ushort[]> ReadMultipleAsync(byte slaveAddress, AddressCollection startAddress, ushort numberOfPoints, CancellationToken cancellationToken);
        IRequest Send(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false);
        Task<IRequest> SendAsync(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false);
        void Connect();
        void DisConnect();
        bool IsConnected();
        event Action<object> ReceiveDataChanged;
        event Action<object, IMessage> SendDataChanged;
    }
}
