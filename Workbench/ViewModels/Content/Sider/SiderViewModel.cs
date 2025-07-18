using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using System.Windows.Forms;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.Consts;
using Workbench.Utils;
using Workbench.Views;
using Workbench.Views.Content.Sider;
using Workbench.Views.Windows;

namespace Workbench.ViewModels.Content.Sider
{
    public class SiderViewModel : BindableBase
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        public SiderViewModel(IEventAggregator eventAggregator, IDialogService dialogService, ProjectManager projectManager)
        {
            _dialogService = dialogService;
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            EventListener();
        }

        private void EventListener()
        {
            _eventAggregator.GetEvent<AddedProjectEvent>().Subscribe((project) =>
            {
                if (!Projects.Any(t => t.UID == project.UID))
                {
                    Projects.Add(project);
                    _projectManager.OpenedProjectList.Add(project);
                    if(_projectManager.CurrentProject==null)
                    {
                        //如果当前工程为Null，那么将新建的工程设为当前工程
                        _projectManager.CurrentProject = project;
                    }
                }
                UpdateHasProject();
            });

            _eventAggregator.GetEvent<RemoveProjectFromSiderEvent>().Subscribe((projectId) =>
            {
                RemoveProjectSide(projectId);
            });

            _eventAggregator.GetEvent<TabChangeEvent>().Subscribe((tabProject) =>
            {
                if (tabProject == null)
                    return;
                var project = Projects.FirstOrDefault(t => t.UID == tabProject.ProjectId);
                var page = UtilsFunc.FindNodeDfs(project, tabProject.UID);
                if (page != null)
                    page.IsSelected = true;
            });
        }

        public void RemoveProjectSide(string projectId)
        {
            var list = Projects.Where(t => t.UID != projectId).ToList();
            Projects.Clear();
            Projects.AddRange(list);
            if (_projectManager.CurrentProject != null && _projectManager.CurrentProject.UID == projectId)
            {
                _projectManager.CurrentProject = null;
                _projectManager.CurrentPPEC = null;
                _eventAggregator.GetEvent<TreeViewSelectedEvent>().Publish(string.Empty);
            }
            _projectManager.OpenedProjectList = _projectManager.OpenedProjectList.Where(t => t.UID != projectId).ToList();
            UpdateHasProject();
        }
        private void UpdateHasProject()
        {
            HasProject = Projects.Count > 0;
        }

        #region Command
        public DelegateCommand RemoveCurrentProjectCommand => new DelegateCommand(async () =>
        {
            if(RightSelectProject==null)
            {
                MessageBox.Show("请选择要移除的芯片!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }

            var resultSelect = MessageBox.Show("是否" + DelProjectName, "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(resultSelect!=System.Windows.Forms.DialogResult.Yes)
            {
                return;
            }
            await _projectManager.RemoveProject(RightSelectProject);
            UpdateHasProject();
            RightSelectProject = null;
        });

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

        private DelegateCommand _saveProjectCommand;
        public DelegateCommand SaveProjectCommand =>
            _saveProjectCommand ?? (_saveProjectCommand = new DelegateCommand(() =>
            {
                if(RightSelectProject==null&& _projectManager.CurrentProject==null)
                {
                    MessageBox.Show("请选择要保存的芯片!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
                    return;
                }
                var saveProject = RightSelectProject;
                if (saveProject == null)
                {
                    saveProject = _projectManager.CurrentProject;
                }

                var result = _projectManager.SaveProject(saveProject);

                if (result)
                    System.Windows.Forms.MessageBox.Show("芯片:"+saveProject.Name+",保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }));
        #endregion

        #region Properties

        private ObservableCollection<PpecProject> _projects = new ObservableCollection<PpecProject>();
        public ObservableCollection<PpecProject> Projects
        {
            get { return _projects; }
            set
            {
                SetProperty(ref _projects, value);
            }
        }
        private PpecProject _rightSelectProject = null;
        public PpecProject RightSelectProject
        {
            get { return _rightSelectProject; }
            set
            {
                SetProperty(ref _rightSelectProject, value);
            }
        }

        private bool _hasProject = false;
        public bool HasProject
        {
            get => _hasProject;
            set => SetProperty(ref _hasProject, value);
        }

        public string _delProjectName = ConstString.DelProjectNameDefault;
        public string DelProjectName
        {
            get => _delProjectName;
            set => SetProperty(ref _delProjectName, value);
        }

        public string _saveProjectName = ConstString.SaveProjectNameDefault;
        public string SaveProjectName
        {
            get => _saveProjectName;
            set => SetProperty(ref _saveProjectName, value);
        }
        #endregion
    }
}
