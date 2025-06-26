using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public class RecentFile
    {
        public string UID { get; set; }
        public string FileName { get; set; }
        public string DirPath { get; set; }
        public DateTime DateTime { get; set; }
        public string DateTimeStr { get; set; }

    }
}
