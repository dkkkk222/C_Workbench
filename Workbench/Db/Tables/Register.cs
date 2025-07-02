using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Db.Tables
{
    [Table("t_register")]
    public class Register
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        /// <summary>
        /// 寄存器名称
        /// </summary>
        [Column("name", CanBeNull = false)]
        public string Name { get; set; }

        [Column("chip_id", CanBeNull = false)]
        public string ChipId { get; set; }

        /// <summary>
        /// 寄存器地址DEC
        /// </summary>
        [Column("address_dec", CanBeNull = false)]
        public uint AddressDec { get; set; }

        /// <summary>
        /// 寄存器地址HEX
        /// </summary>
        [Column("address_hex", CanBeNull = false)]
        public string AddressHex { get; set; }

        /// <summary>
        /// 分类
        /// </summary>
        [Column("category", CanBeNull = false)]
        public string Category { get; set; }

        /// <summary>
        /// 子分类
        /// </summary>
        [Column("sub_category", CanBeNull = false)]
        public string SubCategory { get; set; }

        /// <summary>
        /// R/W
        /// </summary>
        [Column("rw", CanBeNull = false)]
        public string RW { get; set; }

        /// <summary>
        /// 复位值
        /// </summary>
        [Column("reset_value", CanBeNull = false)]
        public string ResetValue { get; set; }
    }
}
