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
using Workbench.SerialAsistant;
using PPEC.Communication;
using Workbench.ViewModels.Pages;
using Workbench.Views.Pages;

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
            Exit += (sender, args) =>
            {
                //关闭node-red进程
                var cmd = Container.Resolve<CommandHandler>();
                cmd.CloseCommandProcess();
            };
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            _log.Info("Workbench started.");
            base.OnStartup(e);
        }

        protected override Window CreateShell()
        {
            CatchException();
            Container.Resolve<SerialBootStrapper>().OnStart();
            Container.Resolve<BootStrapper>().OnStart();
            var mainWindow = Container.Resolve<MainWindow>();
            var splashWindow = Container.Resolve<SplashWindow>();
            splashWindow.ShowDialog();
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
            containerRegistry.RegisterSingleton<ProjectManager>();
            containerRegistry.RegisterSingleton<FileHandler>();
            containerRegistry.RegisterSingleton<CommandHandler>();
            containerRegistry.Register<SerialBootStrapper>();
        }

        private void RegisterViewAndViewModel(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ParamSettingView, ParamSettingViewModel>();
        }

        private void RegisterDialog(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialogWindow<CreateProjectWindow>(nameof(CreateProjectWindow));
            containerRegistry.RegisterDialog<CreateProjectView, CreateProjectViewModel>();
            containerRegistry.RegisterDialogWindow<RenameWindow>(nameof(RenameWindow));
            containerRegistry.RegisterDialog<RenameView, RenameViewModel>();
            containerRegistry.RegisterDialogWindow<RecentFileWindow>(nameof(RecentFileWindow));
            containerRegistry.RegisterDialog<RecentFileView, RecentFileViewModel>();
            containerRegistry.RegisterDialogWindow<NodeRedWindow>(nameof(NodeRedWindow));
            containerRegistry.RegisterDialog<NodeRedView, NodeRedViewModel>();
            containerRegistry.RegisterDialogWindow<UserManualWindow>(nameof(UserManualWindow));
            containerRegistry.RegisterDialog<UserManualView, UserManualViewModel>();
            containerRegistry.RegisterDialogWindow<BootLoaderWindow>(nameof(BootLoaderWindow));
            containerRegistry.RegisterDialog<BootLoaderView, BootLoaderViewModel>();
            containerRegistry.RegisterDialogWindow<AboutWindow>(nameof(AboutWindow));
            containerRegistry.RegisterDialog<AboutView, AboutViewModel>();
            containerRegistry.RegisterDialogWindow<PowerToolWindow>(nameof(PowerToolWindow));
            containerRegistry.RegisterDialog<PowerToolView, PowerToolViewModel>();
            containerRegistry.RegisterDialogWindow<GlobalSettingWindow>(nameof(GlobalSettingWindow));
            containerRegistry.RegisterDialog<GlobalSettingView, GlobalSettingViewModel>();
            containerRegistry.RegisterDialogWindow<PasswordWindow>(nameof(PasswordWindow));
            containerRegistry.RegisterDialog<PasswordView, PasswordViewModel>();
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
