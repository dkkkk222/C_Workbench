using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Views.Content.Sider;

namespace Workbench.ViewModels.Content.Sider
{
    public class SiderViewModel : BindableBase
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        public SiderViewModel(IEventAggregator eventAggregator, ProjectManager projectManager)
        {
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
                }
            });

            _eventAggregator.GetEvent<RemoveProjectFromSiderEvent>().Subscribe((projectId) =>
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

            });

            _eventAggregator.GetEvent<TabChangeEvent>().Subscribe((tabProject) =>
            {
                if (tabProject == null)
                    return;
                var project = Projects.FirstOrDefault(t => t.UID == tabProject.ProjectId);
                var ppec = project.Children.FirstOrDefault(t => t.UID == tabProject.PPEC_Id);
                ppec.Children.FirstOrDefault(t => t.UID == tabProject.UID).IsSelected = true;
            });
        }

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

        #endregion
    }
}
