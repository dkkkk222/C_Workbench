using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity;
using Workbench.Events;
using Workbench.Models;
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
        public ContentViewModel(IUnityContainer container, IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _container = container;
            _eventAggregator = eventAggregator;
            var homeViewModel = container.Resolve<HomeViewModel>();
            homeViewModel.Title = "首页";

            Documents = new ObservableCollection<AvaDocument>() { homeViewModel };
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
                    singleParamsViewModel.Title = treeNodeProject.Label;
                    singleParamsViewModel.IsActive = true;
                    singleParamsViewModel.ContentId = treeNodeProject.UID;
                    singleParamsViewModel.Project = treeNodeProject;
                    Documents.Add(singleParamsViewModel);
                    singleParamsViewModel.LoadData();
                }
                else if (treeNodeProject.Level == ProjectLevel.BatchParams)
                {
                    var batchParamsViewModel = _container.Resolve<BatchParamsViewModel>();
                    batchParamsViewModel.Title = treeNodeProject.Label;
                    batchParamsViewModel.IsActive = true;
                    batchParamsViewModel.ContentId = treeNodeProject.UID;
                    batchParamsViewModel.Project = treeNodeProject;
                    Documents.Add(batchParamsViewModel);
                    batchParamsViewModel.LoadData();
                }
                else if (treeNodeProject.Level == ProjectLevel.Debug)
                {
                    var debugViewModel = _container.Resolve<DebugViewModel>();
                    debugViewModel.Title = treeNodeProject.Label;
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

        private ObservableCollection<ContextMenuItem> _contextMenuItems = new ObservableCollection<ContextMenuItem>
        {
            new ContextMenuItem { Header = "关闭", IconText = "\xe653" },
            new ContextMenuItem { Header = "关闭其他", IconText = "\xe653" },
        };
    }
}
