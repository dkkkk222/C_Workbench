using PPEC.Communication.Common;
using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class ConcurrentComMaster : ICommunicationMaster
    {
        public byte DefaultSlaveId { get; set; } = 1;
        private IFactory _factory { get; set; }
        private ITransport _transport { get; set; }
        private IMaster _master;
        public IMaster Master => _master;
        public AgreementType AgreementType { get; set; }
        public SerialPortConfig SoftConfigInfo { get; set; }
        public CANCommConfig CANCommConfig { get; set; }
        public CancellationTokenSource cts;


        private readonly TimeSpan _minInterval;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ConcurrentComMaster(IFactory Factory)
        {
            _factory = Factory;
            cts = new CancellationTokenSource();
            _minInterval = new TimeSpan(TimeSpan.TicksPerMillisecond * 10);
        }

        public void CreateMaster()
        {
            if (SoftConfigInfo == null)
                return;
            _factory.Retries = SoftConfigInfo.Retries == null ? 0 : SoftConfigInfo.Retries.Value;

            _transport?.StreamResource?.Dispose();
            _master?.Dispose();
            _transport = _factory.CreateTransport(SoftConfigInfo);
            _transport.ReadTimeout = SoftConfigInfo.ReadTimeOut;
            _transport.WriteTimeout = SoftConfigInfo.WriteTimeOut;
            _master = _factory.CreateMaster(_transport);
            cts = new CancellationTokenSource();
        }

        public bool initConfig(ConnectConfig connectConfig)
        {
            if (SoftConfigInfo == null)
                SoftConfigInfo = new SerialPortConfig();
            if (SoftConfigInfo.SerialProtName == connectConfig.SerialConfig.SerialProtName)
                return false;
            SoftConfigInfo = connectConfig.SerialConfig;
            if (_transport != null && _transport.StreamResource != null)
            {
                return _transport.StreamResource.ChangeSerialPortInfo(SoftConfigInfo);
            }
            return true;
        }
        public bool initConfig(string serialProtName, int baudRate = 38400, int wirteTimeOut = 500, int readTimeOut = 500)
        {
            if (SoftConfigInfo == null)
                SoftConfigInfo = new SerialPortConfig();
            if (SoftConfigInfo.SerialProtName == serialProtName)
                return false;
            SoftConfigInfo.SerialProtName = serialProtName;
            SoftConfigInfo.Parity = Parity.None;
            SoftConfigInfo.BaudRate = baudRate;
            SoftConfigInfo.DataBits = 8;
            SoftConfigInfo.StopBits = StopBits.One;
            SoftConfigInfo.BufferSize = 2048;
            SoftConfigInfo.ReadTimeOut = readTimeOut;
            SoftConfigInfo.WriteTimeOut = wirteTimeOut;
            if (_transport != null && _transport.StreamResource != null)
            {
                return _transport.StreamResource.ChangeSerialPortInfo(SoftConfigInfo);
            }
            return true;
        }

        #region SemaphoreSlim
        private Task WaitAsync(CancellationToken cancellationToken)
        {
            int difference = (int)(_minInterval - _stopwatch.Elapsed).TotalMilliseconds;
            if (difference > 0)
            {
                return Task.Delay(difference, cancellationToken);
            }
            return Task.CompletedTask;
        }
        private async Task<T> PerformReadAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            T value = default(T);
            try
            {
                await PerformAsync(async () => value = await action(), cancellationToken);
            }
            catch
            { throw; }

            return value;
        }
        private async Task PerformAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await WaitAsync(cancellationToken);
                await action();
            }
            finally
            {
                _semaphore.Release();
                _stopwatch.Restart();
            }
        }
        #endregion

        #region Connect
        public void DisConnect()
        {
            cts.Cancel();
            _master?.DisConnect();
            //_master.Dispose();
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
        public bool IsConnected()
        {
            if (_master == null)
                return false;
            return _master.IsConnected();
        }
        public void Connect()
        {
            if (cts.Token.IsCancellationRequested == true)
            {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
            _master.Connect();
        }
        #endregion

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
            return PerformReadAsync(() =>
            Task.Factory.StartNew(
                () =>
                ReadHoldingRegisters(startAddress, numberOfPoints)
            ), cancellationToken);
        }

        public void WriteSingleRegister(ushort registerAddress, ushort value)
        {
            if (_master == null)
                return;
            _master.WriteSingle(DefaultSlaveId, new AddressCollection(registerAddress), value);
        }

        public Task WriteSingleRegisterAsync(ushort registerAddress, ushort value, CancellationToken cancellationToken)
        {
            return PerformAsync(() => Task.Factory.StartNew(() => WriteSingleRegister(registerAddress, value)), cancellationToken);
        }

        public void WriteMultipleRegisters(ushort startAddress, ushort[] data)
        {
            if (_master == null)
                return;
            _master.WriteMultiple(DefaultSlaveId, new AddressCollection(startAddress), data);
        }

        public Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] data, CancellationToken cancellationToken)
        {
            return PerformAsync(() => Task.Factory.StartNew(() => WriteMultipleRegisters(startAddress, data)), cancellationToken);
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
        public bool HasReceive()
        { return default; }
        public void SendDataToCan(uint mailId, byte[] data)
        { }
    }
}
