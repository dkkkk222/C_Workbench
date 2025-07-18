using log4net;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Utils.Common;
using Workbench.ViewModels.Content.Sider;

namespace Workbench.Views.Content.Sider
{
    /// <summary>
    /// SiderView.xaml 的交互逻辑
    /// </summary>
    public partial class SiderView : UserControl
    {
        private readonly ProjectManager _projectManager;
        private readonly FileHandler _fileHandler;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        public SiderView(IEventAggregator eventAggregator, FileHandler fileHandler, ProjectManager projectManager, IDialogService dialogService)
        {
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            InitializeComponent();

        }

        private void TreeView_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var seletectedItem = treeView.SelectedItem as PpecProject;
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as PpecProject;
            if (nodeData == null) return;

            if (nodeData.Level == ProjectLevel.Project)
                ShowProjectContextMenu(nodeData);
            else if (nodeData.Level == ProjectLevel.PPEC)
                ShowPPECContextMenu(nodeData);
        }

        private void ShowPPECContextMenu(PpecProject nodeData)
        {
        }

        public void MoveItemUp<T>(ObservableCollection<T> list, int index)
        {
            // 只有当index大于0时元素才能向上移动
            if (index > 0 && index < list.Count)
            {
                T item = list[index];
                list.RemoveAt(index); // 移除当前位置的元素
                list.Insert(index - 1, item); // 在上一个位置重新插入该元素
            }
        }

        public void MoveItemDown<T>(ObservableCollection<T> list, int index)
        {
            // 只有当index小于最高索引时元素才能向下移动
            if (index >= 0 && index < list.Count - 1)
            {
                T item = list[index];
                list.RemoveAt(index); // 移除当前位置的元素
                list.Insert(index + 1, item); // 在下一个位置重新插入该元素
            }
        }

        private void ShowProjectContextMenu(PpecProject nodeData)
        {
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var seletectedItem = treeView.SelectedItem as PpecProject;
            if (seletectedItem == null) return;

            var viewModel = DataContext as SiderViewModel;
            var selectProject = seletectedItem.ProjectId==null? seletectedItem:viewModel.Projects.FirstOrDefault(t => t.UID == seletectedItem.ProjectId);

            if (_projectManager.CurrentProject!=null&&_projectManager.CurrentProject.IsConnecting&& _projectManager.CurrentProject?.UID!= selectProject.UID)
            {
                var result=MessageBox.Show("切换芯片后必须断开原有芯片连接,请确认是否断开!","确认",MessageBoxButton.YesNo,MessageBoxImage.Question);
                if(result==MessageBoxResult.Yes)
                {
                    var closeResult=AsyncDisConnect().GetAwaiter();
                    closeResult.OnCompleted(() =>
                    {
                        ChangeProject(seletectedItem, viewModel);
                    });
                }
                else
                {
                    return;
                }
            }
            else
            {
                ChangeProject(seletectedItem, viewModel);
            }
            
        }
        private void ChangeProject(PpecProject seletectedItem, SiderViewModel viewModel)
        {
            if (seletectedItem.Level == ProjectLevel.Project)
            {
                _projectManager.CurrentProject = seletectedItem;
                _projectManager.CurrentPPEC = null;
            }
            else if (seletectedItem.Level == ProjectLevel.PPEC)
            {
                _projectManager.CurrentPPEC = seletectedItem;
                _projectManager.CurrentProject = viewModel.Projects.FirstOrDefault(t => t.UID == seletectedItem.ProjectId);
            }
            else
            {
                _projectManager.CurrentProject = viewModel.Projects.FirstOrDefault(t => t.UID == seletectedItem.ProjectId);
                _projectManager.CurrentPPEC = _projectManager.CurrentProject.Children.FirstOrDefault(t => t.UID == seletectedItem.PPEC_Id);
            }
            _eventAggregator.GetEvent<TreeViewSelectedEvent>().Publish(seletectedItem.Level);
        }
        private async Task AsyncDisConnect()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Publish();
            await Task.Delay(200);
            _projectManager.CurrentProject.Disconnect();
        }

        private void treeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var seletectedItem = treeView.SelectedItem as PpecProject;
            if (seletectedItem == null)
                return;
            if (seletectedItem.Level == ProjectLevel.Project || seletectedItem.Level == ProjectLevel.PPEC)
                return;

            _eventAggregator.GetEvent<DoubleClickTreeNodeEvent>().Publish(seletectedItem);
        }
    }
}
