using LinqToDB.Mapping;

namespace Workbench.Db.Tables
{
    [Table("t_TelemetryMonit")]
    public class TelemetryMonit
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("chip_id")]
        public string ChipId { get; set; }

        [Column("name")]
        public string Name { get; set; }
        /// <summary>
        /// 分类
        /// </summary>
        [Column("category")]
        public string Category { get; set; }

        [Column("byte_Name")]
        public string ByteName { get; set; }

        [Column("start_byte")]
        public int StartByte { get; set; }
        /// <summary>
        /// 高位
        /// </summary>
        [Column("end_byte")]
        public int EndByte { get; set; }
        [Column("byte_len")]
        public int ByteLen { get; set; }
        [Column("bit_Name")]
        public string BitName { get; set; }
        [Column("start_bit")]
        public int StartBit { get; set; }
        [Column("end_bit")]
        public int EndBit { get; set; }
        [Column("bit_len")]
        public int BitLen { get; set; }

        [Column("type")]
        public int Type { get; set; }

        [Column("param_a")]
        public string ParamA { get; set; }

        [Column("param_b")]
        public string ParamB { get; set; }

        [Column("param_c")]
        public string ParamC { get; set; }


        [Column("param_sign")]
        public string ParamSign { get; set; }

        [Column("formula_show")]
        public string FormulaShow { get; set; }

        [Column("unit")]
        public string Unit { get; set; }
    }
}
