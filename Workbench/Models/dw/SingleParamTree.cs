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
        public List<SingleParamTree> Children { get; set; }
    }
}
