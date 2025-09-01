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

        private ObservableCollection<string> _communicationTypeList = new ObservableCollection<string> { Constants.SERIAL_PORT, Constants.CAN };
        public ObservableCollection<string> CommunicationTypeList
        {
            get => _communicationTypeList;
            set => SetProperty(ref _communicationTypeList, value);
        }

        private string _selectedCommunicationType = Constants.SERIAL_PORT;
        public string SelectedCommunicationType
        {
            get => _selectedCommunicationType;
            set
            {
                IsModbusSelected = value == Constants.SERIAL_PORT;
                PortTitle = value == Constants.SERIAL_PORT ? Constants.SERIAL_PORT : Constants.CAN_PORT;
                var ppec = _projectManager.GetCachePPEC();
                if (ppec != null)
                    ppec.CommunicationType = value;
                SetProperty(ref _selectedCommunicationType, value);
            }
        }

        private bool _isModbusSelected = true;
        public bool IsModbusSelected
        {
            get => _isModbusSelected;
            set => SetProperty(ref _isModbusSelected, value);
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
