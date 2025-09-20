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
using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using System.Reflection;
using Workbench.Db;
using System.Linq;
using AutoMapper;
using Workbench.Profiles;
using Workbench.Db.IService;
using Workbench.Db.Service;
using Workbench.ViewModels.dw;
using Workbench.Views.dw;

namespace Workbench
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        static App() // 最早：在 new App() 之前执行
        {
            HookGlobalExceptions();
        }
        private static void HookGlobalExceptions()
        {
            AppDomain.CurrentDomain.FirstChanceException += (s, e) => _log.Info("FirstChance" + e.Exception);

            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                var ex = ev.ExceptionObject as Exception;
                var text = ex?.ToString() ?? ev.ExceptionObject?.ToString() ?? "<null>";
                _log.Info("AppDomain=" + text);               
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                _log.Info("TaskScheduler" + ev.Exception);
                ev.SetObserved();
            };

            // 注意：如果你在这里引用 Application.Current，确保它已存在；否则建议在 OnStartup 里再挂
            // Application.Current.DispatcherUnhandledException += ... 也可以在 OnStartup 挂
        }
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));
        protected override void OnStartup(StartupEventArgs e)
        {
            HotLoadDatabase();
            RegisteFluentMigrator();
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            _log.Info("Workbench started.");
            base.OnStartup(e);          
        }
        static void LogFatal(string src, Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText("crash.log",
                    $"{DateTime.Now:u} [{src}] {ex}\r\n");
            }
            catch { /* ignore */ }
        }
        private void HotLoadDatabase()
        {
            try
            {
                //预热数据库
                using (var db = new DbContext())
                {
                    var check = db.Registers.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void RegisteFluentMigrator()
        {
            using (var serviceProvider = CreateServices())
            using (var scope = serviceProvider.CreateScope())
            {
                // Put the database update into a scope to ensure
                // that all resources will be disposed.
                UpdateDatabase(scope.ServiceProvider);
            }
        }

        private static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            // Instantiate the runner
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        private static ServiceProvider CreateServices()
        {
            return new ServiceCollection()
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    // Add SQLite support to FluentMigrator
                    .AddSQLite()
                    // Set the connection string
                    .WithGlobalConnectionString("Data Source=database.sqlite")
                    // Define the assembly containing the migrations, maintenance migrations and other customizations
                    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                // Build the service provider
                .BuildServiceProvider(false);
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
            //containerRegistry.RegisterSingleton<WatchViewModel>();
            containerRegistry.RegisterSingleton<ICpService, CpService>();
            //containerRegistry.RegisterSingleton<SmlsContext>(() => new SmlsContext($@"Data Source={AppDomain.CurrentDomain.BaseDirectory}smls_vision.db;Version=3"));
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<RegisterProfile>();
            });
            containerRegistry.RegisterInstance(configuration.CreateMapper());
        }

        private void RegisterViewAndViewModel(IContainerRegistry containerRegistry)
        {
        }

        private void RegisterDialog(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialogWindow<CreateProjectWindow>(nameof(CreateProjectWindow));
            containerRegistry.RegisterDialog<WatchChartListView, WatchChartListViewModel>();
            containerRegistry.RegisterDialog<WatchTableListView, WatchTableListViewModel>();
            containerRegistry.RegisterDialogWindow<ChipManagerWindow>(nameof(ChipManagerWindow));
            containerRegistry.RegisterDialogWindow<ShowChartListWindows>(nameof(ShowChartListWindows));
            containerRegistry.RegisterDialogWindow<ShowTableListWindows>(nameof(ShowTableListWindows)); 
            containerRegistry.RegisterDialog<CreateProjectView, CreateProjectViewModel>();
            containerRegistry.RegisterSingleton<ChipManagerViewModel>();
            containerRegistry.RegisterDialog<ChipManagerView, ChipManagerViewModel>();
            containerRegistry.RegisterDialogWindow<RecentFileWindow>(nameof(RecentFileWindow));
            containerRegistry.RegisterDialog<RecentFileView, RecentFileViewModel>();
            containerRegistry.RegisterDialogWindow<RenameWindow>(nameof(RenameWindow));
            containerRegistry.RegisterDialogWindow<PassWordWindow>(nameof(PassWordWindow));
            containerRegistry.RegisterDialog<RenameView, RenameViewModel>();
            containerRegistry.RegisterDialog<PassWordView, PassWordViewModel>();
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
