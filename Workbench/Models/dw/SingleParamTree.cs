using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.dw
{
    public class SingleParamTree
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public List<SingleParamTree> Children { get; set; }
    }

    public class SingleParamTreeType
    {
        public const string Category = "Category";
        public const string SubCategory = "SubCategory";
        public const string Register = "Register";

    }
}
