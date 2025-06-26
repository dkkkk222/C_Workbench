using PPEC.Communication.CAN;
using PPEC.Communication.Common;
using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;
using static PPEC.Communication.CAN.ControlCANHelper64;

namespace PPEC.Communication
{
    public class CommunicationMasterCAN : ICommunicationMaster
    {
        public byte DefaultSlaveId { get; set; } = 1;
        private IMaster _master;
        public IMaster Master => _master;
        public AgreementType AgreementType { get; set; }
        public SerialPortConfig SoftConfigInfo { get; set; }
        public CANCommConfig CANCommConfig { get; set; }
        public CancellationTokenSource cts;
        private ControlCAN _controlCAN = null;
        public CommunicationMasterCAN()
        {
            cts = new CancellationTokenSource();
        }

        #region CAN
        public bool initConfig(ConnectConfig connectConfig)
        {
            if (CANCommConfig == null)
                CANCommConfig = new CANCommConfig();
            if (connectConfig.CANCommConfig != null)
                CANCommConfig = connectConfig.CANCommConfig;
            return true;
        }
        public void CreateMaster()
        {
            if (CANCommConfig == null)
                return;

            _master?.Dispose();
            _controlCAN = new ControlCAN(CANCommConfig);
        }

        public bool HasReceive()
        {
            if (IsConnect)
            {
                var has = _controlCAN.HasReceive();
                return has;
            }
            else
            {
                return false;
            }
        }

        private bool _isConnect;

        public bool IsConnect
        {
            get
            {
                return _isConnect;
            }
            set
            {
                _isConnect = value;
            }
        }

        public bool IsConnected()
        {
            return IsConnect;
        }

        public void Close()
        {
            try
            {
                _controlCAN?.CloseCAN();
                IsConnect = false;
            }
            catch
            {
                IsConnect = false;
            }
        }
        public void Open()
        {
            try
            {
                if (!_controlCAN.OpenCAN64())
                //if (!_controlCAN.OpenCAN())
                {
                    return;
                }
                if (!_controlCAN.StartCan64())
                //if (!_controlCAN.StartCan(CANCommConfig.CanInd))
                {
                    return;
                }

                IsConnect = true;
            }
            catch
            {
                IsConnect = false;
            }
        }
        #endregion

        #region OLD
        public bool initConfig(string serialProtName, int baudRate = 38400, int wirteTimeOut = 500, int readTimeOut = 500)
        {
            return true;
        }
        public void DisConnect()
        {
            Close();
        }
        public void CancelCts()
        {
            cts.Cancel();
            if (cts.Token.IsCancellationRequested == true)
            {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
        }
        public void Connect()
        {
            Open();

        }

        #region Read/wirte
        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints)
        {
            try
            {
                if (_master == null)
                    return Array.Empty<ushort>();
                return _master.ReadMultiple(DefaultSlaveId, new AddressCollection(startAddress), numberOfPoints);
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, CancellationToken cancellationToken)
        {
            return default;
        }

        public void WriteSingleRegister(ushort registerAddress, ushort value)
        {
            if (_master == null)
                return;
            _master.WriteSingle(DefaultSlaveId, new AddressCollection(registerAddress), value);
        }

        public Task WriteSingleRegisterAsync(ushort registerAddress, ushort value, CancellationToken cancellationToken)
        {
            return default;
        }

        public void WriteMultipleRegisters(ushort startAddress, ushort[] data)
        {
            if (_master == null)
                return;
            _master.WriteMultiple(DefaultSlaveId, new AddressCollection(startAddress), data);
        }

        public Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] data, CancellationToken cancellationToken)
        {
            return default;
        }
        public IRequest Send(byte[] mess, bool isQuick = false, bool IsSendPure = false)
        {
            return _master.Send(mess, isQuick, IsSendPure);
        }

        public async Task<IRequest> SendASync(byte[] mess, bool isQuick = false, bool IsSendPure = false)
        {
            return await _master.SendAsync(mess, isQuick, IsSendPure);
        }
        #endregion
        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _master?.Dispose();
            }
        }
        #endregion
        unsafe public void SendDataToCan(uint mailId, byte[] datas)
        {
            VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
            frame.ID = mailId;
            frame.Data[0] = datas[0];
            frame.Data[1] = datas[1];
            frame.Data[2] = datas[2];
            frame.Data[3] = datas[3];
            frame.Data[4] = datas[4];
            frame.Data[5] = datas[5];
            frame.Data[6] = datas[6];
            frame.Data[7] = datas[7];
            frame.TimeFlag = 0;
            frame.TimeStamp = 0;
            frame.RemoteFlag = 0;
            frame.ExternFlag = 1;
            frame.DataLen = 8;

            _controlCAN.Transmit64(frame);
        }
    }
}
