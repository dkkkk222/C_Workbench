using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Db.Tables;

namespace Workbench.Db
{
    public class DbContext : DataConnection
    {
        public DbContext() : base("SQLiteConnection") { }
        public DbContext(string connectionName) : base(connectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ArgumentException("Connection name cannot be null or whitespace.", nameof(connectionName));
            }
        }

        public ITable<Register> Registers => this.GetTable<Register>();

    }
}
