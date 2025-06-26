using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Pages
{
    public class ParamSettingGroup
    {
        public string Title { get; set; }
        public List<ParamSettingElement> Elements { get; set; } = new List<ParamSettingElement>();
    }
}
