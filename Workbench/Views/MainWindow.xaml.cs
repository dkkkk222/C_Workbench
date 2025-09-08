using log4net;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Windows;
using Workbench.Events;
using Workbench.Utils;
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

        #endregion

        private void ShowChipPDFCommand_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void ShowChipCommand_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _dialogService.Show(nameof(ChipManagerView), new DialogParameters(), result =>
            {

            }, nameof(ChipManagerWindow));
        }

        private void NoTitleBarWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Publish();
            var viewModel = DataContext as MainWindowViewModel;
            var r = MessageBox.Show("是否保存工程？", "提示",
                           MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                viewModel.UserOrAutoSaveProject();
            }
            var ppec = _projectManager.GetCachePPEC();
            ppec.Disconnect();
            _projectManager.SetCurrentPpec(ppec);
        }
    }

}
