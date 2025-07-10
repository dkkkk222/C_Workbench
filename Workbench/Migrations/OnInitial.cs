using AutoMapper;
using FluentMigrator;
using PPEC.Communication.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Workbench.Db.Tables;
using Workbench.Utils;
using static ScottPlot.Generate;

namespace Workbench.Migrations
{
    [Migration(202507020935)]
    public class OnInitial : Migration
    {
        public override void Up()
        {
            Create.Table("t_chip")
                .WithColumn("id").AsString().PrimaryKey()
                .WithColumn("name").AsString(64).NotNullable()
                .WithColumn("file_name").AsString().NotNullable()
                .WithColumn("datetime").AsString().NotNullable()
                .WithColumn("is_deleted").AsString(2).NotNullable();
            Create.Index("IX_t_chip_id").OnTable("t_chip").OnColumn("id").Ascending().WithOptions().Unique();

            Create.Table("t_register")
                .WithColumn("id").AsString().PrimaryKey()
                .WithColumn("chip_id").AsString().NotNullable()
                .WithColumn("name").AsString().NotNullable()
                .WithColumn("address_dec").AsInt32().NotNullable()
                .WithColumn("address_hex").AsString().NotNullable()
                .WithColumn("category").AsString().NotNullable()
                .WithColumn("sub_category").AsString().NotNullable()
                .WithColumn("rw").AsString().NotNullable()
                .WithColumn("reset_value").AsString().NotNullable();
            Create.Index("IX_t_register_id").OnTable("t_register").OnColumn("id").Ascending().WithOptions().Unique();
            Create.Index("IX_t_register_chip_id").OnTable("t_register").OnColumn("chip_id").Ascending();

            Create.Table("t_register_bit")
                .WithColumn("id").AsString().PrimaryKey()
                .WithColumn("register_id").AsString().NotNullable()
                .WithColumn("start_bit").AsInt32().NotNullable()
                .WithColumn("end_bit").AsInt32().NotNullable()
                .WithColumn("length").AsInt32().NotNullable()
                .WithColumn("desc").AsString().Nullable()
                .WithColumn("range_min").AsInt32().Nullable()
                .WithColumn("range_max").AsInt32().Nullable();
            Create.Index("IX_t_register_bit_id").OnTable("t_register_bit").OnColumn("id").Ascending().WithOptions().Unique();
            Create.Index("IX_t_register_bit_register_id").OnTable("t_register_bit").OnColumn("register_id").Ascending();

            Create.Table("t_register_bit_option")
                .WithColumn("id").AsString().PrimaryKey()
                .WithColumn("register_bit_id").AsString().NotNullable()
                .WithColumn("value").AsInt32().NotNullable()
                .WithColumn("display").AsString().NotNullable();
            Create.Index("IX_t_register_bit_option_id").OnTable("t_register_bit_option").OnColumn("id").Ascending().WithOptions().Unique();
            Create.Index("IX_t_register_bit_option_register_bit_id").OnTable("t_register_bit_option").OnColumn("register_bit_id");

            string fileName = "B1.0版本RTL接口及寄存器描述_V1.9_20250421_增加分类.xlsx";
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            var excelData = new RegisterExcelResolve().Parse(filePath);

            string chipId = Guid.NewGuid().ToString("N");
            Insert.IntoTable("t_chip").Row(new
            {
                id = chipId,
                name = "ChipA",
                is_deleted = "A",
                file_name = fileName,
                datetime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            foreach (var meta in excelData)
            {
                string registerId = Guid.NewGuid().ToString("N");

                Insert.IntoTable("t_register").Row(new
                {
                    id = registerId,
                    chip_id = chipId,
                    name = meta.AddrInfo.Name,
                    address_dec = meta.AddrInfo.AddressDec,
                    address_hex = meta.AddrInfo.AddressHex,
                    category = meta.AddrInfo.Category,
                    sub_category = meta.AddrInfo.SubCategory,
                    rw = meta.AddrInfo.RW,
                    reset_value = meta.AddrInfo.ResetValue
                });

                foreach (var bf in meta.AddrInfo.BitFields)
                {
                    string registerBitId = Guid.NewGuid().ToString("N");

                    Insert.IntoTable("t_register_bit").Row(new
                    {
                        id = registerBitId,
                        register_id = registerId,
                        start_bit = bf.StartBit,
                        end_bit = bf.EndBit,
                        length = bf.Length,
                        desc = bf.Desc,
                        range_min = bf.RangeMin,
                        range_max = bf.RangeMax
                    });

                    foreach (var option in bf.Options)
                    {
                        Insert.IntoTable("t_register_bit_option").Row(new
                        {
                            id = Guid.NewGuid().ToString("N"),
                            register_bit_id = registerBitId,
                            value = option.Value,
                            display = option.Display
                        });
                    }
                }

            }
        }

        public override void Down()
        {
        }
    }
}
