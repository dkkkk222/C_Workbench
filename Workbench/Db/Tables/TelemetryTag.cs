using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Workbench.Db.Tables
{
    [Table("t_TelemetryTag")]
    public class TelemetryTagTable
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("chip_id")]
        public string ChipId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}
