using log4net;
using MathNet.Numerics.LinearAlgebra.Factorization;
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
using Workbench.Events;
using Workbench.Models.dw;
using Workbench.Utils;
using Workbench.ViewModels.dw;

namespace Workbench.Models
{
    public class PpecProject : BindableBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PpecProject));
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
            set
            {
                if (SetProperty(ref _communicationType, value))
                { 
                
                }
            }
        }

        #region I2C
        private int _i2cBusId = 0;
        /// <summary>
        /// 总线号
        /// </summary>
        public int I2cBusId
        {
            get => _i2cBusId;
            set => SetProperty(ref _i2cBusId, value);
        }

        private int _i2cAddrBits = 0; // b2b1b0，范围 0..7
        /// <summary>
        /// 地址位
        /// </summary>
        public int I2cAddrBits
        {
            get => _i2cAddrBits;
            set => SetProperty(ref _i2cAddrBits, value);
        }
        private string _Delay = "0";
        public string Delay
        {
            get => _Delay;
            set => SetProperty(ref _Delay, value);
        }

        private string _CanDelay = "5";
        public string CanDelay
        {
            get => _CanDelay;
            set => SetProperty(ref _CanDelay, value);
        }
        private int _I2cBaud = 1;
        /// <summary>
        /// clock
        /// </summary>
        public int I2cBaud
        {
            get => _I2cBaud;
            set => SetProperty(ref _I2cBaud, value);
        }

        private string _ConnectDeviceIndex = "0";
        public string ConnectDeviceIndex
        {
            get => _ConnectDeviceIndex;
            set => SetProperty(ref _ConnectDeviceIndex, value);
        }

        private string _I2CSCL = "0";
        public string I2CSCL
        {
            get => _I2CSCL;
            set => SetProperty(ref _I2CSCL, value);
        }
        #endregion

        #region CAN
        private int _DeviceType = 21;
        /// <summary>
        /// DeviceType
        /// </summary>
        public int DeviceType
        {
            get => _DeviceType;
            set => SetProperty(ref _DeviceType, value);
        }

        private int _SelectedDeviceId = 0; // b2b1b0，范围 0..7
        /// <summary>
        /// DEVICEID
        /// </summary>
        public int SelectedDeviceId
        {
            get => _SelectedDeviceId;
            set => SetProperty(ref _SelectedDeviceId, value);
        }

        private int _SelectedCanId = 0;
        /// <summary>
        /// CANID
        /// </summary>
        public int SelectedCanId
        {
            get => _SelectedCanId;
            set => SetProperty(ref _SelectedCanId, value);
        }

        private int _selectedBaudIndex = 0;
        /// <summary>
        /// CANID
        /// </summary>
        public int SelectedBaudIndex
        {
            get => _selectedBaudIndex;
            set => SetProperty(ref _selectedBaudIndex, value);
        }
        #endregion
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
        private int _buandName = 115200;
        public int BuandName
        {
            get { return _buandName; }
            set
            {
                SetProperty(ref _buandName, value);
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

        //private ObservableCollection<WatchChartModel> _watchChartGroups = new ObservableCollection<WatchChartModel>() {
        //       new WatchChartModel("监测图") {
        //        Id = Guid.NewGuid().ToString("N"),
        //        Header = $"未选中",
        //    }};
        private ObservableCollection<WatchChartModel> _watchChartGroups = new ObservableCollection<WatchChartModel>();
        public ObservableCollection<WatchChartModel> WatchChartGroups
        {
            get => _watchChartGroups;
            set => SetProperty(ref _watchChartGroups, value);
        }

        private ObservableCollection<RegisterAddrInfo> _categoryRegisters = new ObservableCollection<RegisterAddrInfo>();
        public ObservableCollection<RegisterAddrInfo> CategoryRegisters
        {
            get => _categoryRegisters;
            set => SetProperty(ref _categoryRegisters, value);
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
                case Constants.OldSERIAL_PORT:
                case Constants.SERIAL_PORT:
                    return await ConnectSerialPort();
                case Constants.I2C:
                    return await ConnectI2c();
                case Constants.CAN:
                    return await ConnectCan();

                default:
                    break;
            }
            return true;
        }

        private async Task<bool> ConnectSerialPort()
        {
            var service = new SerialPortService();
            try
            {
                
                //连接串口
                service.Connect(PortName, BuandName);
                //接收数据解析规则
                service.DataParser += (byte[] data) =>
                {
                    if (data == null || data.Length < 22)
                        return (string.Empty, 0u);
                    string hex = Utility.ToHexString(data);

                    byte[] addressBytes = new byte[2];
                    Array.Copy(data, 16, addressBytes, 0, 2);
                    string addressHex = Utility.ToHexString(addressBytes);

                    byte[] dataBytes = new byte[4];
                    Array.Copy(data, 18, dataBytes, 0, 4);
                    string dataStr = Utility.ToHexString(dataBytes);
                    var decValue = Utility.ParseHexToUInt(dataStr);

                    return (addressHex, decValue);
                };

                CommService = service;

                var calcResult = UtilsFunc.GetReadCommandByAddress("0170", Constants.Modbus);
                await CommService.SendAsync(calcResult.bytes);
                //var bytes = Utility.HexToBytes("D28C000AFFFFFFFFFFFFFF000AFF0003017050A9");
                //await CommService.SendAsync(bytes);
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
            catch(Exception ex)
            {
                IsConnecting = false;
                service.Close();
                _log.Error(ex);
                return false;
            }
            
        }

        public async Task<bool> ConnectCan()
        {
            try
            {
                var opts = new CanConnectOptions
                {
                    DevType = 21,   // 例如用户选的 USBCAN-2E-U
                    DevId = (uint)SelectedDeviceId,
                    CanId = (uint)SelectedCanId,   // 0 或 1
                    BaudIndex = SelectedBaudIndex,     // 按你的映射
                    SendTimeoutMs = 400                // 可选
                };
                // 连接：USBCAN-2E-U(21), Dev0, CAN0, 500kbps(index=1)
                var can = new CanCommService1();
                can.Delay = CanDelay;
                can.Connect($"CAN:{DeviceType}:{SelectedDeviceId}:{SelectedCanId}:{SelectedBaudIndex}");
                CommService = can;
                IsConnecting = true;
                //can.FrameParser += (VCI_CAN_OBJ data) =>
                //{
                //    switch(data.ID)
                //    {
                //        case 0:
                //            break;
                //    }
                //    string hex = Utility.ToHexString(data);

                //    byte[] addressBytes = new byte[2];
                //    Array.Copy(data, 16, addressBytes, 0, 2);
                //    string addressHex = Utility.ToHexString(addressBytes);

                //    byte[] dataBytes = new byte[4];
                //    Array.Copy(data, 18, dataBytes, 0, 4);
                //    string dataStr = Utility.ToHexString(dataBytes);
                //    var decValue = Utility.ParseHexToUInt(dataStr);

                //    return (addressHex, decValue);
                //};
                // 复位（在 CANA 上，目标 A0）
                //await can.ResetAsync(useCanB: false, dest: 0xA0);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                IsConnecting = false;
                return false;
            }
            
           
        }
        private async Task<bool> ConnectI2c()
        {
            // var service = new Workbench.Communication.I2cCommService();

            // 复用现有 IBaseCommService.Connect 的签名，portName 里塞 I2C 参数
            //string i2cPortToken = $"I2C:{I2cBusId}:{I2cAddrBits}";
            var service = new Workbench.Communication.Ch347I2cCommService();
            try
            {

                string connectStr = string.Format($"CH347:{I2cBusId}:{ConnectDeviceIndex}:{I2cBaud}:{I2CSCL}:{Delay}:0:500:500");
                service.Connect(connectStr);// ("CH347:0:3:1:0:0:0:500:500");
                //service.Connect(i2cPortToken, 0, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"I2C 连接失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IsConnecting = false;
                service.Close();
                return false;
            }

            CommService = service;

            // 3) 通信复位
            //await service.ResetAsync();

            // 4) 写寄存器
            //await service.WriteRegisterAsync(0x0170, 0x12345678);

            // 5) 读寄存器
            var val = await service.ReadRegisterAsync(0x0170);
            Console.WriteLine($"0x0170 = 0x{val:X8}");

            // （可选）按协议先来一次通信复位
            //var i2c = (Workbench.Communication.I2cCommService)CommService;
            //await i2c.ResetAsync(120); // 芯片约100ms复位，这里等120ms

            // 按你的串口逻辑：读地址 0x0170 作为在线校验
            //ushort probeReg = 0x0170;
            //var val = await i2c.ReadRegisterAsync(probeReg);
            if (!val.HasValue)
            {
                MessageBox.Show("板卡连接异常（I2C）无法正常读取寄存器数据，请检查接线/地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IsConnecting = false;
                service.Close();
                return false;
            }

            IsTrueConnected = CommService.IsConnected;
            IsConnecting = true;
            return true;
        }
    }
}
