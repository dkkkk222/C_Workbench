using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public sealed class Sample
    {
        public long TimestampUtcMs { get; set; }                 // 万能时间戳（DateTime.UtcNow.Ticks/TimeSpan）
        public Dictionary<string, string> Values { get; set; }   // 参数ID->数值
        public Dictionary<string, string> RawHex { get; set; }   // 如需保存原始HEX/位域，亦可加 Dictionary<string,string> RawHex 等
    }
}
