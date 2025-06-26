using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models.Enums;

namespace Workbench.Models.BootLoader
{
    public class BBLLCCANItem : BindableBase
    {
        private string name = "";

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private BBLLCCANPort val = default;

        public BBLLCCANPort Value
        {
            get => val;
            set => SetProperty(ref val, value);
        }
    }
}
