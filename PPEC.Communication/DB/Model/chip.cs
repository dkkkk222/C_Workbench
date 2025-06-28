using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace PPEC.Communication.DB.Model
{
    public class smls_chip
    {
        [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("file_path")] public string FilePath { get; set; }


    }
}
