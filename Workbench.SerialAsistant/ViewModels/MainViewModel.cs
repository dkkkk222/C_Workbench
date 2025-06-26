using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Workbench.SerialAsistant.Enums;
using Workbench.SerialAsistant.Events;
using Workbench.SerialAsistant.Models;
using Workbench.SerialAsistant.Utils;

namespace Workbench.SerialAsistant.ViewModels
{
    public class MainViewModel : BindableBase, IDialogAware
    {
        private IComMaster _master;
        private readonly IEventAggregator _eventAggregator;
        public MainViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            //初始化连接配置
            InitDefaultFields();
            CreateUSBWatcher();
            EventListeners();
        }
        #region Properties

        private ObservableCollection<string> _SerialPortList = new ObservableCollection<string>();
        public ObservableCollection<string> SerialPortList
        {
            get { return _SerialPortList; }
            set { SetProperty(ref _SerialPortList, value); }
        }

        private string _selectedSerialPort;
        public string SelectedSerialPort
        {
            get => _selectedSerialPort;
            set => SetProperty(ref _selectedSerialPort, value);
        }

        private ObservableCollection<string> _ConnectTypeList = new ObservableCollection<string>();
        public ObservableCollection<string> ConnectTypeList
        {
            get { return _ConnectTypeList; }
            set { SetProperty(ref _ConnectTypeList, value); }
        }

        private string _selectedConnectType;
        public string SelectedConnectType
        {
            get => _selectedConnectType;
            set
            {
                IsPort = value == ConnectTypeEnum.串口.ToString();
                Dispose();
                if (value == ConnectTypeEnum.网口.ToString() && IsTcpServer)
                {
                    ShowConnectedClients = true;
                }
                else
                {
                    ShowConnectedClients = false;
                }
                SetProperty(ref _selectedConnectType, value);
            }
        }

        private ObservableCollection<SerialCommBoxItems> _baudRateList = CommEntity.SerialBaudRateItemList;
        public ObservableCollection<SerialCommBoxItems> BaudRateList
        {
            get => _baudRateList;
            set => SetProperty(ref _baudRateList, value);
        }

        private SerialCommBoxItems _selectedBaudRate;
        public SerialCommBoxItems SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        private ObservableCollection<int> _dataBit = new ObservableCollection<int>();
        public ObservableCollection<int> DataBit
        {
            get => _dataBit;
            set => SetProperty(ref _dataBit, value);
        }

        private int _selectedDataBit = 8;
        public int SelectedDataBit
        {
            get => _selectedDataBit;
            set => SetProperty(ref _selectedDataBit, value);
        }

        private ObservableCollection<SerialCommBoxItems> _stopBitList = CommEntity.SerialStopBitsItemList;
        public ObservableCollection<SerialCommBoxItems> StopBitList
        {
            get => _stopBitList;
            set => SetProperty(ref _stopBitList, value);
        }

        private SerialCommBoxItems _selectedStopBit;
        public SerialCommBoxItems SelectedStopBit
        {
            get => _selectedStopBit;
            set => SetProperty(ref _selectedStopBit, value);
        }

        private ObservableCollection<SerialCommBoxItems> _parityList = CommEntity.SerialParityItemList;
        public ObservableCollection<SerialCommBoxItems> ParityList
        {
            get => _parityList;
            set => SetProperty(ref _parityList, value);
        }

        private SerialCommBoxItems _selectedParity;
        public SerialCommBoxItems SelectedParity
        {
            get => _selectedParity;
            set => SetProperty(ref _selectedParity, value);
        }

        private int _bufferLength = 1024;
        public int BufferLength
        {
            get => _bufferLength;
            set => SetProperty(ref _bufferLength, value);
        }

        private ObservableCollection<SerialCommBoxItems> _flowControlList = CommEntity.SerialFlowControlItemList;
        public ObservableCollection<SerialCommBoxItems> FlowControlList
        {
            get => _flowControlList;
            set => SetProperty(ref _flowControlList, value);
        }

        private SerialCommBoxItems _selectedFlowControl;
        public SerialCommBoxItems SelectedFlowControl
        {
            get => _selectedFlowControl;
            set => SetProperty(ref _selectedFlowControl, value);
        }

        private string _hostIP = "127.0.0.1";
        public string HostIP
        {
            get => _hostIP;
            set => SetProperty(ref _hostIP, value);
        }

        private int _hostPort = 6000;
        public int HostPort
        {
            get => _hostPort;
            set => SetProperty(ref _hostPort, value);
        }

        private bool _isPort = true;
        public bool IsPort
        {
            get => _isPort;
            set => SetProperty(ref _isPort, value);
        }

        private ObservableCollection<RadioButtonOption> _sendMessageType = new ObservableCollection<RadioButtonOption>
        {
            new RadioButtonOption{ Name = Constants.ASCII},
            new RadioButtonOption{ Name = Constants.Hex, IsSelected = true}
        };
        public ObservableCollection<RadioButtonOption> SendMessageType
        {
            get => _sendMessageType;
            set => SetProperty(ref _sendMessageType, value);
        }

        private ObservableCollection<RadioButtonOption> _receiveMessageType = new ObservableCollection<RadioButtonOption>
        {
            new RadioButtonOption{ Name = Constants.ASCII},
            new RadioButtonOption{ Name = Constants.Hex, IsSelected = true}
        };
        public ObservableCollection<RadioButtonOption> ReceiveMessageType
        {
            get => _receiveMessageType;
            set => SetProperty(ref _receiveMessageType, value);
        }

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private string _statusStr = Constants.Disconnected;
        public string StatusStr
        {
            get => _statusStr;
            set => SetProperty(ref _statusStr, value);
        }

        private string _statusColor = Constants.Red;
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        private ObservableCollection<CommonOptions> _tcpMode = new ObservableCollection<CommonOptions>
        {
            new CommonOptions{ Name = "TCP Client", Value = Constants.TcpClient },
            new CommonOptions{ Name = "TCP Server", Value = Constants.TcpServer },
        };
        public ObservableCollection<CommonOptions> TcpMode
        {
            get => _tcpMode;
            set => SetProperty(ref _tcpMode, value);
        }

        private CommonOptions _selectedTcpMode;
        public CommonOptions SelectedTcpMode
        {
            get => _selectedTcpMode;
            set
            {
                if (value != null)
                {
                    IsTcpServer = value.Value == Constants.TcpServer;
                    ShowConnectedClients = value.Value == Constants.TcpServer;
                }
                SetProperty(ref _selectedTcpMode, value);
            }
        }

        private bool _isTcpServer = false;
        public bool IsTcpServer
        {
            get => _isTcpServer;
            set => SetProperty(ref _isTcpServer, value);
        }

        private ObservableCollection<string> _connectedClients = new ObservableCollection<string>();
        public ObservableCollection<string> ConnectedClients
        {
            get => _connectedClients;
            set => SetProperty(ref _connectedClients, value);
        }

        private string _selectedClientName;
        public string SelectedClientName
        {
            get => _selectedClientName;
            set => SetProperty(ref _selectedClientName, value);
        }

        private bool _showConnectedClients = false;
        public bool ShowConnectedClients
        {
            get => _showConnectedClients;
            set => SetProperty(ref _showConnectedClients, value);
        }

        private int _sendCount = 0;
        public int SendCount
        {
            get => _sendCount;
            set => SetProperty(ref _sendCount, value);
        }

        private int _receiveCount = 0;
        public int ReceiveCount
        {
            get => _receiveCount;
            set => SetProperty(ref _receiveCount, value);
        }


        private bool _showTime = true;
        public bool ShowTime
        {
            get => _showTime;
            set => SetProperty(ref _showTime, value);
        }

        private bool _showSend = true;
        public bool ShowSend
        {
            get => _showSend;
            set => SetProperty(ref _showSend, value);
        }

        #endregion

        #region Methods

        private void EventListeners()
        {
            _eventAggregator.GetEvent<TcpClientConnectedEvent>().Subscribe((clientName) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!ConnectedClients.Contains(clientName))
                    {
                        ConnectedClients.Add(clientName);
                    }
                });
            });

            _eventAggregator.GetEvent<TcpClientDisConnectedEvent>().Subscribe((clientName) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectedClients.Remove(clientName);
                });
            });
        }

        void InitDefaultFields()
        {
            SerialPortList = new ObservableCollection<string>(SerialPortHelper.GetPortNames().ToList());
            SelectedSerialPort = SerialPortList.FirstOrDefault();
            ConnectTypeList.AddRange(new List<string> { ConnectTypeEnum.串口.ToString(), ConnectTypeEnum.网口.ToString() });
            SelectedConnectType = ConnectTypeList.FirstOrDefault();
            SelectedBaudRate = BaudRateList.FirstOrDefault();
            SelectedStopBit = StopBitList.FirstOrDefault(t => t.Value == (int)StopBits.One);
            SelectedParity = ParityList.FirstOrDefault();
            SelectedFlowControl = FlowControlList.FirstOrDefault();
            DataBit.AddRange(Enumerable.Range(5, 4));
            SelectedTcpMode = TcpMode.FirstOrDefault();
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            _master?.Dispose();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private void Dispose()
        {
            _master?.Dispose();
            IsConnected = false;
            StatusColor = Constants.Red;
            StatusStr = Constants.Disconnected;
        }

        #endregion
        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        #region USBHardChange

        public ManagementEventWatcher USBInsert;
        public ManagementEventWatcher USBRemove;

        private void CreateUSBWatcher()
        {
            //建立监听
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;
            //建立插入监听
            try
            {
                WqlEventQuery USBInsertQuery = new WqlEventQuery("__InstanceCreationEvent", "TargetInstance ISA 'Win32_PnPEntity'");
                USBInsertQuery.WithinInterval = new TimeSpan(0, 0, 2);
                USBInsert = new ManagementEventWatcher(scope, USBInsertQuery);
                USBInsert.EventArrived += USBInsert_EventArrived;
                USBInsert.Start();
            }
            catch (Exception ex)
            {
                if (USBInsert != null)
                {
                    USBInsert.Stop();
                }
                throw ex;
            }
            //建立拔出监听
            try
            {
                WqlEventQuery USBRemoveQuery = new WqlEventQuery("__InstanceDeletionEvent", "TargetInstance ISA 'Win32_PnPEntity'");
                USBRemoveQuery.WithinInterval = new TimeSpan(0, 0, 2);
                USBRemove = new ManagementEventWatcher(scope, USBRemoveQuery);
                USBRemove.EventArrived += USBRemove_EventArrived;
                USBRemove.Start();
            }
            catch (Exception ex)
            {
                if (USBRemove != null)
                {
                    USBRemove.Stop();
                }
                throw ex;
            }
        }

        private void USBInsert_EventArrived(object sender, EventArrivedEventArgs e)
        {
            SerialPortList = new ObservableCollection<string>(SerialPortHelper.GetPortNames());
        }

        private void USBRemove_EventArrived(object sender, EventArrivedEventArgs e)
        {
            SerialPortList = new ObservableCollection<string>(SerialPortHelper.GetPortNames());
        }

        #endregion

        #region Commands

        private DelegateCommand _connnetCommand;
        public DelegateCommand ConnnetCommand =>
            _connnetCommand ?? (_connnetCommand = new DelegateCommand(() =>
            {
                switch (SelectedConnectType)
                {
                    case nameof(ConnectTypeEnum.串口):
                        _master = new SerialMaster(new SerialPortConfig
                        {
                            SerialProtName = SelectedSerialPort,
                            BaudRate = SelectedBaudRate.Value,
                            DataBits = SelectedDataBit,
                            StopBits = (StopBits)SelectedStopBit.Value,
                            Parity = (Parity)SelectedParity.Value,
                            FlowControl = (Handshake)SelectedFlowControl.Value,
                            BufferSize = BufferLength
                        });
                        StatusStr = $"{SelectedSerialPort} OPENED, {SelectedBaudRate.Value}, {SelectedDataBit}, {((StopBits)SelectedStopBit.Value).ToString()}, {((Parity)SelectedParity.Value).ToString()}";
                        break;
                    case nameof(ConnectTypeEnum.网口):
                        if (!IsTcpServer)
                        {
                            _master = new TcpClientMaster(HostIP, HostPort, BufferLength);
                            StatusStr = $"Connected to {HostIP}:{HostPort}";
                        }
                        else
                        {
                            _master = new TcpServerMaster(HostIP, HostPort, BufferLength);
                            StatusStr = "Waiting for connection to 127.0.0.1:6000";
                        }
                        break;
                }
                var result = _master.Connect();
                if (result)
                {
                    IsConnected = true;
                    StatusColor = Constants.Green;
                }
                else
                {
                    IsConnected = false;
                    StatusColor = Constants.Red;
                    StatusStr = Constants.Disconnected;
                }
            }));

        private DelegateCommand _sendCommand;
        public DelegateCommand SendCommand =>
            _sendCommand ?? (_sendCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrEmpty(InputText))
                    return;
                if (_master == null)
                {
                    MessageBox.Show("请连接串口助手", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var messageType = SendMessageType.FirstOrDefault(t => t.IsSelected)?.Name;
                var bytes = StringHelper.StringFormat(messageType, InputText.Trim(), false);
                _master.Send(bytes, SelectedClientName);
                _eventAggregator.GetEvent<SendDataEvent>().Publish(bytes);
            }));

        private DelegateCommand _disconnnetCommand;
        public DelegateCommand DisconnnetCommand =>
            _disconnnetCommand ?? (_disconnnetCommand = new DelegateCommand(() =>
            {
                Dispose();
            }));

        #endregion
    }
}
