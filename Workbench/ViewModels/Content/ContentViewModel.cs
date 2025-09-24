using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Utils.Common;
using Workbench.ViewModels.Content.Tabs;
using Workbench.ViewModels.dw;
using Workbench.Views.Content.Tabs;

namespace Workbench.ViewModels.Content
{
    public class ContentViewModel : BindableBase
    {
        private readonly IUnityContainer _container;
        private readonly IEventAggregator _eventAggregator;
        public ProjectManager _projectManager;
        public ContentViewModel(IUnityContainer container, IEventAggregator eventAggregator, IRegionManager regionManager, ProjectManager projectManager)
        {
            _container = container;
            _eventAggregator = eventAggregator;
            _projectManager = projectManager;

            //var homeViewModel = _container.Resolve<HomeViewModel>();
            //homeViewModel.Title = "首页";
            Documents = new ObservableCollection<AvaDocument>();
            //Documents.Add(homeViewModel);
            EventLisen();
        }

        private void EventLisen()
        {
            _eventAggregator.GetEvent<DoubleClickTreeNodeEvent>().Subscribe((treeNodeProject) =>
            {
                var tab = Documents.FirstOrDefault(t => t.ContentId == treeNodeProject.UID);
                if (tab != null)
                {
                    tab.IsActive = true;
                    return;
                }

                if (treeNodeProject.Level == ProjectLevel.SingleParams)
                {
                    var singleParamsViewModel = _container.Resolve<SingleParamsViewModel>();
                    singleParamsViewModel.Title = getTabLabel(treeNodeProject);
                    singleParamsViewModel.IsActive = true;
                    singleParamsViewModel.ContentId = treeNodeProject.UID;
                    singleParamsViewModel.Project = treeNodeProject;
                    Documents.Add(singleParamsViewModel);
                    singleParamsViewModel.LoadData();
                }
                else if (treeNodeProject.Level == ProjectLevel.BatchParams)
                {
                    var batchParamsViewModel = _container.Resolve<BatchParamsViewModel>();
                    batchParamsViewModel.Title = getTabLabel(treeNodeProject);
                    batchParamsViewModel.IsActive = true;
                    batchParamsViewModel.ContentId = treeNodeProject.UID;
                    batchParamsViewModel.Project = treeNodeProject;
                    Documents.Add(batchParamsViewModel);
                    batchParamsViewModel.LoadData();
                }
                else if (treeNodeProject.Level == ProjectLevel.Debug)
                {
                    var debugViewModel = _container.Resolve<WatchViewModel>();
                    debugViewModel.Title = getTabLabel(treeNodeProject);
                    debugViewModel.IsActive = true;
                    debugViewModel.ContentId = treeNodeProject.UID;
                    debugViewModel.Project = treeNodeProject;
                    Documents.Add(debugViewModel);
                    debugViewModel.LoadData();
                }
            });
            _eventAggregator.GetEvent<CloseTabEvent>().Subscribe((contentId) =>
            {
                var doc = Documents.FirstOrDefault(t => t.ContentId == contentId);
                var index = Documents.IndexOf(doc);
                Documents.RemoveAt(index);
            });
            _eventAggregator.GetEvent<RemovePpecEvent>().Subscribe((ppecId) =>
            {
                //删除对应PPEC的Tab
                var removedList = Documents.Where(t => t.Project != null && t.Project.PPEC_Id == ppecId).ToList();
                foreach (var item in removedList)
                {
                    Documents.Remove(item);
                }
            });
            _eventAggregator.GetEvent<ShowHomePageEvent>().Subscribe(() =>
            {
                var isHave = Documents.Where(x => x.Title == "首页").FirstOrDefault();
                if(isHave==null)
                {
                    var homeViewModel = _container.Resolve<HomeViewModel>();
                    homeViewModel.Title = "首页";
                    homeViewModel.IsActive = true;
                    homeViewModel.ContentId = Guid.NewGuid().ToString();
                    Documents.Add(homeViewModel);
                }
                else
                {
                    isHave.IsActive = true;
                }
                
            });
        }

        private string getTabLabel(PpecProject treeNodeProject)
        {
            var project = _projectManager.OpenedProjectList.FirstOrDefault(t => t.UID == treeNodeProject.ProjectId);
            return $"{treeNodeProject.Label}-{project.Label}";
        }

        private ObservableCollection<AvaDocument> _documents;

        public ObservableCollection<AvaDocument> Documents
        {
            get { return _documents; }
            set { SetProperty(ref _documents, value); }
        }

        private AvaDocument _activeDocument;

        public AvaDocument ActiveDocument
        {
            get { return _activeDocument; }
            set
            {
                SetProperty(ref _activeDocument, value);
                _eventAggregator.GetEvent<TabChangeEvent>().Publish(value?.Project);
            }
        }
        public async Task AsyncDisConnect()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Publish();
            await Task.Delay(200);
            _projectManager.CurrentProject.Disconnect();
        }
        private ObservableCollection<ContextMenuItem> _contextMenuItems = new ObservableCollection<ContextMenuItem>
        {
            new ContextMenuItem { Header = "关闭", IconText = "\xe653" },
            new ContextMenuItem { Header = "关闭其他", IconText = "\xe653" },
        };
    }
}
