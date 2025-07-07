using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;

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
        }

        #region Properties

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
        }

        #endregion

        private DelegateCommand _closeCommand;

        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

    }
}
