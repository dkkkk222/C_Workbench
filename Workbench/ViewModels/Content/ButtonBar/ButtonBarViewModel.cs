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
            SerialPortName = PortList.FirstOrDefault();

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
        public void ChangeI2C()
        {
            CommunicationI2CList.Clear();
            var devs = Ch347DeviceEnumerator.Enumerate(excludeMode3: false);
            foreach (var d in devs)
            {
                CommunicationI2CList.Add(new BBLLCCANBAUDItem() {Value=0,Name= d.ToString() });
            }
            SelectedCommunicationI2CType= CommunicationI2CList.FirstOrDefault();
        }

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

            }, ThreadOption.UIThread);

            _eventAggregator.GetEvent<TreeViewSelectedEvent>().Subscribe((treeItemLevel) =>
            {
                IsSelectedProject = true;
                //显示按钮栏的同时，将PPEC的通讯方式和端口绑定到按钮栏
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    SelectedCommunicationType = string.IsNullOrEmpty(ppec.CommunicationType) ? Constants.SERIAL_PORT : ppec.CommunicationType;
                    if (ppec.CommunicationType == Constants.SERIAL_PORT && !string.IsNullOrEmpty(ppec.PortName))
                        SerialPortName = ppec.PortName;
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
                if(IsConnected)
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

        private ObservableCollection<string> _communicationTypeList = new ObservableCollection<string> { Constants.SERIAL_PORT, Constants.CAN, Constants.I2C };
        public ObservableCollection<string> CommunicationTypeList
        {
            get => _communicationTypeList;
            set => SetProperty(ref _communicationTypeList, value);
        }


        private ObservableCollection<string> _communicationI2CDeviceList = new ObservableCollection<string> { "0", "1","2", "3", "4", "5", "6", "7", "8" };
        public ObservableCollection<string> CommunicationI2CDeviceList
        {
            get => _communicationI2CDeviceList;
            set => SetProperty(ref _communicationI2CDeviceList, value);
        }
        private ObservableCollection<string> _communicationI2CSCL = new ObservableCollection<string> {"Enable", "Disable" };
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
        private string _selectCommunicationI2CSCL = "Disable";
        public string SelectCommunicationI2CSCL
        {
            get => _selectCommunicationI2CSCL;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    ppec.I2CSCL = value=="Disable"?"0":"1";
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
        private BBLLCCANBAUDItem _selectedCommunicationI2CType ;
        public BBLLCCANBAUDItem SelectedCommunicationI2CType
        {
            get => _selectedCommunicationI2CType;
            set
            {
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                {
                    if (value == null)
                        return;
                    ppec.I2cBusId = (int)value.Value;
                }
               
                SetProperty(ref _selectedCommunicationI2CType, value);
            } 
        }
        public string _Delay="0";
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
                if(value== Constants.SERIAL_PORT)
                {
                    ConnectType = 1;
                    PortTitle = Constants.SERIAL_PORT;
                }
                if (value == Constants.CAN)
                {
                    ConnectType = 2; PortTitle = Constants.CAN_PORT;
                }
                if (value == Constants.I2C)
                {
                    ConnectType = 3; PortTitle = Constants.I2C;
                }
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
                    switch((int)value?.Value)
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
                _projectManager.SaveAsProject(_projectManager.CurrentProject);
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
                if(_projectManager.CurrentProject.IsConnecting)
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
                if(ppec==null)
                {
                    MessageBox.Show("请选择工程后再连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ppec.PortName = SerialPortName;
                bool result = await ppec.ConnectAsync();
                IsConnected = result;
                if (IsConnected)
                {
                    ConnectIcon = Constants.DisConnectIcon;
                    ConnectStr = Constants.DisConnectStr;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
