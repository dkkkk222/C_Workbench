using AutoMapper;
using FluentMigrator;
using NPOI.SS.UserModel;
using PPEC.Communication.Common;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Linq;
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
                .WithColumn("sdpc_file_name").AsString().Nullable()
                .WithColumn("doc_filepath").AsString().Nullable()
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
                .WithColumn("name").AsString().NotNullable()
                .WithColumn("start_bit").AsInt32().NotNullable()
                .WithColumn("end_bit").AsInt32().NotNullable()
                .WithColumn("length").AsInt32().NotNullable()
                .WithColumn("desc").AsString().Nullable()
                .WithColumn("field_type").AsString().Nullable()
                .WithColumn("range_min").AsInt32().Nullable()
                .WithColumn("range_max").AsInt32().Nullable()
                .WithColumn("param_a").AsString().Nullable()
                .WithColumn("param_b").AsString().Nullable()
                .WithColumn("param_c").AsString().Nullable()
                .WithColumn("param_unit").AsString().Nullable()
                .WithColumn("formula_show").AsString().Nullable();
            Create.Index("IX_t_register_bit_id").OnTable("t_register_bit").OnColumn("id").Ascending().WithOptions().Unique();
            Create.Index("IX_t_register_bit_register_id").OnTable("t_register_bit").OnColumn("register_id").Ascending();

            Create.Table("t_register_bit_option")
                .WithColumn("id").AsString().PrimaryKey()
                .WithColumn("register_bit_id").AsString().NotNullable()
                .WithColumn("value").AsInt32().NotNullable()
                .WithColumn("key").AsString().NotNullable()
                .WithColumn("label").AsString().Nullable()
                .WithColumn("display").AsString().NotNullable();
            Create.Index("IX_t_register_bit_option_id").OnTable("t_register_bit_option").OnColumn("id").Ascending().WithOptions().Unique();
            Create.Index("IX_t_register_bit_option_register_bit_id").OnTable("t_register_bit_option").OnColumn("register_bit_id");

            #region Log
            Create.Table("t_session")
             .WithColumn("session_id").AsString().PrimaryKey()
             .WithColumn("start_utc_ms").AsInt32().Nullable()
             .WithColumn("end_utc_ms").AsInt32().Nullable();
            Create.Index("IX_t_session_session_id").OnTable("t_session").OnColumn("session_id").Ascending().WithOptions().Unique();

            Create.Table("t_frame")
              .WithColumn("frame_id").AsInt32().PrimaryKey()
              .WithColumn("session_id").AsString().Nullable()
              .WithColumn("ts_utc_ms").AsInt32().Nullable();
            Create.Index("IX_t_frame_frame_id").OnTable("t_frame").OnColumn("frame_id").Ascending().WithOptions().Unique();

            Create.Table("t_WatchHistory")
              .WithColumn("frame_id").AsInt32().PrimaryKey()
              .WithColumn("param_id").AsString().Nullable()
              .WithColumn("resultParse").AsString().Nullable()
              .WithColumn("result").AsString().Nullable();
            #endregion

            #region Code

            #region 遥测遥控
            Create.Table("t_TelemetryCode")
             .WithColumn("id").AsString().PrimaryKey()
             .WithColumn("chip_id").AsString().NotNullable()
             .WithColumn("name").AsString().Nullable()
             .WithColumn("code").AsString().Nullable()
             .WithColumn("type").AsString().Nullable()
             .WithColumn("length").AsString().Nullable();

            Create.Table("t_TelemetryMonit")
            .WithColumn("id").AsString().PrimaryKey()
            .WithColumn("chip_id").AsString().NotNullable()
            .WithColumn("name").AsString().Nullable()
            .WithColumn("byte_Name").AsString().Nullable()
            .WithColumn("start_byte").AsInt32().Nullable()
            .WithColumn("end_byte").AsInt32().Nullable()
            .WithColumn("byte_len").AsInt32().Nullable()
            .WithColumn("bit_Name").AsString().Nullable()
            .WithColumn("start_bit").AsInt32().Nullable()
            .WithColumn("end_bit").AsInt32().Nullable()
            .WithColumn("bit_len").AsInt32().Nullable()
            .WithColumn("type").AsInt32().Nullable()
            .WithColumn("param_a").AsString().Nullable()
            .WithColumn("param_b").AsString().Nullable()
            .WithColumn("param_c").AsString().Nullable()
            .WithColumn("param_sign").AsString().Nullable()
            .WithColumn("formula_show").AsString().Nullable()
            .WithColumn("unit").AsString().Nullable();

            Create.Table("t_TelemetryTag")
             .WithColumn("id").AsString().PrimaryKey()
             .WithColumn("chip_id").AsString().NotNullable()
             .WithColumn("name").AsString().Nullable();
            #endregion


            //下面是历史记录相关表
            Create.Table("param_dict")
              .WithColumn("param_id").AsInt32().PrimaryKey().Identity()
              .WithColumn("chip_id").AsString().NotNullable()
              .WithColumn("name").AsString().Nullable()
              .WithColumn("type_code").AsInt32().Nullable();

            Create.Table("history_session")
              .WithColumn("session_id").AsString().PrimaryKey()
              .WithColumn("started_at").AsString().Nullable()
              .WithColumn("ended_at").AsString().Nullable();

            Create.Table("history_frame")
              .WithColumn("session_id").AsString().Nullable()
              .WithColumn("ts_ticks").AsInt64().Nullable()
              .WithColumn("seq").AsInt32().Nullable();

            Create.Table("history_value")
              .WithColumn("session_id").AsString().Nullable()
              .WithColumn("ts_ticks").AsInt64().Nullable()
               .WithColumn("seq").AsInt32().Nullable()
              .WithColumn("param_id").AsInt32().Nullable()
              .WithColumn("num_value").AsDouble().Nullable()
               .WithColumn("text_value").AsString().Nullable();


            #endregion
            string fileName = "B1.0版本RTL接口及寄存器描述_V1.9_20250421_增加分类.xlsx";
            string SDPCfileName1 = "SDPC_workbench软件数据监控表_zby0715.xlsx";
            string SDPCfileName = "SDPC_B10状态监测配置表_20250923.xlsx";
            var ListData=TelemetryParse();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            string SDPCfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileName);
            RegisterExcelResolve registerExcelResolve = new RegisterExcelResolve();
            var excelData = registerExcelResolve.Parse(filePath);
            var excelSDPCData = registerExcelResolve.SDPCParse(SDPCfilePath, excelData);
            string chipId = Guid.NewGuid().ToString("N");
            Insert.IntoTable("t_chip").Row(new
            {
                id = chipId,
                name = "ChipA",
                is_deleted = "A",
                file_name = fileName,
                sdpc_file_name= SDPCfileName,
                datetime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            foreach(var param1 in ListData.Item1)
            {
                string telemetryId = Guid.NewGuid().ToString("N");
                Insert.IntoTable("t_TelemetryCode").Row(new
                {
                    id = telemetryId,
                    chip_id= chipId,
                    name = param1.CommandName,
                    code = param1.CommandCode,
                    type = (int)param1.CommandType,
                    length = param1.CommandLength
                });
            }
            foreach (var param1 in ListData.Item2.Item1)
            {
                Insert.IntoTable("param_dict").Row(new
                {
                    chip_id = chipId,
                    name= param1.CodeName
                }); 
                string telemetryId = Guid.NewGuid().ToString("N");
                Insert.IntoTable("t_TelemetryMonit").Row(new
                {
                    id = telemetryId,
                    chip_id = chipId,
                    name = param1.CodeName,
                    byte_Name = param1.DateLocation,
                    start_byte = param1.StartLocaltion,
                    end_byte = param1.EndLocaltion,
                    byte_len = param1.LocaltionLen,
                    bit_Name = param1.BitName,
                    start_bit = param1.StartBit,
                    end_bit = param1.EndBit,
                    bit_len = param1.BitLength,
                    type = (int)param1.FormParam.Kind,
                    param_a = param1.FormParam.A,
                    param_b = param1.FormParam.B,
                    param_c = (int)param1.FormParam.Kind,
                    param_sign = param1.FormParam.Sign,
                    formula_show = param1.ShowFormParam,
                    unit= param1.Unit,
                });
            }
            //foreach (var param1 in ListData.Item2.Item2)
            //{
            //    string tagId = Guid.NewGuid().ToString("N");
            //    Insert.IntoTable("t_TelemetryTag").Row(new
            //    {
            //        id = tagId,
            //        chip_id = chipId,
            //        name = param1.Name
            //    });
            //}
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
                        range_max = bf.RangeMax,
                        field_type = bf.FieldType,
                        name = bf.Name,
                        param_a=bf.FormParam.ParamA,
                        param_b = bf.FormParam.ParamB,
                        param_c = bf.FormParam.ParamC,
                        param_unit=bf.FormParam.UnitName,
                        formula_show=bf.FormParam.ParamName
                    });

                    foreach (var option in bf.Options)
                    {
                        Insert.IntoTable("t_register_bit_option").Row(new
                        {
                            id = Guid.NewGuid().ToString("N"),
                            register_bit_id = registerBitId,
                            value = option.Value,
                            key = option.Key,
                            label = option.Label,
                            display = option.Display
                        });
                    }
                }

            }
        }
        /// <summary>
        /// 遥测
        /// </summary>
        public (List<TelemetryMeta>, (List<TelemetryMonitAnalysisMeta>, List<TelemetryTag>)) TelemetryParse()
        {
            string SDPCfileNameTelemetryData = "SDPC_B10遥测数据表.xlsx";//数据解析
            string SDPCfileNameCommand = "SDPC_B10遥控指令表.xlsx";//遥测指令
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileNameCommand);
            string filePath1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileNameTelemetryData);
            RegisterExcelResolve registerExcelResolve = new RegisterExcelResolve();
            var telemetryCommand= registerExcelResolve.Telemetry(filePath);
            var telemetryMonit = registerExcelResolve.TelemetryMonit(filePath1);
            return (telemetryCommand, telemetryMonit);
        }

        public override void Down()
        {
        }
    }
}
