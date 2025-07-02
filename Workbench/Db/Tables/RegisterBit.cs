using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Db.Tables
{
    [Table("t_register_bit")]
    public class RegisterBit
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("register_id", CanBeNull = false)]
        public string RegisterId { get; set; }

        /// <summary>
        /// 低位
        /// </summary>
        [Column("start_bit", CanBeNull = false)]
        public int StartBit { get; set; }

        /// <summary>
        /// 高位
        /// </summary>
        [Column("end_bit", CanBeNull = false)]
        public int EndBit { get; set; }

        [Column("length", CanBeNull = false)]
        public int Length { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Column("desc")]
        public string Desc { get; set; }

        /// <summary>
        /// 连续范围最小值
        /// </summary>
        [Column("range_min")]
        public uint? RangeMin { get; set; }

        /// <summary>
        /// 连续范围最大值
        /// </summary>
        [Column("range_max")]
        public uint? RangeMax { get; set; }
    }
}
