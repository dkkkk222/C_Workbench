using Newtonsoft.Json;
using NPOI.Util;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Workbench.Communication;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.Models
{
    public class PpecProject : BindableBase
    {
        public string UID { get; set; }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }
        private ChipInfo _chip;
        public ChipInfo Chip
        {
            get { return _chip; }
            set { SetProperty(ref _chip, value); }
        }
        public string Path { get; set; }
        public string ProjectMark { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
        public string Level { get; set; }
        public string ReName { get; set; }

        /// <summary>
        /// 最近一次连接的端口名
        /// </summary>
        public string LastPortName { get; set; }
        public int? Password { get; set; }
        public string PPEC_Id { get; set; }

        private bool _isTrueConnected = false;
        /// <summary>
        /// 是否真连接
        /// </summary>
        public bool IsTrueConnected
        {
            get { return _isTrueConnected; }
            set { SetProperty(ref _isTrueConnected, value); }
        }


        private bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        public string ProjectId { get; set; }

        private ObservableCollection<PpecProject> _children = new ObservableCollection<PpecProject>();
        public ObservableCollection<PpecProject> Children
        {
            get { return _children; }
            set { SetProperty(ref _children, value); }
        }

        private string _communicationType;

        /// <summary>
        /// 通讯方式
        /// </summary>
        public string CommunicationType
        {
            get { return _communicationType; }
            set { SetProperty(ref _communicationType, value); }
        }

        private string _portName = "COM1";
        /// <summary>
        /// 端口
        /// </summary>
        public string PortName
        {
            get { return _portName; }
            set
            {
                SetProperty(ref _portName, value);
            }
        }

        private ITopologyMaster _master;
        public ITopologyMaster Master
        {
            get { return _master; }
            set { SetProperty(ref _master, value); }
        }

        private ICommChannel _channelMaster;
        public ICommChannel ChannelMaster
        {
            get { return _channelMaster; }
            set { SetProperty(ref _channelMaster, value); }
        }
        private GroundDevice _connectDevice;
        public GroundDevice ConnectDevice
        {
            get { return _connectDevice; }
            set { SetProperty(ref _connectDevice, value); }
        }

        private ObservableCollection<SingleParamHistory> _readWriteHistory = new ObservableCollection<SingleParamHistory>();

        [JsonIgnore]
        public ObservableCollection<SingleParamHistory> ReadWriteHistory
        {
            get => _readWriteHistory;
            set => SetProperty(ref _readWriteHistory, value);
        }

        private ObservableCollection<Sequence> _sequences = new ObservableCollection<Sequence>();
        public ObservableCollection<Sequence> Sequences
        {
            get => _sequences;
            set => SetProperty(ref _sequences, value);
        }

        private GroundDevice _groundService;
        public GroundDevice GroundDevice
        {
            get => _groundService;
            set => SetProperty(ref _groundService, value);
        }

        internal void Disconnect()
        {
            Master.Stop();
            IsTrueConnected = false;
        }

        internal async Task ConnectAsync()
        {
            switch (CommunicationType)
            {
                case Constants.Modbus:
                    await ConnectSerialPort();
                    break;
                default:
                    break;
            }
        }

        private async Task ConnectSerialPort()
        {
            var service = new SerialPortService();
            service.Connect(PortName);

            var bytes = Utility.HexToBytes("D28C000AFFFFFFFFFFFFFF000AFF0003017050A9");
            await service.SendAsync(bytes);
            service.DataParser += (byte[] data) =>
            {
                string hex = Utility.ToHexString(data);

                byte[] addressBytes = new byte[2];
                Array.Copy(data, 16, addressBytes, 0, 2);
                string addressHex = Utility.ToHexString(addressBytes);

                byte[] dataBytes = new byte[4];
                Array.Copy(data, 18, dataBytes, 0, 4);
                string dataStr = Utility.ToHexString(dataBytes);
                var decValue = Utility.ParseHexToUInt(dataStr);

                return (addressHex, decValue); ;
            };
        }
    }
}
