using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Data
{
    public class PPEC_Data
    {
        public string Type { get; set; }
        public string Ppec { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public string ChipDesc { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

    }
}
