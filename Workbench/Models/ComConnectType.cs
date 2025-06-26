using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models.Enums;

namespace Workbench.Models
{
    public class ComConnectType : BindableBase
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private ComConnectEnum _val = default;

        public ComConnectEnum Value
        {
            get => _val;
            set => SetProperty(ref _val, value);
        }
    }
}
