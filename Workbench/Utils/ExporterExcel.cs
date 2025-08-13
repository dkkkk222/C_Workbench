using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PPEC.Communication.Model;
using Workbench.Db;

namespace Workbench.Utils
{
    public class ExporterExcel
    {
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
                    double?[] currentLine = null; // 按 headerNames 长度的数组；每列一个值

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
                                currentLine = new double?[headerNames.Count];
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
                    string fileName = "数据监控表-历史.xlsx";
                    string filePath = System.IO.Path.Combine(xlsxPath, fileName);
                    // 4) 保存
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        wb.Write(fs);
                }
            }
        }
        private static void WriteCurrentLine(ISheet sh, ref int rowIdx, long tsUtcMs, double?[] line)
        {
            var row = sh.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(
                MsToLocal(tsUtcMs).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            for (int i = 0; i < line.Length; i++)
            {
                var c = row.CreateCell(i + 1);
                if (line[i].HasValue) c.SetCellValue(line[i].Value);
                else c.SetCellValue("");
            }
        }
        private static List<FlatRow> ReadChunkByTime(DbContext db, string sessionId, long lastTs, long lastFid, int take)
        {
            // 等价 SQL 思路：
            // SELECT f.frame_id, f.ts_utc_ms, v.param_id, COALESCE(v.val_real, CAST(v.val_int AS REAL)) AS val
            // FROM frame f JOIN value v ON v.frame_id=f.frame_id
            // WHERE f.session_id=@sid AND (f.ts_utc_ms>@lastTs OR (f.ts_utc_ms=@lastTs AND f.frame_id>@lastFid))
            // ORDER BY f.ts_utc_ms, f.frame_id
            // LIMIT @take;

            return (from v in db.Values
                    join f in db.Frames on v.FrameId equals f.FrameId
                    where f.SessionId == sessionId
                       && (f.TsUtcMs > lastTs || (f.TsUtcMs == lastTs && f.FrameId > lastFid))
                    orderby f.TsUtcMs, f.FrameId
                    select new FlatRow
                    {
                        FrameId = f.FrameId,
                        TsUtcMs = f.TsUtcMs,
                        ParamId = v.ParamId,
                        Val =v.Result 
                    })
                   .Take(take)
                   .ToList();
            //v.ValReal ?? (v.ValInt.HasValue ? (double?)v.ValInt.Value : null)
        }
        private static Dictionary<string, string> GetParamNameMapForSession(DbContext db, string sessionId)
        {
            // 只捞本 session 用到过的参数，避免无关参数进入表头
            return (from v in db.Values
                    join f in db.Frames on v.FrameId equals f.FrameId
                    join p in db.RegisterBits on v.ParamId equals p.Id
                    where f.SessionId == sessionId
                    select new { p.Id, p.Desc })
                   .Distinct()
                   .ToList()
                   .ToDictionary(x => x.Id, x => x.Desc, comparer: EqualityComparer<string>.Default);
        }
        
        private static DateTime MsToLocal(long msUtc)
        => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
           .AddMilliseconds(msUtc)
           .ToLocalTime();
    }
}
