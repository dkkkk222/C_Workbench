using Prism.Mvvm;
using System;
using System.Management;
using System.Windows;
using Workbench.Utils;
using Prism.Events;
using Workbench.Events;
using log4net;
using System.Threading.Tasks;
using Workbench.Models;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.DB;
using Prism.Ioc;
using Workbench.Views.Windows;
using System.IO;
using Prism.Commands;
using Workbench.Communication;

namespace Workbench.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private UserManualWindow _userManualWindow;
        private MainServices MainServices;
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindowViewModel));
        public string ExcelPath;
        public MainWindowViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator, FileHandler fileHandler, ProjectManager projectManager)
        {
            //MainServices = containerProvider.Resolve<MainServices>();
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            CreateUSBWatcher();

            //新协议通讯--wzw-625
            //TestMain();
            //EXCEL解析--wzw--626
            //RegisterExcelParser rep=new RegisterExcelParser();
            //rep.Parse(ExcelPath);
            //WZW--627
            InitDataList();
            //TestI2C();

        }

        public void UserOrAutoSaveProject()
        { 
            foreach(var pro in _projectManager.OpenedProjectList)
            {
                _projectManager.SaveProject(pro);
            }
        }
        public async void InitDataList()
        {
            await InitDataStaticService.Instance.GetChipType();
        }
        static async Task TestMain()
        {
            var devU = new GroundDevice("COM3", ConnectPortType.UART, 115200);

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
            //UtilsFunc.PowerOnEvent();
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
        private DelegateCommand _ShowCHMDocumentCommand;
        public DelegateCommand ShowCHMDocumentCommand =>
            _ShowCHMDocumentCommand ?? (_ShowCHMDocumentCommand = new DelegateCommand(ShowCHMDocument));
        //private DelegateCommand _connectCommand;
        //public DelegateCommand ConnectCommand =>
        //    _connectCommand ?? (_connectCommand = new DelegateCommand(async () =>
        //    {
        //        if (_projectManager.CurrentPPEC == null)
        //        {
        //            MessageBox.Show("请选择工程", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //            return;
        //        }
        //        _eventAggregator.GetEvent<ConnectEvent>().Publish();
        //    }));

        #endregion

        #region Methods
        public void ShowCHMDocument()
        {
            if (_userManualWindow == null)
                _userManualWindow = new UserManualWindow();
            var userManualName = "workbench_user_manual";
            var path = $"{AppDomain.CurrentDomain.BaseDirectory}Resource\\{userManualName}.pdf";
            if (!File.Exists(path))
            {
                MessageBox.Show($"未找到【Workbench用户手册】，请下载最新安装包并重新安装程序", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_userManualWindow.IsVisible)
            {
                if (_userManualWindow.WindowState == WindowState.Minimized)
                {
                    _userManualWindow.WindowState = WindowState.Normal;
                }
                _userManualWindow.Activate();
            }
            else
            {
                _userManualWindow = new UserManualWindow();
                _userManualWindow.Show();
            }
        }
        #endregion
    }
}
