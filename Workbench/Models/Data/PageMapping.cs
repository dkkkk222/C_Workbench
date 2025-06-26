using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Data
{
    public class PageMapping : BindableBase
    {
        public string PPEC { get; set; }
        public string ParamSettingPage { get; set; }
        public string DebugPage { get; set; }
    }
}
