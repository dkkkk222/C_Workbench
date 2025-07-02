using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Migrations
{
    [Migration(202507020935)]
    public class OnInitial : Migration
    {
        public override void Down()
        {
            Create.Table("t_register")
                .WithColumn("id").AsString().PrimaryKey();
            Create.Index("IX_t_register_id").OnTable("t_register").OnColumn("id").Ascending().WithOptions().Unique();
        }

        public override void Up()
        {
        }
    }
}
