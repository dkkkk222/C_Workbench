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
        public int ParamIdInt;
        public string Val;
        public double? NumValue; // 数值型；若你有 TextValue，可自己扩展
        public long TsTicks;     // 排序/分页使用
    }
    public sealed class FlatRowSeq
    {
        public long TsTicks;
        public int Seq;       // 用作分页游标替代 frame_id
        public int ParamId;
        public double? NumValue;
    }
}
