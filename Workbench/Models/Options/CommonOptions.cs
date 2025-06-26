using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Options
{
    public class CommonOptions : BindableBase
    {
        private int _value;

        public int Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _label;

        public string Label
        {
            get { return _label; }
            set { SetProperty(ref _label, value); }
        }
    }
}
