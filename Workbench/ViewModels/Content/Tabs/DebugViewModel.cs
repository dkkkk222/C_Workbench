using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Events;
using Workbench.Models;

namespace Workbench.ViewModels.Content.Tabs
{
    public class DebugViewModel : AvaDocument
    {
        private readonly IEventAggregator _eventAggregator;

        public DebugViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        private DelegateCommand _closeCommand;

        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        public override void LoadData()
        {
        }
    }
}
