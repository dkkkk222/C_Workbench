using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Db.Tables
{
    [Table("t_register_bit_option")]
    public class RegisterBitOption
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("register_bit_id", CanBeNull = false)]
        public string RegisterBitId { get; set; }

        [Column("value", CanBeNull = false)]
        public uint Value { get; set; }

        [Column("display", CanBeNull = false)]
        public string Display { get; set; }

        [Column("key", CanBeNull = false)]
        public string Key { get; set; }

        [Column("label", CanBeNull = true)]
        public string Label { get; set; }
    }
}
