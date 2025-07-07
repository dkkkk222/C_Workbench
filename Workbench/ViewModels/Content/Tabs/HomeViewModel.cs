using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Views;
using Workbench.Views.Windows;

namespace Workbench.ViewModels.Content.Tabs
{
    public class HomeViewModel : AvaDocument
    {
        private FileHandler _fileHandler;
        private readonly IDialogService _dialogService;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;

        public HomeViewModel(FileHandler fileHandler, IDialogService dialogService, IEventAggregator eventAggregator, ProjectManager projectManager)
        {
            _fileHandler = fileHandler;
            _dialogService = dialogService;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            LoadIntroduceData();
            LoadRecentFiles();

            _eventAggregator.GetEvent<AddedProjectEvent>().Subscribe((project) =>
            {
                LoadRecentFiles();
            });

            //刷新最近文件列表
            _eventAggregator.GetEvent<RefreshRecentFileEvent>().Subscribe(() =>
            {
                LoadRecentFiles();
            });
        }

        private void LoadRecentFiles()
        {
            RecentFiles.Clear();
            var recentFiles = _fileHandler.GetRecentFiles();
            foreach (var file in recentFiles.OrderByDescending(t => t.DateTime).Take(5))
            {
                RecentFiles.Add(file);
            }
        }

        private void LoadIntroduceData()
        {
        }

        #region property

        private ObservableCollection<RecentFile> _recentFiles = new ObservableCollection<RecentFile>();
        public ObservableCollection<RecentFile> RecentFiles
        {
            get { return _recentFiles; }
            set
            {
                SetProperty(ref _recentFiles, value);
            }
        }

        #endregion

        #region Command

        private DelegateCommand<string> _openWebPageCommand;
        public DelegateCommand<string> OpenWebPageCommand =>
            _openWebPageCommand ?? (_openWebPageCommand = new DelegateCommand<string>((href) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = href,
                    UseShellExecute = true
                });
            }));

        private DelegateCommand _newProjectCommand;
        public DelegateCommand NewProjectCommand =>
            _newProjectCommand ?? (_newProjectCommand = new DelegateCommand(() =>
            {
                _dialogService.Show(nameof(CreateProjectView), new DialogParameters(), result =>
                {
                }, nameof(CreateProjectWindow));
            }));

        private DelegateCommand _openProjectCommand;
        public DelegateCommand OpenProjectCommand =>
            _openProjectCommand ?? (_openProjectCommand = new DelegateCommand(() =>
            {
                _projectManager.OpenProject();
            }));

        private DelegateCommand<RecentFile> _openRecentFileCommand;
        public DelegateCommand<RecentFile> OpenRecentFileCommand =>
            _openRecentFileCommand ?? (_openRecentFileCommand = new DelegateCommand<RecentFile>((recentFile) =>
            {
                _projectManager.OpenRecentFile(recentFile);
            }));

        private DelegateCommand _showRecentFileViewCommand;
        public DelegateCommand ShowRecentFileViewCommand =>
            _showRecentFileViewCommand ?? (_showRecentFileViewCommand = new DelegateCommand(() =>
            {
                _dialogService.Show(nameof(RecentFileView), new DialogParameters(), result =>
                {

                }, nameof(RecentFileWindow));
            }));

        private DelegateCommand _closeCommand;

        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        #endregion

        public override void LoadData()
        {
        }
    }
}
