using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.dw
{
    public class CategoryTree
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string AddressHex { get; set; }
        public string AddressDec { get; set; }
        public List<CategoryTree> Children { get; set; }
    }

    public class CategoryTreeType
    {
        public const string Category = "Category";
        public const string SubCategory = "SubCategory";
        public const string Register = "Register";

    }
}
