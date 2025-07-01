using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Management;
using System.Windows;
using Workbench.Utils.Common;
using Workbench.Utils;
using Prism.Events;
using Workbench.Events;
using log4net;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Workbench.Models;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Common;
using PPEC.Communication.DB;
using Prism.Ioc;

namespace Workbench.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private MainServices MainServices;
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindowViewModel));
        public string ExcelPath;
        public MainWindowViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator, FileHandler fileHandler, ProjectManager projectManager)
        {
            MainServices = containerProvider.Resolve<MainServices>();
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            CreateUSBWatcher();
            InitVersionHub();

            //新协议通讯--wzw-625
            //TestMain();
            //EXCEL解析--wzw--626
            //RegisterExcelParser rep=new RegisterExcelParser();
            //rep.Parse(ExcelPath);
            //WZW--627
            InitDataList();
        }

        public async void InitDataList()
        {
            await InitDataModelService.Instance.InitChipList(MainServices.ChipService);
        }
        static async Task TestMain()
        {
            var devU = new GroundDevice("COM3", ConnectPortType.UART,115200);

            await Task.WhenAll(devU.ConnectAsync());

            /* 并发测试 */
            var t1 = devU.ReadRegAsync(0x0001);
            await Task.WhenAll(t1);

            Console.WriteLine("全部完成");
        }

        #region Propertis

        private readonly string upgradeFileName = "upgrade.json";
        private readonly string upgradeProcedure = "procedure.json";
        public Upgrade upgradeLocal;
        public Upgrade upgradeRemote;
        public StaticUpgrade currentProInfo;
        public string _host;
        private bool updateBadge = false;
        /// <summary>
        /// 更新提醒红点显示数量，NULL为不显示，1为1个，以此类推
        /// </summary>
        public bool UpdateBadge
        {
            get => updateBadge;
            set => SetProperty(ref updateBadge, value);
        }

        private bool _hasPpec = false;
        public bool HasPpec
        {
            get => _hasPpec;
            set => SetProperty(ref _hasPpec, value);
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private string _connectName = Constants.Connect;
        public string ConnectName
        {
            get => _connectName;
            set => SetProperty(ref _connectName, value);
        }


        #endregion

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
            _eventAggregator.GetEvent<SerialPortAddRemoveEvent>().Publish();
            //上电重连
            UtilsFunc.PowerOnEvent();
        }

        private void USBRemove_EventArrived(object sender, EventArrivedEventArgs e)
        {
            _eventAggregator.GetEvent<SerialPortAddRemoveEvent>().Publish();
            //串口连接断电或者拔出时，需要关闭串口连接
            UtilsFunc.PowerOffEvent();
        }

        #endregion

        private bool _isMaximized = false;
        public bool IsMaximized
        {
            get => _isMaximized;
            set => SetProperty(ref _isMaximized, value);
        }
        internal void OnChangedState(WindowState windowState)
        {
            IsMaximized = windowState == WindowState.Maximized;
        }

        #region Commands

        private DelegateCommand _upgradeCommand;
        public DelegateCommand UpgradeCommand =>
            _upgradeCommand ?? (_upgradeCommand = new DelegateCommand(() =>
            {
                //找到启动器路径，然后启动启动器，关闭本程序
                try
                {
                    var result = CheckUpdaterVersion().GetAwaiter();
                    result.OnCompleted(() =>
                    {
                        if (result.GetResult())
                        {
                            var callback = System.Windows.MessageBox.Show("是否开始更新?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                            if (callback == MessageBoxResult.Yes)
                            {
                                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;//当前程序目录
                                DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory).Parent;
                                string targetFolderPath = Path.Combine(directoryInfo.FullName, upgradeProcedure);
                                currentProInfo.IsDirectUpdate = "true";
                                UtilsFunc.UpdateLocalJson(targetFolderPath, currentProInfo);

                                var StartUpName = _fileHandler.ReadLocalFileObject<AppConfig>(Constants.CONFIG_FILE_PATH)?.StartUpName;
                                ProcessStartInfo startInfo = new ProcessStartInfo();
                                startInfo.WorkingDirectory = directoryInfo.FullName;//设置工作目录
                                startInfo.FileName = string.IsNullOrEmpty(StartUpName) ? "initiating.exe" : StartUpName;
                                startInfo.UseShellExecute = true;
                                startInfo.Verb = "runas";
                                Process.Start(startInfo);
                                //关闭本程序
                                Process.GetCurrentProcess().Kill();
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("当前版本已是最新!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });

                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    Process.GetCurrentProcess().Kill();
                }
            }));

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand =>
            _connectCommand ?? (_connectCommand = new DelegateCommand(async () =>
            {
                if (_projectManager.CurrentPPEC == null)
                {
                    MessageBox.Show("请选择工程", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                _eventAggregator.GetEvent<ConnectEvent>().Publish();
            }));

        #endregion

        #region Methods

        public async Task<bool> CheckUpdaterVersion()
        {
            if (upgradeLocal != null)
            {
                _host = upgradeLocal.Host;
                upgradeRemote = await UpdateHelper.GetUpdaterRemoteInfo(_host, upgradeFileName);
                if (upgradeRemote != null)
                {
                    //版本比较
                    int res = 0;
                    res = UpdateHelper.Compare(upgradeRemote.Workbench.Version, upgradeLocal.Workbench.Version);
                    if (res > 0)
                    {
                        //需要update更新程序
                        return true;
                    }
                }
            }
            else
            {
                string HostUrl = _fileHandler.ReadLocalFileObject<AppConfig>(Constants.CONFIG_FILE_PATH)?.HostUrl;
                _host = HostUrl;
                upgradeRemote = await UpdateHelper.GetUpdaterRemoteInfo(_host, upgradeFileName);
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;//当前程序目录
                string targetFolderPath = Path.Combine(currentDirectory, "upgrade.json");//版本文件
                using (StreamWriter writer = new StreamWriter(targetFolderPath))
                {
                    var content = JsonConvert.SerializeObject(upgradeRemote);
                    writer.WriteLine(content);
                }
            }

            return false;
        }

        private void InitVersionHub()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;//当前程序目录
            DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory).Parent;
            string targetFolderPath = Path.Combine(directoryInfo.FullName, upgradeProcedure);//需要启动的程序所在文件夹
            currentProInfo = UpdateHelper.GetLocalInfo<StaticUpgrade>(targetFolderPath);
            upgradeLocal = UpdateHelper.GetLocalInfo<Upgrade>(upgradeFileName);
            var result = CheckUpdaterVersion().GetAwaiter();
            result.OnCompleted(() =>
            {
                if (result.GetResult())
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateBadge = true;
                    });

                }
                else
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateBadge = false;
                    });
                }
            });
        }

        #endregion
    }
}
