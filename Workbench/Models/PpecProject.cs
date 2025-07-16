using Newtonsoft.Json;
using PPEC.Communication;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private bool _isConnecting = false;
        [JsonIgnore]
        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        private string _communicationType = Constants.SERIAL_PORT;

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
        [JsonIgnore]
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
        [JsonIgnore]
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

        private ObservableCollection<WatchGroup> _watchGroups = new ObservableCollection<WatchGroup>();
        public ObservableCollection<WatchGroup> WatchGroups
        {
            get => _watchGroups;
            set => SetProperty(ref _watchGroups, value);
        }

        private IBaseCommService _commService;
        [JsonIgnore]
        public IBaseCommService CommService
        {
            get => _commService;
            set => SetProperty(ref _commService, value);
        }

        internal void Disconnect()
        {
            if (CommService != null)
            {
                CommService.Close();
                IsConnecting = false;
            }
        }

        internal async Task<bool> ConnectAsync()
        {
            switch (CommunicationType)
            {
                case Constants.SERIAL_PORT:
                    return await ConnectSerialPort();
                default:
                    break;
            }
            return true;
        }

        private async Task<bool> ConnectSerialPort()
        {
            var service = new SerialPortService();
            //连接串口
            service.Connect(PortName);
            //接收数据解析规则
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

            CommService = service;

            var bytes = Utility.HexToBytes("D28C000AFFFFFFFFFFFFFF000AFF0003017050A9");
            await CommService.SendAsync(bytes);
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            var res = CommService.Read("0170");
            if (!res.HasValue)
            {
                MessageBox.Show("板卡连接异常，请检查", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IsConnecting = false;
                service.Close();
                return false;
            }
            else
            {
                IsTrueConnected = CommService.IsConnected;
                IsConnecting = true;
                return true;
            }
        }
    }
}
