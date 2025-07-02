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
    }
}
