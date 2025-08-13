using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Workbench.Db.Tables
{
    [Table(Name = "t_frame")]
    public class FrameRow
    {
        [PrimaryKey, Identity, Column(Name = "frame_id")] 
        public int FrameId { get; set; }
        [Column(Name = "session_id"), NotNull] 
        public string SessionId { get; set; }
        [Column(Name = "ts_utc_ms"), NotNull] 
        public long TsUtcMs { get; set; }

    }
}
