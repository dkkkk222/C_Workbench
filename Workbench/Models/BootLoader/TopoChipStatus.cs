using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.BootLoader
{
    public class TopoChipStatus
    {
        public TopoChipStatus(int version, CurrentChipStateEnum ccse)
        {
            Version = version;
            CurrentChipState = ccse;
        }

        public int Version { get; set; }

        public CurrentChipStateEnum CurrentChipState { get; set; }
    }

    public enum CurrentChipStateEnum
    {
        None,
        App,
        Boot
    }
}
