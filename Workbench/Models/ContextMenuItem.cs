using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public class ContextMenuItem : BindableBase
    {
        private string _header;
        public string Header
        {
            get { return _header; }
            set { SetProperty(ref _header, value); }
        }

        private string _iconText;
        public string IconText
        {
            get { return _iconText; }
            set { SetProperty(ref _iconText, value); }
        }
    }
}
