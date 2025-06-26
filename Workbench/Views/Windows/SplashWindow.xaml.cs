
using Common.Controls;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Workbench.Events;
using Workbench.ViewModels;

namespace Workbench.Views.Windows
{
    /// <summary>
    /// SplashWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SplashWindow
    {
        private MainWindow _mainWindow;
        private IEventAggregator _eventAggregator;
        private readonly IContainerProvider _containerProvider;
        public SplashWindow(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            _containerProvider = containerProvider;
            _eventAggregator = eventAggregator;
            InitializeComponent();
            DataContext = new SplashWindowViewModel();
            DelayedExecution();
        }

        private async void DelayedExecution()
        {
            // 创建一个定时器
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                this.Close(); // 关闭当前窗口
            };
            timer.Start();
        }

        private void ShowAndCenterMainWindow()
        {
            _mainWindow.WindowState = WindowState.Normal;
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var windowWidth = _mainWindow.Width;
            var windowHeight = _mainWindow.Height;
            _mainWindow.Left = (screenWidth / 2) - (windowWidth / 2);
            _mainWindow.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
