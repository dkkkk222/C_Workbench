using Microsoft.Win32;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Enum;
using PPEC.Communication.Parameter.Transform;
using PPEC.Communication.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PPEC.Communication.CAN.ControlCANHelper64;

namespace PPEC.Communication.CAN
{
    public class CANMaster : ParameterWithTransMasterCAN, ITopologyMaster
    {
        private readonly ITopologyConfigLookup _configLookup;
        [Newtonsoft.Json.JsonIgnore]
        public readonly ITransformLookup _copyTransLookup;
        [Newtonsoft.Json.JsonIgnore]
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
        private System.Timers.Timer _readTimer = new System.Timers.Timer();
        private bool _changed = false;
        public bool Changed { get => _changed; set => _changed = value; }

        private CancellationTokenSource cts;

        private IDictionary<uint, SmlsCanFrame> _configListCAN;
        public IDictionary<uint, SmlsCanFrame> ConfigListCAN
        {
            get
            {
                if (_configListCAN == null)
                {
                    InitTopoConfigList();
                }
                return _configListCAN;
            }
            set
            {
                _configListCAN = value;
            }
        }

        private List<ShowParams> _ShowJsonParams;

        public List<ShowParams> ShowJsonParams
        {
            get
            {
                if (_ShowJsonParams == null)
                {
                    _ShowJsonParams = new List<ShowParams>();
                    _ShowJsonParams.AddRange(InitBindParamsForJson());
                }
                return _ShowJsonParams;
            }
            set
            {
                _ShowJsonParams = value;
            }
        }

        private IDictionary<AddressName, TopoConfigMeta<AddressName>> _configList;
        public IDictionary<AddressName, TopoConfigMeta<AddressName>> ConfigList
        {
            get
            {
                if (_configList == null)
                {
                    InitTopoConfigList();
                }
                return _configList;
            }
            set
            {
                _configList = value;
            }
        }
        public CANMaster(ITopologyConfigLookup configLookup, IDictionary<string, CANModelParam> registers, ITransformLookup transLookup) : base(registers, transLookup)
        {
            _configLookup = configLookup;
            //_comMaster = comMaster;
            //_id = id;
            _copyTransLookup = transLookup;
            _readTimer.Interval = 500;
            _readTimer.Elapsed += ReadTimer_Elapsed;
            InitTopoConfigList();
            InitDic();
        }
        //public CANMaster(CANMaster topologyMaster, IDictionary<string, CANModelParam> registers, ITransformLookup transLookup) : base(registers, transLookup)
        //{
        //    _configLookup = topologyMaster._configLookup;
        //    _comMaster = topologyMaster._comMaster;
        //    _id = topologyMaster.Id;
        //    Changed = topologyMaster.Changed;
        //    _copyTransLookup = topologyMaster._copyTransLookup;
        //    ConfigList = topologyMaster.ConfigList;
        //    ConfigListCAN = topologyMaster.ConfigListCAN;
        //    InitDic();
        //    CopyDic(topologyMaster);
        //}

        public void IsConnectToken()
        {

        }

        private void CopyDic(CANMaster topologyMaster)
        {
            foreach (var dic in topologyMaster.DicCache)
            {
                if (!base.DicCache.ContainsKey(dic.Key))
                {
                    DicCache.Add(dic.Key, dic.Value);
                }
                else
                {
                    DicCache[dic.Key] = dic.Value;
                }
            }
        }

        /// <summary>
        /// 初始化数据缓存
        /// </summary>
        private void InitDic()
        {
            foreach (var con in _configListCAN)
            {
                foreach (var c in con.Value.DataDetails)
                {
                    if (!base.DicCache.ContainsKey(c.Value.CanDataName))
                    {
                        if (c.Value.CanDataName == "OUTPUT_VOLTAGE")
                        {
                            string a = "";
                        }
                        if (c.Value.RegisterType == RegisterTypeParam.FLOAT)
                        {
                            DicCache.Add(c.Value.CanDataName, new CANModelParam() { DisplayValue = Convert.ToSingle(c.Value.DefaultValue) });
                        }
                        if (c.Value.RegisterType == RegisterTypeParam.UINT16)
                        {
                            DicCache.Add(c.Value.CanDataName, new CANModelParam() { DisplayValue = Convert.ToUInt16(c.Value.DefaultValue) });
                        }
                        if (c.Value.RegisterType == RegisterTypeParam.INT32)
                        {
                            DicCache.Add(c.Value.CanDataName, new CANModelParam() { DisplayValue = Convert.ToInt32(c.Value.DefaultValue) });
                        }
                        if (c.Value.RegisterType == RegisterTypeParam.DECIMAL)
                        {
                            DicCache.Add(c.Value.CanDataName, new CANModelParam() { DisplayValue = Convert.ToDecimal(c.Value.DefaultValue) });
                        }
                        if (c.Value.RegisterType == RegisterTypeParam.BYTE)
                        {
                            DicCache.Add(c.Value.CanDataName, new CANModelParam() { DisplayValue = Convert.ToByte(c.Value.DefaultValue) });
                        }
                        //DicCache.Add(c.CanDataName, new CANModelParam() { DisplayValue = c.DefaultValue });
                    }
                }
            }
        }
        /// <summary>
        /// 初始化动态绑定数据
        /// </summary>
        public List<ShowParams> InitBindParamsForJson(bool forOpen = false)
        {
            List<ShowParams> initList = new List<ShowParams>();
            foreach (var config in ConfigListCAN)
            {
                foreach (var sm in config.Value.DataDetails)
                {
                    var adName = (AddressName)System.Enum.Parse(typeof(AddressName), sm.Value.CanDataName);
                    var initShowValue = (float)(sm.Value.DefaultValue == null ? 0 : sm.Value.DefaultValue);
                    if (forOpen)
                    {
                        initShowValue = GetValue<float>(adName, false);
                    }
                    ShowParams tempParms = new ShowParams();

                    tempParms.AddressName = adName;
                    tempParms.ShowName = sm.Value.Comment;
                    tempParms.ShowSuffix = sm.Value.ShowSuffix;
                    tempParms.Precision = sm.Value.Precision.Value;
                    tempParms.ShowPrecision = tempParms.Precision == 0 ? CommonParametersName.ShowPrecision + "1" : CommonParametersName.ShowPrecision + Math.Pow(10, -tempParms.Precision).ToString();
                    tempParms.RegType = (RegisterType)sm.Value.RegisterType;
                    tempParms.ShowValue = initShowValue;
                    tempParms.DefaultValue = sm.Value.DefaultValue == null ? "0" : Convert.ToString(sm.Value.DefaultValue);
                    tempParms.Change = false;
                    tempParms.MaxValue = sm.Value.MaxValue;
                    tempParms.MinValue = sm.Value.MinValue;

                    initList.Add(tempParms);
                }
            }
            return initList;
        }
        public static double GetPrecision(int input)
        {
            // 使用Math.Pow来计算10的负input次幂
            return Math.Pow(10, -input);
        }
        /// <summary>
        /// 初始化配置信息
        /// </summary>
        private void InitTopoConfigList()
        {
            try
            {
                _configListCAN = _configLookup.GetConfig(_id).CANMetaConfig;
            }
            catch (Exception ex)
            { }
        }

        public IParamInfo GetConfigParamInfo(AddressName name)
        {
            return _configLookup.GetConfig(_id).GetParamInfo(name);
        }
        public IParamInfo GetConfigParamInfo(AddressName name, uint mailBox)
        {
            return _configLookup.GetConfig(_id).GetParamInfo(mailBox, name);
        }
        #region ReadAndWrite

        private void SyncFromSlave(ushort startAddress, ushort numOfRegisters)
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接！");
                return;
            }
            try
            {
                //var slaveRet = _comMaster.ReadHoldingRegisters(startAddress, numOfRegisters);
                //Registers.WritePoints(startAddress, slaveRet);
            }
            catch (Exception ex)
            { }
        }

        public T GetValue<T>(AddressName name, bool isSyncSlave = true)
        {
            if (!DicCache.ContainsKey(name.ToString()))
            {
                return default;
            }
            var curValue = ConfigListCAN.Where(t => t.Value.Datas.Where(x => x.CanDataName == name.ToString()).FirstOrDefault() != null).FirstOrDefault();
            var mailFrame = curValue.Value;
            var thisValue = mailFrame.DataDetails[name.ToString()];

            var val = base.DicCache[name.ToString()];
            var returnValue = val.DisplayValue;
            if (returnValue == null)
                returnValue = 0;
            return (T)Convert.ChangeType(returnValue, typeof(T));
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
                //PPEC.Logging.Log.Info("PPEC未连接");
                return default;
            }
            try
            {
                //var ret = await _comMaster.ReadHoldingRegistersAsync(info.StartAddress, info.NumOfRegisters, cts.Token);
                //if (ret != null)
                //    Registers.WritePoints(info.StartAddress, ret);
                //base.DicCache[name.ToString()] = "";

                var ss = base.DicCache[name.ToString()];
                return (T)ss.DisplayValue;
            }
            catch (Exception ex)
            {
                return base.GetValue<T>(info);
            }

        }

        /// <summary>
        /// 批量读取
        /// </summary>
        /// <param name="listName">需要读取的参数</param>
        /// <returns></returns>
        public List<T> GetMultiValue<T>(List<AddressName> listName, bool isSyncSlave = true) where T : struct
        {
            if (isSyncSlave)
            {
                if (!_comMaster.IsConnected())
                {
                    //PPEC.Logging.Log.Info("PPEC未连接");
                    return default;
                }
                ushort registerCnt = 0;
                var startInfo = _configLookup.GetConfig(_id).GetParamInfo(listName[0]);
                foreach (var name in listName)
                {
                    registerCnt += _configLookup.GetConfig(_id).GetParamInfo(name).NumOfRegisters;
                }
                var slaveRet = _comMaster.ReadHoldingRegisters(startInfo.StartAddress, registerCnt);
                Registers.WritePoints(startInfo.StartAddress, slaveRet);
            }

            List<T> ret = new List<T>();
            foreach (var name in listName)
            {
                var info = _configLookup.GetConfig(_id).GetParamInfo(name);
                ret.Add(base.GetValue<T>(info));
            }
            return ret;
        }

        public async Task<List<T>> GetMultiValueAsync<T>(List<AddressName> listName, bool isSyncSlave = true) where T : struct
        {
            try
            {
                List<T> ret = new List<T>();
                foreach (var name in listName)
                {
                    ret.Add(this.GetValue<T>(name));
                }
                return ret;

            }
            catch (Exception ex)
            {
                return default;
            }
        }

        /// <summary>
        /// 批量写入参数到寄存器
        /// </summary>
        public void SlaveToBufferBatch(ushort startAddress = 0, ushort numOfRegisters = 125)
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接");
                return;
            }
            var slaveRet = _comMaster.ReadHoldingRegisters(startAddress, numOfRegisters);
            Registers.WritePoints(startAddress, slaveRet);
        }
        public void SlaveToBufferBatch(AddressName addressName, ushort numOfRegisters = 125)
        {
            try
            {
                ushort startAddress = 0;
                if (!_comMaster.IsConnected())
                {
                    //PPEC.Logging.Log.Info("PPEC未连接");
                    return;
                }
                //var startAddressName = _configLookup.GetConfig(_id).GetParamInfo(addressName);
                //if (startAddressName == null)
                //    return;
                //startAddress = startAddressName.StartAddress;
                //var slaveRet = _comMaster.ReadHoldingRegisters(startAddress, numOfRegisters);
                //Registers.WritePoints(startAddress, slaveRet);
            }
            catch (Exception ex)
            { }

        }
        public async Task SlaveToBufferBatchAsync(ushort startAddress = 0, ushort numOfRegisters = 125)
        {
            try
            {
                if (!_comMaster.IsConnected())
                {
                    //PPEC.Logging.Log.Info("PPEC未连接");
                    return;
                }
                //var slaveRet = await _comMaster.ReadHoldingRegistersAsync(startAddress, numOfRegisters, cts.Token);
                //Registers.WritePoints(startAddress, slaveRet);
            }
            catch (Exception ex)
            {
                string a = "";
            }

        }
        /// <summary>
        /// 根据AddressName设置参数,异步
        /// </summary>
        /// <param name="isSyncSlave">isSyncSlave默认为True,isSyncSlave为False时读取Buffer</param>
        public void SetValue<T>(T value, AddressName name, bool isSyncSlave = true)
        {
            if (!this.ComMaster.IsConnected())
                return;
            //获取配置中对应的参数
            var curValue = ConfigListCAN.Where(t => t.Value.Datas.Where(x => x.CanDataName == name.ToString()).FirstOrDefault() != null).FirstOrDefault();

            //获取邮箱
            uint mailId = curValue.Key;
            //获取邮箱对应的帧数据
            var mailFrame = curValue.Value;
            if (!mailFrame.DataDetails.ContainsKey(name.ToString()))
            {
                return;
            }
            var thisValue = mailFrame.DataDetails[name.ToString()];
            IParamInfo info = new CANParamInfo(thisValue.StartIndex, thisValue.NumOfData, thisValue.Unit, thisValue.Comment);

            //获取当前缓存数据 CANModelParam 
            if (!DicCache.ContainsKey(name.ToString()))
            {
                return;
            }
            var dic = DicCache[name.ToString()];
            dic.DisplayValue = value;
            base.SetValue(value, info, name.ToString());
            //获取默认值（无效值），只修改帧中需要修改的byte[]字节，其它字节为无效值不修改。
            List<byte> data = mailFrame.GetOriginalFrame(DicCache);

            if (isSyncSlave)
                ComMaster.SendDataToCan(mailId, data.ToArray());


            Changed = true;
        }

        /// <summary>
        /// 异步
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task SetValueAsync<T>(T value, AddressName name) where T : struct
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接");
                return;
            }
            //var info = _configLookup.GetConfig(_id).GetParamInfo(name);
            //if (info == null)
            //return;
            try
            {
                SetValue(value, name);
            }
            catch (Exception ex)
            {
                //Log.Error(ex.Message); 
            }
        }
        /// <summary>
        /// 将Buffer中的信息下发到下位机
        /// </summary>
        public void ASyncToSlave(AddressName name, IParamInfo info = null)
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
            _comMaster.WriteMultipleRegistersAsync(info.StartAddress, val);
        }
        public void SyncToSlave(AddressName name, IParamInfo info = null)
        { }
        public void SyncToSlave(string name)
        {
            try
            {
                //获取配置中对应的参数
                var curValue = ConfigListCAN.Where(t => t.Value.Datas.Where(x => x.CanDataName == name.ToString()).FirstOrDefault() != null).FirstOrDefault();

                //获取邮箱
                uint mailId = curValue.Key;
                var mailFrame = curValue.Value;
                var thisValue = mailFrame.DataDetails[name.ToString()];
                IParamInfo info = new CANParamInfo(thisValue.StartIndex, thisValue.NumOfData, thisValue.Unit, thisValue.Comment);

                //获取邮箱对应的帧数据
                //获取当前缓存数据 CANModelParam 
                if (!DicCache.ContainsKey(name.ToString()))
                {
                    return;
                }
                var dic = DicCache[name.ToString()];
                List<byte> data = mailFrame.GetOriginalFrame(DicCache);

                ComMaster.SendDataToCan(mailId, data.ToArray());


                Changed = true;

            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// 批量写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listName">批量寄存器地址</param>
        /// <param name="listValue">需要写入的数据</param>
        /// <param name="isSyncSlave">是否只存储到BUFFER</param>
        public void SetMultiValue<T>(List<AddressName> listName, List<T> listValue, bool isSyncSlave = true) where T : struct
        {
            var info = _configLookup.GetConfig(_id).GetParamInfo(listName[0]);
            List<ushort> listValueUshort = new List<ushort>();
            for (int i = 0; i < listName.Count; i++)
            {
                var infoWrite = _configLookup.GetConfig(_id).GetParamInfo(listName[i]);
                base.SetValue(listValue[i], infoWrite);
                var val = Registers.ReadPoints(infoWrite.StartAddress, infoWrite.NumOfRegisters);
                listValueUshort.AddRange(val);
            }
            if (isSyncSlave)
            {
                if (!_comMaster.IsConnected())
                {
                    //PPEC.Logging.Log.Info("PPEC未连接");
                    return;
                }
                _comMaster.WriteMultipleRegisters(info.StartAddress, listValueUshort.ToArray());
            }
        }
        /// <summary>
        /// 批量异步写入
        /// </summary>
        public async Task SetMultiValueAsync<T>(List<AddressName> listName, List<T> listValue) where T : struct
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接");
                return;
            }
            var info = _configLookup.GetConfig(_id).GetParamInfo(listName[0]);
            List<ushort> listValueUshort = new List<ushort>();
            for (int i = 0; i < listName.Count; i++)
            {
                var infoWrite = _configLookup.GetConfig(_id).GetParamInfo(listName[i]);
                this.SetValue(listValue[i], listName[i]);
                //base.SetValue(listValue[i], infoWrite, listName[i].ToString());
                //var val = Registers.ReadPoints(infoWrite.StartAddress, infoWrite.NumOfRegisters);
                //listValueUshort.AddRange(val);
            }
            try
            {
                //await _comMaster.WriteMultipleRegistersAsync(info.StartAddress, listValueUshort.ToArray(), cts.Token);
            }
            catch (Exception ex)
            {
            }

        }

        public void BufferToSlaveBatch(ushort[] data, ushort startAddress = 0)
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接");
                return;
            }
            _comMaster.WriteMultipleRegisters(startAddress, data);
        }

        public async Task BufferToSlaveBatchAsync(ushort[] data, ushort startAddress = 0)
        {
            if (!_comMaster.IsConnected())
            {
                //PPEC.Logging.Log.Info("PPEC未连接");
                return;
            }
            await _comMaster.WriteMultipleRegistersAsync(startAddress, data, cts.Token);
        }

        public async Task<T> WriteReadAsync<T>(T value, AddressName name) where T : struct
        {
            await SetValueAsync<T>(value, name);
            return await GetValueAsync<T>(name);
        }
        #endregion
        public void CancelCts()
        {
            //cts.Cancel();
            //if (cts.Token.IsCancellationRequested == true)
            //{
            //    cts.Dispose();
            //    cts = new CancellationTokenSource();
            //}
        }
        //private void ReadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    if (ComMaster.IsConnected())
        //    {
        //        UInt32 res = new UInt32();
        //        //var has = ComMaster.HasReceive();
        //        //if (!has)
        //        //    return;
        //        res = Device.VCI_GetReceiveNum(ComMaster.CANCommConfig.DeviceType, ComMaster.CANCommConfig.DeviceInd, ComMaster.CANCommConfig.CanInd);
        //        if (res == 0)
        //            return;
        //        UInt32 con_maxlen = 50;
        //        IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * (Int32)con_maxlen);

        //        res = Device.VCI_Receive(ComMaster.CANCommConfig.DeviceType, ComMaster.CANCommConfig.DeviceInd, ComMaster.CANCommConfig.CanInd, pt, con_maxlen, 100);

        //        for (UInt32 i = 0; i < res; i++)
        //        {
        //            VCI_CAN_OBJ obj =  (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((UInt32)pt + i * Marshal.SizeOf(typeof(VCI_CAN_OBJ))), typeof(VCI_CAN_OBJ));
        //            //VCI_CAN_OBJ obj1 = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((uint)pt + (i * Marshal.SizeOf(typeof(VCI_CAN_OBJ)))), typeof(VCI_CAN_OBJ));

        //            if (obj.RemoteFlag == 0)
        //            {
        //                var frame= ConfigListCAN[obj.ID];
        //                SetDataToDic(obj.Data,frame);
        //            }
        //        }

        //        Marshal.FreeHGlobal(pt);
        //    }
        //    else
        //    {
        //        ComMaster.Connect();
        //    }
        //}
        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];
        unsafe private void ReadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ComMaster.IsConnected())
            {

                UInt32 res = new UInt32();

                res = Device64.VCI_Receive(ComMaster.CANCommConfig.DeviceType, ComMaster.CANCommConfig.DeviceInd, ComMaster.CANCommConfig.CanInd, ref m_recobj[0], 1000, 100);

                if (res == 0xFFFFFFFF) res = 0;
                String str = "";
                for (UInt32 i = 0; i < res; i++)
                {

                    str += "  帧ID:0x" + System.Convert.ToString(m_recobj[i].ID, 16);

                    if (m_recobj[i].RemoteFlag == 0)
                    {
                        byte len = (byte)(m_recobj[i].DataLen % 9);

                        var frame = ConfigListCAN[m_recobj[i].ID];
                        fixed (VCI_CAN_OBJ* m_recobj1 = &m_recobj[i])
                        {
                            byte[] byteArray = new byte[8];
                            byteArray[0] = m_recobj1->Data[0];
                            byteArray[1] = m_recobj1->Data[1];
                            byteArray[2] = m_recobj1->Data[2];
                            byteArray[3] = m_recobj1->Data[3];
                            byteArray[4] = m_recobj1->Data[4];
                            byteArray[5] = m_recobj1->Data[5];
                            byteArray[6] = m_recobj1->Data[6];
                            byteArray[7] = m_recobj1->Data[7];
                            //Marshal.Copy((IntPtr)m_recobj1->Data, byteArray, 0, 8);

                            SetDataToDic(byteArray, frame);
                        }
                    }
                }
            }
            else
            {
                ComMaster.Connect();
            }
        }
        /// <summary>
        /// 将收到的数据帧存到缓存
        /// </summary>
        /// <param name="datas">收到的数据</param>
        /// <param name="scf">配置信息</param>
        public void SetDataToDic(byte[] datas, SmlsCanFrame scf)
        {
            foreach (var scd in scf.Datas)
            {
                if (!base.DicCache.ContainsKey(scd.CanDataName))
                {
                    DicCache.Add(scd.CanDataName, new CANModelParam());
                }
                IParamInfo info = new CANParamInfo(scd.StartIndex, scd.NumOfData, scd.Unit, scd.Comment);
                base.DicCache[scd.CanDataName].Recevie = datas;
                dynamic value = base.GetValue<float>(info, scd.CanDataName);
                switch (info.RegisterType)
                {
                    case RegisterTypeParam.UINT16:
                        value = base.GetValue<ushort>(info, scd.CanDataName);
                        break;
                    case RegisterTypeParam.FLOAT:
                        value = base.GetValue<float>(info, scd.CanDataName);
                        break;
                    case RegisterTypeParam.INT32:
                        value = base.GetValue<int>(info, scd.CanDataName);
                        break;
                    case RegisterTypeParam.DECIMAL:
                        value = base.GetValue<decimal>(info, scd.CanDataName);
                        break;
                    case RegisterTypeParam.SHORT:
                        value = base.GetValue<short>(info, scd.CanDataName);
                        break;
                    default:
                        value = base.GetValue<float>(info, scd.CanDataName);
                        break;
                }
                DicCache[scd.CanDataName].DisplayValue = value;
            }
        }

        public void Start()
        {
            if (_readTimer != null)
                _readTimer.Start();
        }
        public void Stop()
        {
            if (_readTimer != null)
                _readTimer.Stop();
        }
    }
}
