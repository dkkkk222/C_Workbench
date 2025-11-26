using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Force.DeepCloner;
using Prism.Mvvm;
using Workbench.Utils;

namespace Workbench.Models.dw
{
    public class CategoryTree:BindableBase
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string AddressHex { get; set; }
        public string AddressDec { get; set; }
        public CategoryTree Parent { get; set; }
        public List<CategoryTree> Children { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isCheck=false;
        public bool IsCheck
        {
            get => _isCheck;
            set
            {
                SetProperty(ref _isCheck, value);               
            } 
        }
    }

    public class CategoryTreeType
    {
        public const string Type = "Type";
        public const string Category = "Category";
        public const string SubCategory = "SubCategory";
        public const string Register = "Register";

    }
}
