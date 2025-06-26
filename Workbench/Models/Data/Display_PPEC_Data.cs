using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Data
{
    public class Display_PPEC_Data
    {
        public string Type { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string PPEC { get; set; }
        public string Content { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
