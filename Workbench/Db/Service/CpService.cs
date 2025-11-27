using AutoMapper;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using MathNet.Numerics.RootFinding;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PPEC.Communication.Common;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Workbench.Db.IService;
using Workbench.Db.Tables;

namespace Workbench.Db.Service
{
    public class CpService : ICpService
    {
        private readonly IMapper _mapper;
        public CpService(IMapper mapper)
        {
            _mapper = mapper;
        }
        public async Task<Chip> GetChipById(string id)
        {
            using (var db = new DbContext())
            {
                return await db.Chips.FirstOrDefaultAsync(t => t.Id == id);
            }
        }

        public async Task<List<RegisterMeta>> GetChipRegisters(string chipId)
        {
            var target = new List<RegisterMeta>();
            using (var db = new DbContext())
            {
                var registers = await db.Registers.Where(t => t.ChipId == chipId).ToListAsync();
                foreach (var register in registers)
                {
                    var meta = new RegisterMeta();
                    var addressInfo = _mapper.Map<RegisterAddrInfo>(register);

                    //bit field
                    var registerBits = await db.RegisterBits.Where(t => t.RegisterId == register.Id).ToListAsync();
                    var bitFields = new List<BitField>();
                    foreach (var rgb in registerBits)
                    {
                        var bitField = _mapper.Map<BitField>(rgb);

                        #region 公式计算赋值
                        bitField.FormParam.ParamA = rgb.ParamA==null?1:double.Parse(rgb.ParamA);
                        bitField.FormParam.ParamB = rgb.ParamB==null?0:double.Parse(rgb.ParamB);
                        bitField.FormParam.ParamC = rgb.ParamC;
                        bitField.FormParam.ParamDic =UtilHelper.ParseExcelDataToDictionary(rgb.ParamName);
                        bitField.FormParam.ParamName = rgb.ParamName;
                        bitField.FormParam.UnitName = rgb.UnitName;
                        #endregion

                        var registerBitOptions = await db.RegisterBitOptions.Where(t => t.RegisterBitId == rgb.Id).ToListAsync();
                        var bitOptions = _mapper.Map<ObservableCollection<BitOption>>(registerBitOptions);
                        bitField.Options = bitOptions;

                        bitFields.Add(bitField);
                    }

                    addressInfo.BitFields =new ObservableCollection<BitField> (bitFields);

                    meta.AddrInfo = addressInfo;
                    target.Add(meta);
                }
            }

            return target;
        }

        public async Task<List<TelemetryCode>> GetTeleList(string chipId)
        {
            using (var db = new DbContext())
            {
                return await db.TelemetryCodes.Where(t => t.ChipId == chipId).ToListAsync();
            }
        }

        public async Task<List<TelemetryMonit>> GetTeleMoniteList(string chipId)
        {
            using (var db = new DbContext())
            {
                return await db.TelemetryMonits.Where(t => t.ChipId == chipId).ToListAsync();
            }
        }

        // ======================= 新增的三个数据库操作 =======================

        public async Task SaveParamsListAsync(string chipId, IEnumerable<ParamDict> items)
        {
            using var db = new DbContext();
            using var tx = await db.BeginTransactionAsync();

            // 1) 删除该 ChipId 旧数据
            await db.ParamDicts.Where(t => t.ChipId == chipId).DeleteAsync();

            // 2) 准备新数据
            var list = items.Select(x =>
            { 
                x.ChipId = chipId;
                return x;
            }).ToList();

            // 3) 批量写入
            await db.BulkCopyAsync(new BulkCopyOptions { MaxBatchSize = 1000 }, list);

            await tx.CommitAsync();
        }

        public async Task SaveTeleMonListAsync(string chipId, IEnumerable<TelemetryMonit> items)
        {
            using var db = new DbContext();
            using var tx = await db.BeginTransactionAsync();

            // 1) 删除该 ChipId 旧数据
            await db.TelemetryMonits.Where(t => t.ChipId == chipId).DeleteAsync();

            // 2) 准备新数据
            var list = items.Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Id))
                    x.Id = Guid.NewGuid().ToString("N");
                x.ChipId = chipId;
                return x;
            }).ToList();

            // 3) 批量写入
            await db.BulkCopyAsync(new BulkCopyOptions { MaxBatchSize = 1000 }, list);

            await tx.CommitAsync();
        }

        public async Task SaveTeleTagListAsync(string chipId, IEnumerable<TelemetryTagTable> items)
        {
            try
            {
                using var db = new DbContext();
                using var tx = await db.BeginTransactionAsync();

                // 1) 删除该 ChipId 旧数据
                await db.TelemetryTagTs.Where(t => t.ChipId == chipId).DeleteAsync();

                // 2) 准备新数据
                var list = items.Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.Id))
                        x.Id = Guid.NewGuid().ToString("N");
                    x.ChipId = chipId;
                    return x;
                }).ToList();

                // 3) 批量写入
                await db.BulkCopyAsync(new BulkCopyOptions { MaxBatchSize = 1000 }, list);

                await tx.CommitAsync();
            }
            catch(Exception ex)
            {

            }
        }
        /// <summary>
        /// 保存：按 ChipId 先删后插（事务）。
        /// </summary>
        public async Task SaveTeleListAsync(string chipId, IEnumerable<TelemetryCode> items)
        {
            using var db = new DbContext();
            using var tx = await db.BeginTransactionAsync();

            // 1) 删除该 ChipId 旧数据
            await db.TelemetryCodes.Where(t => t.ChipId == chipId).DeleteAsync();

            // 2) 准备新数据
            var list = items.Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Id))
                    x.Id = Guid.NewGuid().ToString("N");
                x.ChipId = chipId;
                return x;
            }).ToList();

            // 3) 批量写入
            await db.BulkCopyAsync(new BulkCopyOptions { MaxBatchSize = 1000 }, list);

            await tx.CommitAsync();
        }

        /// <summary>
        /// 删除：按 codeId 删除 Telemetry 记录（返回删除条数）。
        /// </summary>
        public async Task<int> DeleteTeleByChipAsync(string codeId)
        {
            using var db = new DbContext();
            var affected = await db.TelemetryCodes.Where(t => t.Id == codeId).DeleteAsync();
            return affected;
        }

        /// <summary>
        /// 新增：插入单条 Telemetry 记录（返回生成的 Id）。
        /// </summary>
        public async Task<string> AddTelemetryAsync(string chipId, TelemetryCode item)
        {
            using var db = new DbContext();

            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N");

            item.ChipId = chipId;

            await db.InsertAsync(item);
            return item.Id;
        }
        public async Task<int> UpdateTelemetryAsync(TelemetryCode item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(item.Id)) throw new ArgumentException("Id 不能为空", nameof(item));

            using var db = new DbContext();

            var rows = await db.GetTable<TelemetryCode>()
                .Where(t => t.Id == item.Id)
                .Set(t => t.Name, item.Name)
                .Set(t => t.Code, item.Code)
                .Set(t => t.Type, item.Type)
                .Set(t => t.Length, item.Length)
                .Set(t => t.ChipId, item.ChipId)   // 如不希望更新 ChipId，可删掉本行
                .UpdateAsync();

            return rows;
        }

        public async Task<int> UpdateTelemetryFieldsAsync(
                        string id,
                        string name = null,
                        string code = null,
                        string type = null,
                        string length = null,
                        string chipId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id 不能为空", nameof(id));

            using var db = new DbContext();

            var q = db.GetTable<TelemetryCode>().Where(t => t.Id == id);

            IUpdatable<TelemetryCode> up = null;

            // 注意：这里用的是本地函数；C# 7.0+ 支持
            void AddSet<TField>(Expression<Func<TelemetryCode, TField>> field, TField value)
            {
                // 第一次调用从 IQueryable 起步，之后在 IUpdatable 上继续链式追加
                up = up == null ? q.Set(field, value) : up.Set(field, value);
            }

            if (name != null) AddSet(t => t.Name, name);
            if (code != null) AddSet(t => t.Code, code);
            if (type != null) AddSet(t => t.Type, type);
            if (length != null) AddSet(t => t.Length, length);
            if (chipId != null) AddSet(t => t.ChipId, chipId);

            if (up == null) return 0; // 没有任何字段需要更新

            return await up.UpdateAsync();
        }

        #region Excel
        /// <summary>
        /// 从数据库读取指定 Chip 的 TelemetryCode，导出到 Excel。
        /// </summary>
        public async Task ExportTelemetryExcelAsync(string chipId, string xlsxPath)
        {
            List<TelemetryCode> list;
            using (var db = new DbContext())
            {
                list = await db.TelemetryCodes.Where(t => t.ChipId == chipId).ToListAsync();
            }

            await ExportTelemetryExcelAsync(list, xlsxPath);
        }

        /// <summary>
        /// 直接把传入的 TelemetryCode 集合导出到 Excel（适合从界面绑定集合导出）。
        /// </summary>
        public Task ExportTelemetryExcelAsync(IEnumerable<TelemetryCode> items, string xlsxPath)
        {
            // 归一化 Type，避免大小写/空白差异
            string norm(string s) => (s ?? string.Empty).Trim();

            var all = (items ?? Enumerable.Empty<TelemetryCode>()).ToList();

            // 分类：只放入这两类，其它类型忽略（如需落到第三个 Sheet，可再加）
            var indirect = all.Where(x => norm(x.Type) == "0").OrderBy(x => x.Code).ToList();
            var countCmd = all.Where(x => norm(x.Type) == "1").OrderBy(x => x.Code).ToList();

            // 开始写 Excel
            var wb = new XSSFWorkbook();

            // 通用样式
            var headerStyle = CreateHeaderStyle(wb);
            var textStyle = CreateTextStyle(wb);

            // Sheet1：间接指令
            {
                var sheet = wb.CreateSheet("间接指令");
                // 头
                var headers = new[] { "编号", "指令名称", "指令码", "指令类型" };
                WriteHeaderRow(sheet, headers, headerStyle);

                // 数据
                int rowIndex = 1;
                for (int i = 0; i < indirect.Count; i++)
                {
                    var t = indirect[i];
                    var r = sheet.CreateRow(rowIndex++);
                    WriteCell(r, 0, (i + 1).ToString(), textStyle); // 编号
                    WriteCell(r, 1, t.Name, textStyle);
                    WriteCell(r, 2, t.Code, textStyle);
                    WriteCell(r, 3,  "遥控指令", textStyle);
                }

                sheet.CreateFreezePane(0, 1);
                AutoSizeColumns(sheet, headers.Length);
            }

            // Sheet2：注数指令
            {
                var sheet = wb.CreateSheet("注数指令");
                var headers = new[] { "编号", "指令名称", "指令长度(byte)", "指令码", "指令类型" };
                WriteHeaderRow(sheet, headers, headerStyle);

                int rowIndex = 1;
                for (int i = 0; i < countCmd.Count; i++)
                {
                    var t = countCmd[i];
                    var r = sheet.CreateRow(rowIndex++);
                    WriteCell(r, 0, (i + 1).ToString(), textStyle); // 编号
                    WriteCell(r, 1, t.Name, textStyle);
                    WriteCell(r, 2, t.Length, textStyle);           // 直接写 Length 字符串；如需转数字可再解析
                    WriteCell(r, 3, t.Code, textStyle);
                    WriteCell(r, 4, "注数指令", textStyle);
                }

                sheet.CreateFreezePane(0, 1);
                AutoSizeColumns(sheet, headers.Length);
            }

            // 保存
            var timeName = DateTime.Now.ToString("yyyyMMddHHmmss.fff", CultureInfo.InvariantCulture);
            var fileName = $"遥测指令表-{timeName}.xlsx";
            var filePath = System.IO.Path.Combine(xlsxPath, fileName);

             

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                wb.Write(fs);
            wb.Close();
            return Task.CompletedTask;
        }

        // ---------- NPOI 辅助 ----------

        private static ICellStyle CreateHeaderStyle(IWorkbook wb)
        {
            var style = wb.CreateCellStyle();
            var font = wb.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.FillPattern = FillPattern.SolidForeground;
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            var fmt = wb.CreateDataFormat();
            style.DataFormat = fmt.GetFormat("@"); // 文本
            return style;
        }

        private static ICellStyle CreateTextStyle(IWorkbook wb)
        {
            var style = wb.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;
            var fmt = wb.CreateDataFormat();
            style.DataFormat = fmt.GetFormat("@"); // 文本
            return style;
        }

        private static void WriteHeaderRow(ISheet sheet, string[] headers, ICellStyle style)
        {
            var row = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = row.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = style;
            }
            row.HeightInPoints = 20f;
        }

        private static void WriteCell(IRow row, int col, string value, ICellStyle style)
        {
            var cell = row.CreateCell(col, CellType.String);
            cell.SetCellValue(value ?? string.Empty);
            cell.CellStyle = style;
        }

        private static void AutoSizeColumns(ISheet sheet, int columnCount)
        {
            // 先设置一个合理的默认宽度，随后 AutoSize 提升观感
            sheet.DefaultColumnWidth = 18;
            for (int i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
                // 适当增加 2 个字符宽度，避免中文被截断
                var w = sheet.GetColumnWidth(i);
                sheet.SetColumnWidth(i, Math.Min(255 * 256, w + 2 * 256));
            }
        }
        #endregion
    }
}
