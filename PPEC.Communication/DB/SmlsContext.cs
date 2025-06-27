using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Data;
using LinqToDB;
using PPEC.Communication.DB.Model;

namespace PPEC.Communication.DB
{
    public class SmlsContext : DataConnection
    {
        public SmlsContext(string connectionString) : base(new DataOptions().UseSQLite(connectionString))
        {
        }
    }
}
