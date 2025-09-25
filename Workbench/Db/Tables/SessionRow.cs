using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Workbench.Db.Tables
{
    [Table(Name = "t_WatchHistory")]
    public class ValueRow
    {
        [Column(Name = "frame_id"), NotNull] 
        public int FrameId { get; set; }
        [Column(Name = "param_id"), NotNull] 
        public string ParamId { get; set; }

        [Column(Name = "resultParse"), Nullable]
        public string ResultParse { get; set; }
        [Column(Name = "result"), Nullable]
        public string Result { get; set; }
    }
}
