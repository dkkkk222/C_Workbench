using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public class FlatRow
    {
        public long FrameId;
        public long TsUtcMs;
        public string ParamId;
        public double? Val;
    }
}
