using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.Data;
using Workbench.Utils;
using Workbench.ViewModels.Pages;

namespace Workbench.ViewModels.Content.Tabs
{
    public class DevelopViewModel : AvaDocument
    {
        private readonly FileHandler _fileHandler;
        public readonly IRegionManager _regionManager;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, BindableBase> _dic;
        public DevelopViewModel(FileHandler fileHandler, IEventAggregator eventAggregator, ProjectManager projectManager, IRegionManager regionManager)
        {
            _fileHandler = fileHandler;
            _regionManager = regionManager;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;

            _dic = new Dictionary<string, BindableBase>()
            {
                { "ParamSettingView", new ParamSettingViewModel() },
            };
        }

        #region Properties

        private ObservableCollection<PageMapping> _menus = new ObservableCollection<PageMapping>();
        public ObservableCollection<PageMapping> Menus
        {
            get { return _menus; }
            set { SetProperty(ref _menus, value); }
        }

        private BindableBase _developRightViewModel;
        public BindableBase DevelopRightViewModel
        {
            get { return _developRightViewModel; }
            set { SetProperty(ref _developRightViewModel, value); }
        }

        #endregion


        #region Method

        public override void LoadData()
        {
            var pageMapping = _fileHandler.ReadLocalFile<PageMapping>("Data/PageMapping.json");
            var project = this.Project;
            if (pageMapping != null && project != null)
            {
                //从打开的工程列表中获取当前页面对应的PPEC
                var ppec = _projectManager.GetPpecById(this.Project.ProjectId, this.Project.PPEC_Id);
                var mapping = pageMapping.FirstOrDefault(t => t.PPEC == ppec.Label);
                if (mapping == null)
                    return;
                DevelopRightViewModel = _dic[mapping.ParamSettingPage];
            }
        }

        #endregion

        private DelegateCommand _closeCommand;

        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

    }
}
