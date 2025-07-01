using PPEC.Communication;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;
using Prism.Mvvm;
using System.Collections.ObjectModel;

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
        public ChipInfo _Chip;
        public ChipInfo Chip
        {
            get { return _Chip; }
            set { SetProperty(ref _Chip, value); }
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


        public bool _isSelected = false;
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
        public GroundDevice _connectDevice;
        public GroundDevice ConnectDevice
        {
            get { return _connectDevice; }
            set { SetProperty(ref _connectDevice, value); }
        }
        internal void Disconnect()
        {
            Master.Stop();
            IsTrueConnected = false;
        }
    }
}
