using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Enum
{
    public enum RegisterLimit
    {
        READ = 1,
        WRITE = 1 << 1,
        PROTECT = 1 << 2,
        ALL = READ | WRITE
    }
}
