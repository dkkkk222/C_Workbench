using PPEC.Communication.Enum;
using PPEC.Communication.Parameter;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using PPEC.Communication.Interface;
using System.Threading.Tasks;
using System.Threading;

namespace PPEC.Communication
{
    public class TopologyMaster : ParameterWithTransMaster, ITopologyMaster
    {
        private readonly ITopologyConfigLookup _configLookup;
        private ICommunicationMaster _comMaster;
        public ICommunicationMaster ComMaster
        {
            get { return _comMaster; }
            set { _comMaster = value; }
        }
        private TopologyId _id;
        public TopologyId Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private bool _changed = false;
        public bool Changed { get => _changed; set => _changed = value; }
        private CancellationTokenSource cts;

        public TopologyMaster(ITopologyConfigLookup configLookup, IDefaultDataSource registers, ITransformLookup transLookup) : base(registers, transLookup)
        {
            _configLookup = configLookup;
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 根据AddressName设置参数,异步
        /// </summary>
        /// <param name="isSyncSlave">isSyncSlave默认为True,isSyncSlave为False时读取Buffer</param>
        public void SetValue<T>(T value, AddressName name, bool isSyncSlave = true)
        {
            var info = _configLookup.GetConfig(_id).GetParamInfo(name);
            if (info == null)
                return;
            base.SetValue(value, info);
            if (isSyncSlave)
                SyncToSlave(name, info);//同步

            Changed = true;
        }

        public void SyncToSlave(string name)
        { }

        public void SyncToSlave(AddressName name, IParamInfo info = null)
        {
            try
            {
                if (info == null)
                    info = _configLookup.GetConfig(_id).GetParamInfo(name);
                if (info == null)
                    return;
                if (!_comMaster.IsConnected())
                {
                    //PPEC.Logging.Log.Info("PPEC未连接！");
                    return;
                }
                var val = Registers.ReadPoints(info.StartAddress, info.NumOfRegisters);
                _comMaster.WriteMultipleRegisters(info.StartAddress, val);
            }
            catch (Exception ex)
            {

            }

        }

        public T GetValue<T>(AddressName name, bool isSyncSlave = true)
        {
            var info = _configLookup.GetConfig(_id).GetParamInfo(name);
            if (info == null)
            {
                return default;
            }
            if (isSyncSlave)
            {
                SyncFromSlave(info.StartAddress, info.NumOfRegisters);
            }
            return base.GetValue<T>(info);
        }

        private void SyncFromSlave(ushort startAddress, ushort numOfRegisters)
        {
            if (!_comMaster.IsConnected())
            {
                //Log.Info("PPEC未连接！");
                return;
            }
            try
            {
                var slaveRet = _comMaster.ReadHoldingRegisters(startAddress, numOfRegisters);
                Registers.WritePoints(startAddress, slaveRet);
            }
            catch (Exception ex)
            { }
        }

        public void Start()
        {
            _comMaster.Connect();
        }

        public void Stop()
        {
            _comMaster.DisConnect();
        }

        public async Task<T> GetValueAsync<T>(AddressName name) where T : struct
        {
            var info = _configLookup.GetConfig(_id).GetParamInfo(name);
            if (info == null)
            {
                return default;
            }
            if (!_comMaster.IsConnected())
            {
                //Log.Info("PPEC未连接");
                return default;
            }
            try
            {
                var ret = await _comMaster.ReadHoldingRegistersAsync(info.StartAddress, info.NumOfRegisters, cts.Token);
                if (ret != null)
                    Registers.WritePoints(info.StartAddress, ret);
                return base.GetValue<T>(info);
            }
            catch (Exception ex)
            {
                return base.GetValue<T>(info);
            }

        }

        public async Task SetValueAsync<T>(T value, AddressName name) where T : struct
        {
            if (!_comMaster.IsConnected())
            {
                //Log.Info("PPEC未连接");
                return;
            }
            var info = _configLookup.GetConfig(_id).GetParamInfo(name);
            if (info == null)
                return;
            try
            {
                base.SetValue(value, info);
                var val = Registers.ReadPoints(info.StartAddress, info.NumOfRegisters);
                await _comMaster.WriteMultipleRegistersAsync(info.StartAddress, val, cts.Token);
            }
            catch (Exception ex)
            {
                //Log.Error(ex.Message); 
            }
        }

        public void SlaveToBufferBatch(AddressName addressName, ushort numOfRegisters = 125)
        {
            ushort startAddress = 0;
            if (!_comMaster.IsConnected())
            {
                return;
            }
            var startAddressName = _configLookup.GetConfig(_id).GetParamInfo(addressName);
            if (startAddressName == null)
                return;
            startAddress = startAddressName.StartAddress;
            var slaveRet = _comMaster.ReadHoldingRegisters(startAddress, numOfRegisters);
            Registers.WritePoints(startAddress, slaveRet);
        }
    }
}
