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

        public ITable<Chip> Chips => this.GetTable<Chip>();
        public ITable<Register> Registers => this.GetTable<Register>();
        public ITable<RegisterBit> RegisterBits => this.GetTable<RegisterBit>();
        public ITable<RegisterBitOption> RegisterBitOptions => this.GetTable<RegisterBitOption>();
        public ITable<FrameRow> Frames => this.GetTable<FrameRow>();
        public ITable<ValueRow> Values => this.GetTable<ValueRow>();
    }
}
