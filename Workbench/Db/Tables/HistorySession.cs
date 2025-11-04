using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Workbench.Db.Tables
{
    [Table(Name = "history_session")]
    public class HistorySession
    {
        [PrimaryKey, Column("session_id"), NotNull] public string SessionId { get; set; }
        [Column("started_at")] public string StartedAt { get; set; } // ISO8601
        [Column("ended_at")] public string EndedAt { get; set; }
    }

    [Table(Name = "history_frame")]
    public class HistoryFrame
    {
        [PrimaryKey(1), Column("session_id"), NotNull] public string SessionId { get; set; }
        [PrimaryKey(2), Column("ts_ticks")] public long TsTicks { get; set; } // DateTime.Ticks
        [PrimaryKey(3), Column("seq")] public int Seq { get; set; } // 本周期递增序号
    }
    [Table(Name = "param_dict")]
    public class ParamDict
    {
        [PrimaryKey, Identity, Column("param_id")] public int ParamId { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("type_code")] public int TypeCode { get; set; } // 0:double
    }
    [Table(Name = "history_value")]
    public class HistoryValue
    {
        [PrimaryKey(1), Column("session_id"), NotNull] public string SessionId { get; set; }
        [PrimaryKey(2), Column("ts_ticks")] public long TsTicks { get; set; }
        [PrimaryKey(3), Column("seq")] public int Seq { get; set; }
        [PrimaryKey(4), Column("param_id")] public int ParamId { get; set; }
        [Column("num_value"), Nullable] public double? NumValue { get; set; }
        // 如需文本：
        [Column("text_value"), Nullable]           public string   TextValue { get; set; }
    }
}
