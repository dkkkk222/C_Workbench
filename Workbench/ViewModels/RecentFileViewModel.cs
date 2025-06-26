using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Utils;

namespace Workbench.ViewModels
{
    public class RecentFileViewModel : BindableBase, IDialogAware
    {
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        public RecentFileViewModel(FileHandler fileHandler, ProjectManager projectManager)
        {
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            LoadData();
        }

        private void LoadData()
        {
            RecentFiles.Clear();
            RecentFiles.AddRange(_fileHandler.GetRecentFiles());
        }

        private ObservableCollection<RecentFile> _recentFiles = new ObservableCollection<RecentFile>();
        public ObservableCollection<RecentFile> RecentFiles
        {
            get { return _recentFiles; }
            set { SetProperty(ref _recentFiles, value); }
        }

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private DelegateCommand<RecentFile> _rowDoubleClickCommand;
        public DelegateCommand<RecentFile> RowDoubleClickCommand =>
            _rowDoubleClickCommand ?? (_rowDoubleClickCommand = new DelegateCommand<RecentFile>((recentFile) =>
            {
                _projectManager.OpenRecentFile(recentFile);
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            }));
    }
}
