using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Db.Tables
{
    [Table("t_chip")]
    public class Chip
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("name", CanBeNull = false, Length = 64)]
        public string Name { get; set; }

        [Column("file_name", CanBeNull = false)]
        public string FileName { get; set; }

        [Column("sdpc_file_name", CanBeNull = false)]
        public string SDPCfileName { get; set; }
        [Column("doc_filepath", CanBeNull = false)]
        public string DocFilePath { get; set; }

        [Column("datetime", CanBeNull = false)]
        public string Datetime { get; set; }

        [Column("is_deleted", CanBeNull = false, Length = 2)]
        public string IsDeleted { get; set; }
    }
}
