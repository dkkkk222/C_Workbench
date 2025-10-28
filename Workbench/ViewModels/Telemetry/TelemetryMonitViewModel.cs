using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Workbench.Models;
using Workbench.Utils;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryMonitViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        public TelemetryMonitViewModel(IEventAggregator eventAggregator, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
        }

        private DelegateCommand _closeCommand;
        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {

            }));

        public override void LoadData()
        { }
    }
}
