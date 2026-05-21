using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System.Linq;
using Workbench.Views.Windows;
using Workbench.Views;
using Workbench.Utils;
using System.Collections.ObjectModel;
using Prism.Events;
using Workbench.Events;
using Workbench.Utils.Common;
using Workbench.Models;
using System.Windows.Forms;
using System.Threading.Tasks;
using System;
using Workbench.Communication;
using System.Device.I2c;
using System.Collections.Generic;
using HandyControl.Tools.Extension;
using System.Windows.Documents;

namespace Workbench.ViewModels.Content.ButtonBar
{
    public class ButtonBarViewModel : BindableBase
    {
        private readonly FileHandler _fileHandler;
        private readonly IDialogService _dialogService;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;

        public ButtonBarViewModel(IDialogService dialogService, ProjectManager projectManager, IEventAggregator eventAggregator, FileHandler fileHandler)
        {
            _fileHandler = fileHandler;
            _dialogService = dialogService;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            InitTheme();
            EventListeners();
            PortList.AddRange(SerialPortHelper.GetPortNames());
            _buandList.AddRange(new List<int>()
            {
                115200,
                9600,
                19200,
                38400,
                57600
            });
            SerialPortName = PortList.FirstOrDefault();
            BuandName = _buandList.FirstOrDefault();
            //CAN口默认值
            SelectedCAN = _cANList.FirstOrDefault();
            SelectedCANBaud = _cANBaudList.FirstOrDefault();
            SelectedCANType = _BBLLCCANTYPEList.FirstOrDefault();
            InitI2CBaud();
            SelectedCommunicationI2CDevice = CommunicationI2CDeviceList.FirstOrDefault();
            ChangeI2C();
        }
        public string _connectStr = Constants.ConnectStr;

        public string ConnectStr
        {
            get => _connectStr;
            set => SetProperty(ref _connectStr, value);
        }

        public string _connectIcon = Constants.ConnectIcon;

        public string ConnectIcon
        {
            get => _connectIcon;
            set => SetProperty(ref _connectIcon, value);
        }
        #region 连接属性初始化
        public void ChangeUart()
        {
            var ppec = _projectManager.GetCachePPEC();
            if (ppec != null)
            {
                ppec.PortName = SerialPortName;
                ppec.BuandName = BuandName;
            }
        }
        public void ChangeI2C()
        {

            //if (_projectManager != null && _projectManager.CurrentProject != null && _projectManager.CurrentProject.CommService != null && _projectManager.CurrentProject.CommService.IsConnected)

            if (_projectManager?.CurrentProject?.CommService?.IsConnected == true)
            {

            }
            else
            { 
                CommunicationI2CList.Clear();
                var devs = Ch347DeviceEnumerator.Enumerate(excludeMode3: false);
                foreach (var d in devs)
                {
                    CommunicationI2CList.Add(new BBLLCCANBAUDItem() { Value = 0, Name = d.ToString() });
                }
                SelectedCommunicationI2CType = CommunicationI2CList.FirstOrDefault();
            }


            var ppec = _projectManager.GetCachePPEC();
            if (ppec != null)
            {
                ppec.ConnectDeviceIndex = SelectedCommunicationI2CDevice;
                ppec.I2cBaud = (int)SelectedCommunicationI2CClock.Value;
                ppec.I2CSCL = SelectCommunicationI2CSCL == "禁止" ? "0" : "1";
                ppec.Delay = Delay;
            }
        }

        public void ChangeCan()
        {
            var ppec = _projectManager.GetCachePPEC();
            if (ppec != null)
            {
                ppec.SelectedCanId = (int)SelectedCANBaud.Value;
                switch ((int)SelectedCANType.Value)
                {
                    case 0:
                        ppec.DeviceType = 21;
                        break;
                    case 1:
                        ppec.DeviceType = 4;
                        break;
                }
                ppec.SelectedBaudIndex = (int)SelectedCANBaud.Value;
                ppec.CanDelay = CanDelay;
            }
        }
        #endregion

        public void InitI2CBaud()
        {
            _communicationI2CClock.AddRange(new List<BBLLCCANBAUDItem>()
            {
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.A, Name = "20kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.B, Name = "100 kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.C, Name = "400 kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.D, Name = "750 kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.E, Name = "50 kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.F, Name = "200 kHz" },
                new BBLLCCANBAUDItem { Value = BBLLCCANBaud.G, Name = "1M kHz" }
            });
            SelectedCommunicationI2CClock = CommunicationI2CClock[1];
        }
        private void EventListeners()
        {
            _eventAggregator.GetEvent<SerialPortAddRemoveEvent>().Subscribe(() =>
            {
                var portName = SerialPortName;
                PortList.Clear();
                PortList.AddRange(SerialPortHelper.GetPortNames());
                if (!string.IsNullOrEmpty(portName) && PortList.Contains(portName))
                    SerialPortName = portName;
                else
                    SerialPortName = PortList.FirstOrDefault();

                ChangeI2C();
                ChangeCan();
                ChangeUart();

            }, ThreadOption.UIThread);

            _eventAggregator.GetEvent<TreeViewSelectedEvent>().Subscribe((treeItemLevel) =>
            {
                IsSelectedProject = true;
                //显示按钮栏的同时，将PPEC的通讯方式和端口绑定到按钮栏
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    SelectedConnectType = string.IsNullOrEmpty(ppec.ConnectType) ? Constants.PIN_CONNECT : ppec.ConnectType;
                    SelectedCommunicationType = string.IsNullOrEmpty(ppec.CommunicationType) ? Constants.SERIAL_PORT : ppec.CommunicationType;
                   
                    if (ppec.CommunicationType == Constants.SERIAL_PORT || ppec.CommunicationType == Constants.OldSERIAL_PORT)
                    {
                        if (!string.IsNullOrEmpty(ppec.PortName))
                        {
                            SerialPortName = ppec.PortName;
                        }
                        if (ppec.BuandName > 0)
                        {
                            BuandName = ppec.BuandName;
                        }
                    }

                }
                //if (!string.IsNullOrEmpty(treeItemLevel) && treeItemLevel != ProjectLevel.Project)
                //{
                //    IsSelectedProject = true;
                //    //显示按钮栏的同时，将PPEC的通讯方式和端口绑定到按钮栏
                //    var ppec = _projectManager.GetCachePPEC();
                //    if (ppec != null)
                //    {
                //        SelectedCommunicationType = string.IsNullOrEmpty(ppec.CommunicationType) ? Constants.Modbus : ppec.CommunicationType;
                //        if (ppec.CommunicationType == Constants.Modbus && !string.IsNullOrEmpty(ppec.PortName))
                //            SerialPortName = ppec.PortName;
                //    }
                //}
                //else
                //    IsSelectedProject = false;
            }, ThreadOption.UIThread);

            _eventAggregator.GetEvent<CurrentPpecChangedEvent>().Subscribe((ppec) =>
            {
                if (ppec == null)
                    return;
                SerialPortName = ppec.PortName ?? PortList.FirstOrDefault();
            });

            _eventAggregator.GetEvent<ConnectEvent>().Subscribe(async () =>
            {
                await OnConnectionAsync();
            });
            _eventAggregator.GetEvent<CloseConnectEvent>().Subscribe(() =>
            {
                if (IsConnected)
                {
                    var ppec = _projectManager.GetCachePPEC();
                    ppec.Disconnect();
                    _projectManager.SetCurrentPpec(ppec);
                }
                ConnectIcon = Constants.ConnectIcon;
                ConnectStr = Constants.ConnectStr;
                IsConnected = false;

            });
        }

        #region Properties

        private bool _isDarkTheme = false;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                SetProperty(ref _isDarkTheme, value);
                UtilsFunc.ChangeTheme(value);
            }
        }
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private ObservableCollection<int> _buandList = new ObservableCollection<int>();
        public ObservableCollection<int> BuandList
        {
            get { return _buandList; }
            set { SetProperty(ref _buandList, value); }
        }

        private int _buandName;
        public int BuandName
        {
            get => _buandName;
            set
            {
                SetProperty(ref _buandName, value);
            }
        }

        private ObservableCollection<string> _portList = new ObservableCollection<string>();
        public ObservableCollection<string> PortList
        {
            get { return _portList; }
            set { SetProperty(ref _portList, value); }
        }

        private string _serialPortName;
        public string SerialPortName
        {
            get => _serialPortName;
            set
            {
                SetProperty(ref _serialPortName, value);
            }
        }

        private ObservableCollection<string> _connectTypeList = new ObservableCollection<string> { Constants.PIN_CONNECT, Constants.SYS_CONNECT};
        public ObservableCollection<string> ConnectTypeList
        {
            get => _connectTypeList;
            set => SetProperty(ref _connectTypeList, value);
        }

        private string _selectedConnectType = Constants.PIN_CONNECT;
        public string SelectedConnectType
        {
            get => _selectedConnectType;
            set
            {  
                if (value == Constants.PIN_CONNECT)
                {
                    CommunicationTypeList = new ObservableCollection<string> { Constants.SERIAL_PORT, Constants.CAN, Constants.I2C};                    
                }
                if (value == Constants.SYS_CONNECT)
                {
                    CommunicationTypeList = new ObservableCollection<string> { Constants.SERIAL_PORT, Constants.CAN };
                }
                SelectedCommunicationType = CommunicationTypeList[0];
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                    ppec.ConnectType = value;
                SetProperty(ref _selectedConnectType, value);
            }
        }


        private ObservableCollection<string> _communicationTypeList = new ObservableCollection<string> { Constants.SERIAL_PORT, Constants.CAN, Constants.I2C };//, Constants.Telemetry
        public ObservableCollection<string> CommunicationTypeList
        {
            get => _communicationTypeList;
            set => SetProperty(ref _communicationTypeList, value);
        }


        private ObservableCollection<string> _communicationI2CDeviceList = new ObservableCollection<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8" };
        public ObservableCollection<string> CommunicationI2CDeviceList
        {
            get => _communicationI2CDeviceList;
            set => SetProperty(ref _communicationI2CDeviceList, value);
        }
        private ObservableCollection<string> _communicationI2CSCL = new ObservableCollection<string> { "使能", "禁止" };
        public ObservableCollection<string> CommunicationI2CSCL
        {
            get => _communicationI2CSCL;
            set => SetProperty(ref _communicationI2CSCL, value);
        }

        private ObservableCollection<BBLLCCANBAUDItem> _communicationI2CList = new ObservableCollection<BBLLCCANBAUDItem>();
        public ObservableCollection<BBLLCCANBAUDItem> CommunicationI2CList
        {
            get => _communicationI2CList;
            set => SetProperty(ref _communicationI2CList, value);
        }

        private ObservableCollection<BBLLCCANBAUDItem> _communicationI2CClock = new ObservableCollection<BBLLCCANBAUDItem>();
        public ObservableCollection<BBLLCCANBAUDItem> CommunicationI2CClock
        {
            get => _communicationI2CClock;
            set => SetProperty(ref _communicationI2CClock, value);
        }
        private string _selectCommunicationI2CSCL = "禁止";
        public string SelectCommunicationI2CSCL
        {
            get => _selectCommunicationI2CSCL;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.I2CSCL = value == "禁止" ? "0" : "1";
                }
                SetProperty(ref _selectCommunicationI2CSCL, value);
            }
        }

        private string _selectedCommunicationI2CDevice = "";
        public string SelectedCommunicationI2CDevice
        {
            get => _selectedCommunicationI2CDevice;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.ConnectDeviceIndex = value;
                }
                SetProperty(ref _selectedCommunicationI2CDevice, value);
            }
        }

        private BBLLCCANBAUDItem _selectedCommunicationI2CClock;
        public BBLLCCANBAUDItem SelectedCommunicationI2CClock
        {
            get => _selectedCommunicationI2CClock;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    if (value == null)
                        return;
                    ppec.I2cBaud = (int)value.Value;
                }

                SetProperty(ref _selectedCommunicationI2CClock, value);
            }
        }
        private BBLLCCANBAUDItem _selectedCommunicationI2CType;
        public BBLLCCANBAUDItem SelectedCommunicationI2CType
        {
            get => _selectedCommunicationI2CType;
            set
            {
                if (_projectManager != null)
                {
                    var ppec = _projectManager.GetCachePPEC();
                    if (ppec != null)
                    {
                        if (value == null)
                            return;
                        ppec.I2cBusId = (int)value.Value;
                    }
                }


                SetProperty(ref _selectedCommunicationI2CType, value);
            }
        }
        public string _RegisterDelay = "10";
        public string RegisterDelay
        {
            get => _RegisterDelay;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.RegisterDelay = value;
                }

                SetProperty(ref _RegisterDelay, value);
            }
        }
        public string _Delay = "0";
        public string Delay
        {
            get => _Delay;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.Delay = value;
                }

                SetProperty(ref _Delay, value);
            }
        }

        public string _CanDelay = "5";
        public string CanDelay
        {
            get => _CanDelay;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.CanDelay = value;
                }

                SetProperty(ref _CanDelay, value);
            }
        }
        private string _selectedCommunicationType = Constants.SERIAL_PORT;
        public string SelectedCommunicationType
        {
            get => _selectedCommunicationType;
            set
            {
                IsModbusSelected = value == Constants.SERIAL_PORT;
                //PortTitle = value == Constants.SERIAL_PORT ? Constants.SERIAL_PORT : Constants.CAN_PORT;
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                    ppec.CommunicationType = value;
                if(SelectedConnectType == Constants.PIN_CONNECT)
                {
                    if (value == Constants.SERIAL_PORT || value == Constants.OldSERIAL_PORT)
                    {
                        ConnectType = 1;
                        PortTitle = Constants.SERIAL_PORT;
                        ChangeUart();
                    }
                    if (value == Constants.CAN)
                    {
                        ConnectType = 2; PortTitle = Constants.CAN_PORT;
                        ChangeCan();
                    }
                }
                else if (SelectedConnectType == Constants.SYS_CONNECT)
                {
                    if (value == Constants.SERIAL_PORT || value == Constants.OldSERIAL_PORT)
                    {
                        ConnectType = 1;
                        PortTitle = Constants.SERIAL_PORT;
                        ChangeUart();
                    }
                    if (value == Constants.CAN)
                    {
                        ConnectType = 2; PortTitle = Constants.CAN_PORT;
                        ChangeCan();
                    }
                }
               
                if (value == Constants.I2C)
                {
                    ConnectType = 3; PortTitle = Constants.I2C;
                    ChangeI2C();
                }
                //if (value == Constants.Telemetry)
                //{
                //    ConnectType = 1; PortTitle = Constants.Telemetry;
                //    ChangeUart();
                //}
                SetProperty(ref _selectedCommunicationType, value);
            }
        }

        private bool _isModbusSelected = true;
        public bool IsModbusSelected
        {
            get => _isModbusSelected;
            set => SetProperty(ref _isModbusSelected, value);
        }

        private int _connectType = 0;
        public int ConnectType
        {
            get => _connectType;
            set => SetProperty(ref _connectType, value);
        }

        private string _portTitle = Constants.SERIAL_PORT + "：";
        public string PortTitle
        {
            get => _portTitle;
            set
            {
                value = value + "：";
                SetProperty(ref _portTitle, value);
            }
        }

        private bool _isSelectedProject = false;
        public bool IsSelectedProject
        {
            get => _isSelectedProject;
            set => SetProperty(ref _isSelectedProject, value);
        }

        private ObservableCollection<BBLLCCANBAUDItem> _cANBaudList = CommEntity.BBLLCCANBAUDList;
        public ObservableCollection<BBLLCCANBAUDItem> CANBaudList
        {
            get => _cANBaudList;
            set => SetProperty(ref _cANBaudList, value);
        }

        private ObservableCollection<BBLLCCANBAUDItem> _BBLLCCANTYPEList = CommEntity.BBLLCCANTYPEList;
        public ObservableCollection<BBLLCCANBAUDItem> BBLLCCANTYPEList
        {
            get => _BBLLCCANTYPEList;
            set => SetProperty(ref _BBLLCCANTYPEList, value);
        }

        private BBLLCCANBAUDItem _selectedCANBaud;
        public BBLLCCANBAUDItem SelectedCANBaud
        {
            get => _selectedCANBaud;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                    ppec.SelectedBaudIndex = (int)value?.Value;
                SetProperty(ref _selectedCANBaud, value);
            }
        }

        private BBLLCCANBAUDItem _selectedCANType;
        public BBLLCCANBAUDItem SelectedCANType
        {
            get => _selectedCANType;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)

                {
                    switch ((int)value?.Value)
                    {
                        case 0:
                            ppec.DeviceType = 21;
                            break;
                        case 1:
                            ppec.DeviceType = 4;
                            break;
                    }
                }

                SetProperty(ref _selectedCANType, value);
            }
        }

        private ObservableCollection<BBLLCCANItem> _cANList = CommEntity.BBLLCCANList;
        public ObservableCollection<BBLLCCANItem> CANList
        {
            get => _cANList;
            set => SetProperty(ref _cANList, value);
        }

        private BBLLCCANItem _selectedCAN;
        public BBLLCCANItem SelectedCAN
        {
            get => _selectedCAN;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                    ppec.SelectedCanId = (int)value?.Value;
                SetProperty(ref _selectedCAN, value);
            }
        }

        #endregion

        #region Command
        private DelegateCommand _newProjectCommand;
        public DelegateCommand NewProjectCommand =>
            _newProjectCommand ?? (_newProjectCommand = new DelegateCommand(() =>
            {
                _dialogService.Show(nameof(CreateProjectView), new DialogParameters(), result =>
                {

                }, nameof(CreateProjectWindow));
            }));

        private DelegateCommand _openProjectCommand;
        public DelegateCommand OpenProjectCommand =>
            _openProjectCommand ?? (_openProjectCommand = new DelegateCommand(() =>
            {
                _projectManager.OpenProject();
            }));

        private DelegateCommand _saveProjectCommand;
        public DelegateCommand SaveProjectCommand =>
            _saveProjectCommand ?? (_saveProjectCommand = new DelegateCommand(() =>
            {
                var result = _projectManager.SaveProject(_projectManager.CurrentProject);
                if (result)
                    System.Windows.Forms.MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }));

        private DelegateCommand _saveAsProjectCommand;
        public DelegateCommand SaveAsProjectCommand =>
            _saveAsProjectCommand ?? (_saveAsProjectCommand = new DelegateCommand(() =>
            {
                var isSuc=_projectManager.SaveAsProject(_projectManager.CurrentProject);
                if (isSuc)
                    System.Windows.Forms.MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

        private DelegateCommand _disconnectCommand;
        public DelegateCommand DisconnectCommand =>
            _disconnectCommand ?? (_disconnectCommand = new DelegateCommand(async () =>
            {
                await CloseConnect();
            }));

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand =>
            _connectCommand ?? (_connectCommand = new DelegateCommand(async () =>
            {
                if (_projectManager.CurrentProject == null)
                {
                    MessageBox.Show("尚未选择工程");
                    return;
                }
                if (_projectManager.CurrentProject.IsConnecting)
                {
                    await CloseConnect();
                    ConnectIcon = Constants.ConnectIcon;
                    ConnectStr = Constants.ConnectStr;
                }
                else
                {
                    await OnConnectionAsync();
                    //ConnectIcon = Constants.DisConnectIcon;
                    //ConnectStr = Constants.DisConnectStr;
                }

            }));

        #endregion

        #region Methods

        public async Task CloseConnect()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Publish();
            await Task.Delay(200);
            var ppec = _projectManager.GetCachePPEC();
            ppec.Disconnect();
            _projectManager.SetCurrentPpec(ppec);
            IsConnected = false;
        }

        private void InitTheme()
        {
            IsDarkTheme = true;
        }

        private async Task OnConnectionAsync()
        {
            try
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec == null)
                {
                    MessageBox.Show("请选择工程后再连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ppec.PortName = SerialPortName;
                ppec.BuandName = BuandName;
                bool result = await ppec.ConnectAsync();
                IsConnected = result;
                if (IsConnected)
                {
                    ConnectIcon = Constants.DisConnectIcon;
                    ConnectStr = Constants.DisConnectStr;
                }
                _eventAggregator.GetEvent<OnConnctedEvent>().Publish();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
