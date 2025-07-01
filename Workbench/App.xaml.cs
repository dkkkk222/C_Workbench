using log4net.Config;
using log4net;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Workbench.Utils;
using Workbench.ViewModels;
using Workbench.Views;
using Workbench.Views.Content;
using Workbench.Views.Content.ButtonBar;
using Workbench.Views.Content.Sider;
using Workbench.Views.Windows;
using PPEC.Communication.DB;
using PPEC.Communication.DB.Provided;
using PPEC.Communication.Interface.DB;

namespace Workbench
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));
        protected override void OnStartup(StartupEventArgs e)
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            _log.Info("Workbench started.");
            base.OnStartup(e);
        }

        protected override Window CreateShell()
        {
            CatchException();
            var mainWindow = Container.Resolve<MainWindow>();
            return mainWindow;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterDialog(containerRegistry);
            RegisterViewAndViewModel(containerRegistry);
            containerRegistry.RegisterSingleton<IChipService, ChipService>();
            containerRegistry.RegisterSingleton<ProjectManager>();
            containerRegistry.RegisterSingleton<FileHandler>();
            containerRegistry.RegisterSingleton<MainServices>();
            containerRegistry.RegisterSingleton<SmlsContext>(() => new SmlsContext($@"Data Source={AppDomain.CurrentDomain.BaseDirectory}smls_vision.db;Version=3"));
        }

        private void RegisterViewAndViewModel(IContainerRegistry containerRegistry)
        {
        }

        private void RegisterDialog(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialogWindow<CreateProjectWindow>(nameof(CreateProjectWindow));
            containerRegistry.RegisterDialogWindow<ChipManagerWindow>(nameof(ChipManagerWindow)); 
            containerRegistry.RegisterDialog<CreateProjectView, CreateProjectViewModel>();
            containerRegistry.RegisterSingleton<ChipManagerViewModel>();
            containerRegistry.RegisterDialog<ChipManagerView, ChipManagerViewModel>();
            containerRegistry.RegisterDialogWindow<RecentFileWindow>(nameof(RecentFileWindow));
            containerRegistry.RegisterDialog<RecentFileView, RecentFileViewModel>();
        }

        private void CatchException()
        {
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                _log.Error(args.Exception.Message);
                args.SetObserved();
            };
            //UI线程未捕获异常处理事件（UI主线程）
            DispatcherUnhandledException += (sender, args) =>
            {
                _log.Error(args.Exception.Message);
                args.Handled = true;
            };
            //Thead，处理在非UI线程上未处理的异常,当前域未处理异常
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                _log.Error(args.ExceptionObject.ToString());
            };
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ModuleCatalog().AddModule<ContentModule>()
                .AddModule<ButtonBarModule>()
                .AddModule<SiderModule>();
        }
    }
}
