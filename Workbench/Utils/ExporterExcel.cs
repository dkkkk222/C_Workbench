using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using log4net;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PPEC.Communication.DB.Provided;
using PPEC.Communication.Model;
using Workbench.Db;

namespace Workbench.Utils
{
    public class ExporterExcel
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExporterExcel));
        public void ExportSessionToExcel_MergedByTimeAndId(string sessionId, string xlsxDirectory, int page = 200_000)
        {
            try
            {

                using (var db = new DbContext())
                {
                    // 1) 取出：本 session 用到的 paramId -> name（名称可重复，仅用于显示）；列由 paramId 确定
                    var id2name = GetParamNameMapForSession(db, sessionId); // Dictionary<string, string>

                    // 列顺序：按 paramId 排（也可改成按 name 再按 id 排，但列键仍是 id）
                    //var headerIds = id2name.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
                    var headerIds = id2name.Keys
                                       .OrderBy(id => id2name[id] ?? "", StringComparer.Ordinal)
                                       .ThenBy(id => id, StringComparer.Ordinal)
                                       .ToList();

                    // 列显示标签：Name(paramId)。若无名称则仅显示 paramId
                    var headerLabels = headerIds
                        .Select(id => {
                            string n;
                            return id2name.TryGetValue(id, out n) && !string.IsNullOrWhiteSpace(n)
                                ? $"{n}"
                                : id;
                        })
                        .ToList();

                    // paramId -> 列序号
                    var id2col = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (int i = 0; i < headerIds.Count; i++) id2col[headerIds[i]] = i;

                    using (var wb = new XSSFWorkbook())
                    {
                        var sh = wb.CreateSheet("Data");
                        int rowIdx = 0;

                        // 2) 写表头
                        var header = sh.CreateRow(rowIdx++);
                        header.CreateCell(0).SetCellValue("时间");
                        for (int i = 0; i < headerLabels.Count; i++)
                            header.CreateCell(i + 1).SetCellValue(headerLabels[i]);

                        // 3) 分页读取 + 按“时间合并行”
                        long lastTs = -1;
                        long lastFid = 0;

                        long currentTs = -1;
                        string[] currentLine = null; // 与 headerIds 同长度

                        while (true)
                        {
                            var chunk = ReadChunkByTime(db, sessionId, lastTs, lastFid, page); // List<FlatRow>
                            if (chunk.Count == 0) break;

                            foreach (var rec in chunk)
                            {
                                // 新时间戳：先把上一时间的聚合行写出
                                if (currentTs != -1 && rec.TsUtcMs != currentTs)
                                {
                                    WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine);
                                    currentTs = -1;
                                    currentLine = null;
                                }

                                // 初始化当前时间的缓冲行
                                if (currentTs == -1)
                                {
                                    currentTs = rec.TsUtcMs;
                                    currentLine = new string[headerIds.Count];
                                }

                                // 以 paramId 定位列，最后值覆盖
                                int col;
                                if (id2col.TryGetValue(rec.ParamId, out col))
                                {
                                    currentLine[col] = rec.Val;
                                }
                                // 若某 paramId 不在 id2col（极少数晚到头的情况），可选择忽略或动态扩列（此处忽略）
                            }

                            // 更新分页游标
                            var last = chunk[chunk.Count - 1];
                            lastTs = last.TsUtcMs;
                            lastFid = last.FrameId;
                        }

                        // 末尾补写
                        if (currentTs != -1 && currentLine != null)
                        {
                            WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine);
                        }

                        // 4) 保存
                        Directory.CreateDirectory(xlsxDirectory);
                        var timeName = DateTime.Now.ToString("yyyyMMddHHmmss.fff");
                        var fileName = $"数据监测表-历史{timeName}.xlsx";
                        var filePath = System.IO.Path.Combine(xlsxDirectory, fileName);
                        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            wb.Write(fs);
                    }
                    _log.Info("下载完成");
                    MessageBox.Show("历史数据下载完成!");
                }
            }
            catch (Exception ex) 
            {
                _log.Error(ex);
            }
        }
        public void ExportSessionToExcel_MergedByTimeAndName(string sessionId, string xlsxPath, int page = 200_000)
        {
            using (var db = new DbContext())
            {
                // 1) 取出：本 session 用到的 param_id -> name；以及“去重后的参数名列表”做表头（按名称排序）
                var id2name = GetParamNameMapForSession(db, sessionId); // Dictionary<int, string>
                var headerNames = id2name.Values.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
                var name2col = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < headerNames.Count; i++) name2col[headerNames[i]] = i;

                using (var wb = new XSSFWorkbook())
                {
                    var sh = wb.CreateSheet("Data");
                    int rowIdx = 0;

                    // 2) 写表头
                    var header = sh.CreateRow(rowIdx++);
                    header.CreateCell(0).SetCellValue("时间");
                    for (int i = 0; i < headerNames.Count; i++)
                        header.CreateCell(i + 1).SetCellValue(headerNames[i]);

                    // 3) 按 (ts_utc_ms, frame_id) 分页读取并合并
                    long lastTs = -1;       // 上一页最后时间
                    long lastFid = 0;       // 在相同时间内的最后 frame_id
                                            // 跨页“半行缓存”：如果某个时间的行在页末未写完，续到下一页继续合并
                    long currentTs = -1;
                    string[] currentLine = null; // 按 headerNames 长度的数组；每列一个值

                    while (true)
                    {
                        var chunk = ReadChunkByTime(db, sessionId, lastTs, lastFid, page); // List<FlatRow>
                        if (chunk.Count == 0) break;

                        foreach (var rec in chunk)
                        {
                            // 如果遇到新的时间戳：先把上一个时间的聚合行写出
                            if (currentTs != -1 && rec.TsUtcMs != currentTs)
                            {
                                WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine);
                                currentTs = -1;
                                currentLine = null;
                            }

                            // 初始化当前时间的缓冲行
                            if (currentTs == -1)
                            {
                                currentTs = rec.TsUtcMs;
                                currentLine = new string[headerNames.Count];
                            }

                            // 将该记录写入“同名参数的列”（同名出现多次，以最后出现的值覆盖）
                            string name;
                            if (id2name.TryGetValue(rec.ParamId, out name))
                            {
                                int col;
                                if (name2col.TryGetValue(name, out col))
                                {
                                    // 同名合并策略：最后值覆盖
                                    currentLine[col] = rec.Val;
                                }
                            }
                        }

                        // 更新分页游标（取本页最后一条记录的时间和frame_id）
                        var last = chunk[chunk.Count - 1];
                        lastTs = last.TsUtcMs;
                        lastFid = last.FrameId;
                    }

                    // 循环结束后，如果还有未写出的“当前时间行”，补写
                    if (currentTs != -1 && currentLine != null)
                    {
                        WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine);
                    }
                    string fileName = "数据监测表-历史.xlsx";
                    string filePath = System.IO.Path.Combine(xlsxPath, fileName);
                    // 4) 保存
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        wb.Write(fs);
                }
            }
        }
        private static void WriteCurrentLine(ISheet sh, ref int rowIdx, long tsUtcMs, string[] line)
        {
            var row = sh.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(
                MsToLocal(tsUtcMs).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            for (int i = 0; i < line.Length; i++)
            {
                var c = row.CreateCell(i + 1);
                c.SetCellValue(line[i]);
                //else c.SetCellValue("");
            }
        }
        private static List<FlatRow> ReadChunkByTime(DbContext db, string sessionId, long lastTs, long lastFid, int take)
        {
            // 说明：
            // - v.Result：按你的模型取数值列；如果你是 val_real/val_int，请改回 COALESCE 逻辑
            // - ParamId/SessionId 为 string；若你的库是 int，请把类型改回 int

            return (from v in db.Values
                    join f in db.Frames on v.FrameId equals f.FrameId
                    where f.SessionId == sessionId
                       && (f.TsUtcMs > lastTs || (f.TsUtcMs == lastTs && f.FrameId > lastFid))
                    orderby f.TsUtcMs, f.FrameId
                    select new FlatRow
                    {
                        FrameId = f.FrameId,
                        TsUtcMs = f.TsUtcMs,
                        ParamId = v.ParamId,                         // 以参数ID为列键
                        Val = v.Result                           // <<< 如果你没有 Result，请改为：
                                                                 // Val = v.ValReal ?? (v.ValInt.HasValue ? (double?)v.ValInt.Value : null)
                    })
                   .Take(take)
                   .ToList();
        }
        private static Dictionary<string, string> GetParamNameMapForSession(DbContext db, string sessionId)
        {
            // 你的模型示例中：参数表是 RegisterBits（Id, Desc）
            // 如果你的参数表叫 parameter(name) 或别的，请在这里改成对应的表与字段
            return (from v in db.Values
                    join f in db.Frames on v.FrameId equals f.FrameId
                    join p in db.RegisterBits on v.ParamId equals p.Id
                    where f.SessionId == sessionId
                    select new { p.Id, p.Desc })
                   .Distinct()
                   .ToList()
                   .ToDictionary(x => x.Id, x => x.Desc ?? "", StringComparer.Ordinal);
        }

        private static DateTime MsToLocal(long msUtc)
        => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
           .AddMilliseconds(msUtc)
           .ToLocalTime();
    }

    public static class HistoryExcelExporter_BySeq
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HistoryExcelExporter_BySeq));
        public static void ExportSessionToExcel_MergedByTimeAndSeq(
            string connStr,
            string sessionId,
            string xlsxDirectory,
            int page = 200_000)
        {
            // 1) 列头：paramId -> name（仅取本 session 出现过的）
            var id2name = GetParamNameMapForSession(connStr, sessionId); // Dictionary<int,string>

            //var headerIds = id2name.Keys
            //    .OrderBy(id => id2name[id] ?? "", StringComparer.Ordinal)
            //    .ThenBy(id => id)
            //    .ToList();
            var headerIds = id2name.Keys
                .OrderBy(id => GetAlphaPrefix(id2name[id]), StringComparer.OrdinalIgnoreCase)
                .ThenBy(id => GetTailNumberOrMax(id2name[id]))
                .ThenBy(id => id) // 兜底
                .ToList();

            var headerLabels = headerIds
                .Select(id => string.IsNullOrWhiteSpace(id2name[id]) ? id.ToString() : id2name[id])
                .ToList();

            var id2col = new Dictionary<int, int>(headerIds.Count);
            for (int i = 0; i < headerIds.Count; i++) id2col[headerIds[i]] = i + 1; // +1 因为 0 是时间列

            using (var wb = new XSSFWorkbook())
            {
                var sh = wb.CreateSheet("Data");
                int rowIdx = 0;

                // 表头
                var header = sh.CreateRow(rowIdx++);
                header.CreateCell(0).SetCellValue("时间");
                for (int i = 0; i < headerLabels.Count; i++)
                    header.CreateCell(i + 1).SetCellValue(headerLabels[i]);

                // 时间样式
                var tsStyle = wb.CreateCellStyle();
                var fmt = wb.CreateDataFormat();
                tsStyle.DataFormat = fmt.GetFormat("yyyy-mm-dd hh:mm:ss.000");

                // 2) 分页读取 + 按“时间合并行”
                long lastTs = -1;
                int lastSeq = -1;

                long currentTs = -1;
                double?[] currentLine = null;

                while (true)
                {
                    var chunk = ReadChunkByTime_Seq(connStr, sessionId, lastTs, lastSeq, page);
                    if (chunk.Count == 0) break;

                    foreach (var rec in chunk)
                    {
                        // 新时间戳 -> 先写上一时间行
                        if (currentTs != -1 && rec.TsTicks != currentTs)
                        {
                            WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine, tsStyle);
                            currentTs = -1;
                            currentLine = null;
                        }

                        // 初始化当前时间缓冲
                        if (currentTs == -1)
                        {
                            currentTs = rec.TsTicks;
                            currentLine = new double?[headerIds.Count];
                        }

                        if (id2col.TryGetValue(rec.ParamId, out int col))
                            currentLine[col - 1] = rec.NumValue;
                    }

                    var last = chunk[chunk.Count - 1];
                    lastTs = last.TsTicks;
                    lastSeq = last.Seq;
                }

                if (currentTs != -1 && currentLine != null)
                    WriteCurrentLine(sh, ref rowIdx, currentTs, currentLine, tsStyle);

                // 3) 保存（关键：确保目录 + FileStream）
                Directory.CreateDirectory(xlsxDirectory);
                var timeName = DateTime.Now.ToString("yyyyMMddHHmmss.fff", CultureInfo.InvariantCulture);
                var fileName = $"遥测数据监测表-历史-{timeName}.xlsx";
                var filePath = System.IO.Path.Combine(xlsxDirectory, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    wb.Write(fs);
                _log.Info("下载完成");
                MessageBox.Show("历史数据下载完成!");
            }
        }

        private static readonly System.Text.RegularExpressions.Regex sPrefix =
    new System.Text.RegularExpressions.Regex(@"^\s*([A-Za-z]+)", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex sNumber =
            new System.Text.RegularExpressions.Regex(@"(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string GetAlphaPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var m = sPrefix.Match(name);
            return m.Success ? m.Groups[1].Value : name;
        }

        private static int GetTailNumberOrMax(string name)
        {
            if (string.IsNullOrEmpty(name)) return int.MaxValue;
            var m = sNumber.Match(name);
            int n;
            return (m.Success && int.TryParse(m.Groups[1].Value, out n)) ? n : int.MaxValue;
        }

        private static List<FlatRowSeq> ReadChunkByTime_Seq(
            string connStr, string sessionId, long lastTs, int lastSeq, int page)
        {
            using (var db = new DbContext())
            {
                var q =
                    from f in db.HistoryFrames
                    where f.SessionId == sessionId
                    join v in db.HistoryValues
                        on new { f.SessionId, f.TsTicks, f.Seq }
                        equals new { v.SessionId, v.TsTicks, v.Seq }
                    where (f.TsTicks > lastTs) || (f.TsTicks == lastTs && f.Seq > lastSeq)
                    orderby f.TsTicks, f.Seq, v.ParamId
                    select new FlatRowSeq
                    {
                        TsTicks = f.TsTicks,
                        Seq = f.Seq,
                        ParamId = v.ParamId,
                        NumValue = v.NumValue
                    };

                return q.Take(page).ToList();
            }
        }

        private static Dictionary<int, string> GetParamNameMapForSession(string connStr, string sessionId)
        {
            using (var db = new DbContext())
            {
                var q =
                    (from v in db.HistoryValues
                     join p in db.ParamDicts on v.ParamId equals p.ParamId
                     join f in db.HistoryFrames on new { v.SessionId, v.TsTicks, v.Seq }
                                               equals new { f.SessionId, f.TsTicks, f.Seq }
                     where f.SessionId == sessionId
                     select new { p.ParamId, p.Name })
                    .Distinct();

                return q.ToDictionary(x => x.ParamId, x => x.Name);
            }
        }

        private static void WriteCurrentLine(ISheet sh, ref int rowIdx, long tsTicks, double?[] line, ICellStyle tsStyle)
        {
            var row = sh.CreateRow(rowIdx++);
            var dt = new DateTime(tsTicks); // 若存的是 UTC ticks 则改为 new DateTime(tsTicks, DateTimeKind.Utc).ToLocalTime()
            var c0 = row.CreateCell(0);
            c0.SetCellValue(dt);
            c0.CellStyle = tsStyle;

            for (int i = 0; i < line.Length; i++)
            {
                var c = row.CreateCell(i + 1);
                var val = line[i];
                if (val.HasValue) c.SetCellValue(val.Value);
                else c.SetCellValue(string.Empty);
            }
        }
    }
}
