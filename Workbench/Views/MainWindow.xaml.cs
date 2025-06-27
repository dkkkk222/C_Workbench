using Common.Controls;
using log4net;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shell;
using Workbench.Events;
using Workbench.Models;
using Workbench.SerialAsistant.Views;
using Workbench.SerialAsistant.Windows;
using Workbench.Utils;
using Workbench.Utils.Common;
using Workbench.ViewModels;
using Workbench.Views.Windows;

namespace Workbench.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class MainWindow
    {
        private readonly CommandHandler _cmd;
        private readonly IDialogService _dialogService;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));
        private static bool _isMaximized = false;

        public MainWindow(IEventAggregator eventAggregator, IDialogService dialogService, ProjectManager projectManager, CommandHandler cmd)
        {
            new WindowResizer2(this);
            _cmd = cmd;
            _eventAggregator = eventAggregator;
            _projectManager = projectManager;
            _dialogService = dialogService;
            InitializeComponent();
            StateChanged += (sender, args) =>
            {
                var viewModel = DataContext as MainWindowViewModel;
                var mainWindow = sender as MainWindow;
                viewModel.OnChangedState(mainWindow.WindowState);
            };

            EventListen();
        }

        private void EventListen()
        {
            _eventAggregator.GetEvent<TreeViewSelectedEvent>().Subscribe((treeItemLevel) =>
            {
                var currentProject = _projectManager.CurrentProject;
                var currentPPEC = _projectManager.CurrentPPEC;
                if (currentProject != null)
                {
                    BottomBarPanel.Visibility = Visibility.Visible;
                    ProjectName.Text = currentProject.Name;
                    if (currentPPEC == null)
                    {
                        ChipName.Text = string.Empty;
                        ChipPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ChipName.Text = currentPPEC.Name;
                        ChipPanel.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    BottomBarPanel.Visibility = Visibility.Collapsed;

                }
            });
        }

        #region 窗体控制

        /// <summary>
        /// 最小化窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minimum_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        /// <summary>
        /// 关闭应用程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 最大化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Maximum_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        #endregion
        #region 菜单按钮事件

        /// <summary>
        /// 新建
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _dialogService.Show(nameof(CreateProjectView), new DialogParameters(), result =>
            {

            }, nameof(CreateProjectWindow));
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _projectManager.OpenProject();
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _projectManager.SaveProject(_projectManager.CurrentProject);
        }

        /// <summary>
        /// 另存为
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _projectManager.SaveAsProject(_projectManager.CurrentProject);
        }

        /// <summary>
        /// 用户手册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserManual_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _dialogService.ShowDialog(nameof(UserManualView), new DialogParameters(), result => { }, nameof(UserManualWindow));
        }

        /// <summary>
        /// 工程菜单打开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            //判断当前选中PPEC是否已连接
            var currentPPEC = _projectManager.CurrentPPEC;
            var viewModel = DataContext as MainWindowViewModel;
            viewModel.HasPpec = currentPPEC != null;
            var cachePPEC = _projectManager.GetCachePPEC();
            if (cachePPEC == null)
            {
                viewModel.IsConnected = false;
                viewModel.ConnectName = Constants.Connect;
            }
            else
            {
                viewModel.IsConnected = cachePPEC.IsTrueConnected;
                viewModel.ConnectName = cachePPEC.IsTrueConnected ? Constants.Disconnect : Constants.Connect;
            }
        }

        #endregion

        private async void NodeRed_Click(object sender, RoutedEventArgs e)
        {
            var nodeRedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "node-red-template-embedded-master");
            if (Directory.Exists(nodeRedPath))
            {
                var output = _cmd.ExecuteCommandAysnc(new List<string>() {
                                        $"cd {nodeRedPath}",
                                        "npm run start"
                                    });
            }

            var message = await _cmd.ExecuteCommandAysnc(new List<string> { "netstat -ano | findStr :1880" });
            _dialogService.ShowDialog(nameof(NodeRedView), new DialogParameters(), result =>
            {
            }, nameof(NodeRedWindow));
        }

        private void SerialAssistant_Click(object sender, RoutedEventArgs e)
        {
            _dialogService.ShowDialog(nameof(MainView), new DialogParameters(), result =>
            {
            }, nameof(MainWindow));
        }
        private void BootLoader_Click(object sender, RoutedEventArgs e)
        {
            _dialogService.ShowDialog(nameof(BootLoaderView), new DialogParameters(), result =>
            {
            }, nameof(BootLoaderWindow));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            _dialogService.ShowDialog(nameof(AboutView), new DialogParameters(), result =>
            {
            }, nameof(AboutWindow));
        }

        private void PowerTool_Click(object sender, RoutedEventArgs e)
        {
            _dialogService.ShowDialog(nameof(PowerToolView), new DialogParameters(), result =>
            {
            }, nameof(PowerToolWindow));
        }

        private void GlobalSetting_Click(object sender, RoutedEventArgs e)
        {
            _dialogService.ShowDialog(nameof(GlobalSettingView), new DialogParameters(), result =>
            {
            }, nameof(GlobalSettingWindow));
        }

        private void ShowChipCommand_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _dialogService.Show(nameof(ChipManagerView), new DialogParameters(), result =>
            {

            }, nameof(ChipManagerWindow));
        }
    }

}
