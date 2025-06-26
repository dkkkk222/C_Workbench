using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant.Models
{
    public class SerialCommBoxItems : BindableBase
    {
        private string name = "";

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private int val = default;

        public int Value
        {
            get => val;
            set => SetProperty(ref val, value);
        }
    }
}
