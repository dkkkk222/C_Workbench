using PPEC.Communication.Common;
using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public interface ICommunicationMaster : IDisposable
    {
        IMaster Master { get; }
        AgreementType AgreementType { get; set; }
        SerialPortConfig SoftConfigInfo { get; set; }
        CANCommConfig CANCommConfig { get; set; }
        bool initConfig(ConnectConfig connectConfig);
        bool initConfig(string serialProtName, int baudRate = 38400, int wirteTimeOut = 500, int readTimeOut = 500);
        void CreateMaster();
        byte DefaultSlaveId { get; set; }

        void Connect();
        void DisConnect();
        bool IsConnected();
        void CancelCts();
        ushort[] ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints);

        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, CancellationToken cancellationToken = default(CancellationToken));

        void WriteSingleRegister(ushort registerAddress, ushort value);

        Task WriteSingleRegisterAsync(ushort registerAddress, ushort value, CancellationToken cancellationToken = default(CancellationToken));

        void WriteMultipleRegisters(ushort startAddress, ushort[] data);

        Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] data, CancellationToken cancellationToken = default(CancellationToken));

        IRequest Send(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false);

        Task<IRequest> SendASync(byte[] sendBytes, bool isQuickCommand = false, bool IsSendPure = false);

        bool HasReceive();
        void SendDataToCan(uint mailid, byte[] datas);
    }
}
