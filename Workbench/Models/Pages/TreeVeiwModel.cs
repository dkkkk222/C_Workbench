using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Pages
{
    public class TreeVeiwModel : BindableBase
    {
        public string Name { get; set; }

        public bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        public List<TreeVeiwModel> Children { get; set; } = new List<TreeVeiwModel>();
    }
}
