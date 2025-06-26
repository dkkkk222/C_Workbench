using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Pages
{
    public class ControlConfig
    {
        public string ControlElementName { get; set; }
        public string Type { get; set; }
        public List<ValueName> Options { get; set; } = new List<ValueName>();
    }
}
