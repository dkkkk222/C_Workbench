using log4net;
using Microsoft.Web.WebView2.Core;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.Data;
using Workbench.Utils;
using Workbench.Utils.Common;
using Workbench.ViewModels.Content.Sider;
using Workbench.Views.Windows;

namespace Workbench.Views.Content.Sider
{
    /// <summary>
    /// SiderView.xaml 的交互逻辑
    /// </summary>
    public partial class SiderView : UserControl
    {
        private static List<PPEC_Data> _data = new List<PPEC_Data>();
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
            var seletectedItem = treeView.SelectedItem as PPEC_Project;
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as PPEC_Project;
            if (nodeData == null) return;

            if (!_data.Any())
                _data = _fileHandler.ReadLocalFile<PPEC_Data>("Data/PPEC_Data.json");

            if (nodeData.Level == ProjectLevel.Project)
                ShowProjectContextMenu(nodeData);
            else if (nodeData.Level == ProjectLevel.PPEC)
                ShowPPECContextMenu(nodeData);
        }

        private void ShowPPECContextMenu(PPEC_Project nodeData)
        {
            ContextMenu menu = new ContextMenu();
            menu.FontSize = 14;

            //移除PPEC
            MenuItem deleteItem = new MenuItem();
            deleteItem.Header = "移除PPEC";
            deleteItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            deleteItem.Icon = TextManager.CreateIconFont("\xe633");
            deleteItem.Click += (sender, args) =>
            {
                var viewModel = DataContext as SiderViewModel;
                var project = viewModel.Projects.FirstOrDefault(t => t.UID == nodeData.ProjectId);
                var removedPPEC = project.Children.FirstOrDefault(t => t.UID == nodeData.UID);
                project.Children.Remove(removedPPEC);
                _eventAggregator.GetEvent<RemovePpecEvent>().Publish(nodeData.UID);
            };
            menu.Items.Add(deleteItem);

            //重命名
            MenuItem renameItem = new MenuItem();
            renameItem.Header = "重命名";
            renameItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            renameItem.Icon = TextManager.CreateIconFont("\xe632");
            renameItem.Click += (sender, args) =>
            {
                var oldName = nodeData.Name;
                _dialogService.Show(nameof(RenameView), new DialogParameters(), result =>
                {
                    var name = result.Parameters.GetValue<string>("Name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        nodeData.Name = name;
                        var project = _projectManager.OpenedProjectList.FirstOrDefault(t => t.UID == nodeData.ProjectId);
                        _projectManager.SaveProject(project);
                    }

                }, nameof(RenameWindow));
            };
            menu.Items.Add(renameItem);

            //向上移动
            MenuItem moveUpItem = new MenuItem();
            moveUpItem.Header = "向上移动";
            moveUpItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            moveUpItem.Icon = TextManager.CreateIconFont("\xe6e3");
            moveUpItem.Click += (sender, args) =>
            {
                var viewModel = DataContext as SiderViewModel;
                var projects = viewModel.Projects;
                var children = projects.FirstOrDefault(t => t.UID == nodeData.ProjectId)?.Children;
                var index = children.IndexOf(nodeData);
                MoveItemUp<PPEC_Project>(children, index);
            };
            menu.Items.Add(moveUpItem);

            //向下移动
            MenuItem moveDownItem = new MenuItem();
            moveDownItem.Header = "向下移动";
            moveDownItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            moveDownItem.Icon = TextManager.CreateIconFont("\xe728");
            moveDownItem.Click += (sender, args) =>
            {
                var viewModel = DataContext as SiderViewModel;
                var projects = viewModel.Projects;
                var children = projects.FirstOrDefault(t => t.UID == nodeData.ProjectId)?.Children;
                var index = children.IndexOf(nodeData);
                MoveItemDown<PPEC_Project>(children, index);
            };
            menu.Items.Add(moveDownItem);

            menu.IsOpen = true;
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

        private void ShowProjectContextMenu(PPEC_Project nodeData)
        {
            ContextMenu menu = new ContextMenu();
            menu.FontSize = 14;

            //新增PPEC
            MenuItem addItem = new MenuItem();
            addItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            addItem.Header = "新增PPEC";
            var ppecList = _data.Select(t => t.Ppec).Distinct().OrderBy(t => t).ToList();
            foreach (var ppec in ppecList)
            {
                var mi = new MenuItem() { Header = ppec };
                mi.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
                mi.Icon = TextManager.CreateIconFont("\xe7a9");
                mi.Click += (sender, args) =>
                {
                    var ppecNode = _projectManager.CreatePPEC(ppec, nodeData.UID);
                    var viewModel = DataContext as SiderViewModel;
                    viewModel.Projects.FirstOrDefault(t => t.UID == nodeData.UID)?.Children.Add(ppecNode);
                };
                addItem.Items.Add(mi);
            }
            addItem.Icon = TextManager.CreateIconFont("\xe7a9");
            menu.Items.Add(addItem);

            //新增拓扑
            MenuItem addTopoitem = new MenuItem();
            addTopoitem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            addTopoitem.Header = "新增拓扑";
            foreach (var topo in _data)
            {
                var mi = new MenuItem() { Header = topo.Title };
                mi.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
                mi.Icon = TextManager.CreateIconFont("\xef4a");
                mi.Click += (sender, args) =>
                {
                    var ppecNode = _projectManager.CreatePPEC(topo.Ppec, nodeData.UID);
                    var viewModel = DataContext as SiderViewModel;
                    viewModel.Projects.FirstOrDefault(t => t.UID == nodeData.UID)?.Children.Add(ppecNode);
                };
                addTopoitem.Items.Add(mi);
            }
            addTopoitem.Icon = TextManager.CreateIconFont("\xef4a");
            menu.Items.Add(addTopoitem);

            menu.Items.Add(new Separator());

            //移除
            MenuItem deleteItem = new MenuItem();
            deleteItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            deleteItem.Header = "移除工程";
            deleteItem.Icon = TextManager.CreateIconFont("\xe634");
            deleteItem.Click += (sender, args) =>
            {
                _eventAggregator.GetEvent<RemoveProjectFromSiderEvent>().Publish(nodeData.UID);
            };
            menu.Items.Add(deleteItem);

            //重命名
            MenuItem renameItem = new MenuItem();
            renameItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            renameItem.Header = "重命名";
            renameItem.Icon = TextManager.CreateIconFont("\xe632");
            renameItem.Click += (sender, args) =>
            {
                var oldName = nodeData.Name;
                _dialogService.Show(nameof(RenameView), new DialogParameters(), result =>
                {
                    var name = result.Parameters.GetValue<string>("Name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        nodeData.Name = name;
                        _projectManager.RenameProject(nodeData, oldName);
                    }

                }, nameof(RenameWindow));
            };
            menu.Items.Add(renameItem);

            //保存
            MenuItem saveItem = new MenuItem();
            saveItem.Header = "保存";
            saveItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            saveItem.Icon = TextManager.CreateIconFont("\xe60c");
            saveItem.Click += (sender, args) =>
            {
                _projectManager.SaveProject(nodeData);
            };
            menu.Items.Add(saveItem);

            //另存为
            MenuItem saveAsItem = new MenuItem();
            saveAsItem.SetResourceReference(MenuItem.ForegroundProperty, "TitleBarColor");
            saveAsItem.Header = "另存为";
            saveAsItem.Icon = TextManager.CreateIconFont("\xe624");
            saveAsItem.Click += (sender, args) =>
            {
                _projectManager.SaveAsProject(nodeData);
            };
            menu.Items.Add(saveAsItem);

            // 根据需要添加更多项
            menu.IsOpen = true;
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var seletectedItem = treeView.SelectedItem as PPEC_Project;
            if (seletectedItem == null) return;
            if (seletectedItem.Level == ProjectLevel.Project)
            {
                _projectManager.CurrentProject = seletectedItem;
                _projectManager.CurrentPPEC = null;
            }
            else if (seletectedItem.Level == ProjectLevel.PPEC)
            {
                _projectManager.CurrentPPEC = seletectedItem;
                var viewModel = DataContext as SiderViewModel;
                _projectManager.CurrentProject = viewModel.Projects.FirstOrDefault(t => t.UID == seletectedItem.ProjectId);
            }
            else
            {
                var viewModel = DataContext as SiderViewModel;
                _projectManager.CurrentProject = viewModel.Projects.FirstOrDefault(t => t.UID == seletectedItem.ProjectId);
                _projectManager.CurrentPPEC = _projectManager.CurrentProject.Children.FirstOrDefault(t => t.UID == seletectedItem.PPEC_Id);
            }
            _eventAggregator.GetEvent<TreeViewSelectedEvent>().Publish(seletectedItem.Level);
        }

        private void treeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var seletectedItem = treeView.SelectedItem as PPEC_Project;
            if (seletectedItem == null)
                return;
            if (seletectedItem.Level == ProjectLevel.Project || seletectedItem.Level == ProjectLevel.PPEC)
                return;
            _eventAggregator.GetEvent<DoubleClickTreeNodeEvent>().Publish(seletectedItem);
        }
    }
}
