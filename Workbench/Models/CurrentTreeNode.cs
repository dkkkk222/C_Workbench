using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public class CurrentTreeNode
    {
        public PPEC_Project CurrentProject { get; set; }
        public PPEC_Project CurrentPPEC { get; set; }
        public List<string> OpenedProjectUidList { get; set; }

    }
}
