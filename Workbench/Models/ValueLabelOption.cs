using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public class ValueLabelOption : BindableBase
    {
        private object _value;
        public object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }
    }
}
