using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Enums
{
    public enum UpdateTopoEnum
    {
        None,
        移相全桥 = 1,
        LLC,
        BuckBoost,
        DAB,
        单相逆变整流,
        三相逆变整流,
        维也纳整流,
        LC,
    }
}
