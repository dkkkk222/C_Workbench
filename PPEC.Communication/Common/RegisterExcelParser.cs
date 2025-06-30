using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PPEC.Communication.Model;

namespace PPEC.Communication.Common
{
    public class RegisterExcelParser
    {
        public List<RegisterMeta> Parse(string excelPath)
        {
            var metaList = new List<RegisterMeta>();
            var addrDictByName = new Dictionary<string, RegisterAddrInfo>(); // 名称→地址
            var formatter = new DataFormatter();
            using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook wb = WorkbookFactory.Create(fs);

                /* ---------- Sheet1：寄存器地址表 ---------- */
                var sheetAddr = wb.GetSheet("寄存器地址");               // 假设第 1 张
                for (int i = 1; i <= sheetAddr.LastRowNum; i++) // 跳过表头
                {
                    var r = sheetAddr.GetRow(i); if (r == null) continue;

                    var info = new RegisterAddrInfo
                    {
                        AddressDec = uint.Parse(formatter.FormatCellValue(r.GetCell(0))),
                        AddressHex = formatter.FormatCellValue(r.GetCell(1)).PadLeft(4, '0'),
                        Category = formatter.FormatCellValue(r.GetCell(2)),
                        SubCategory = formatter.FormatCellValue(r.GetCell(3)),
                        Name = formatter.FormatCellValue(r.GetCell(4)),
                        RW = formatter.FormatCellValue(r.GetCell(5)),
                        ResetValue = formatter.FormatCellValue(r.GetCell(6)),
                    };
                    addrDictByName[info.Name] = info;
                }

                /* ---------- Sheet2：寄存器内容表 ---------- */
                var sheetDesc = wb.GetSheet("寄存器内容说明"); // 假设第 2 张
                RegisterMeta current = null;

                for (int i = 1; i <= sheetDesc.LastRowNum; i++)
                {
                    var r = sheetDesc.GetRow(i); if (r == null) continue;
                    string regName = formatter.FormatCellValue(r.GetCell(0)).Trim();
                    string bitStr = formatter.FormatCellValue(r.GetCell(1)).Trim();
                    string desc = formatter.FormatCellValue(r.GetCell(2)).Trim();
                    string remark = formatter.FormatCellValue(r.GetCell(3)).Trim();

                    /* 新寄存器行：第一列非空 */
                    if (!string.IsNullOrEmpty(regName))
                    {
                        if (!addrDictByName.TryGetValue(regName, out var addrInfo))
                        {
                            continue;
                            //throw new Exception($"地址表中未找到寄存器 {regName}"); 
                        }
                        current = new RegisterMeta { AddrInfo = addrInfo };
                        metaList.Add(current);
                    }

                    /* 位域行 */
                    if (current != null && !string.IsNullOrEmpty(bitStr))
                    {
                        var bf = ParseBitField(bitStr, desc, remark);
                        current.AddrInfo.BitFields.Add(bf);
                    }
                }
            }
            return metaList;
        }

        private static readonly Regex _regSpan = new Regex(@"b(?<hi>\d+)\s*-\s*b(?<lo>\d+)", RegexOptions.Compiled);
        private static readonly Regex _regSingle = new Regex(@"b(?<bit>\d+)", RegexOptions.Compiled);
        private static readonly Regex _regPair = new Regex(
    @"(?<val>0x[0-9A-Fa-f]+|[01]{1,8}|[0-9]+)\s*[：:\-,，]?\s*(?<txt>[^；;,\n\r]+)",
    RegexOptions.Compiled);
        private static readonly Regex _regSkipWhole = new Regex(
    @"^\s*(待定|预留)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /* 解析“b31-b24”+备注 */
        private static BitField ParseBitField(string bitStr, string desc, string remark)
        {
            // ==== 1. 解析位宽 ====
            bitStr = bitStr.Trim().ToLowerInvariant();
            int hi, lo;

            if (_regSpan.IsMatch(bitStr))
            {
                var m = _regSpan.Match(bitStr);
                hi = int.Parse(m.Groups["hi"].Value);
                lo = int.Parse(m.Groups["lo"].Value);
            }
            else if (_regSingle.IsMatch(bitStr))
            {
                hi = lo = int.Parse(_regSingle.Match(bitStr).Groups["bit"].Value);
            }
            else
                throw new FormatException($"无法识别位域格式: {bitStr}");

            // ==== 2. 构造 BitField ====
            var bf = new BitField
            {
                StartBit = lo,
                EndBit = hi,
                Desc = desc?.Trim()
            };

            remark = remark?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(remark))
                return bf;                       // 没有备注直接返回

            // ==== 3. 不需处理的关键字 ====
            if (_regSkipWhole.IsMatch(remark))
                return bf;
            /* 2. 备注完全不含 ;； :： - 这几类分隔符 —— 也视为无需解析 */
            int index = remark.IndexOfAny(new[] { ';', '；', '：', ':', '-' });
            if (index < 0)
            {
                return bf;
            }
            // ==== 4. 连续范围 —— 形如 “000-111” ====
            if (Regex.IsMatch(remark, @"^[01]+\s*-\s*[01]+$"))
            {
                var parts = remark.Split('-');
                bf.RangeMin = Convert.ToUInt32(parts[0], 2);
                bf.RangeMax = Convert.ToUInt32(parts[1], 2);
                return bf;
            }

            // ==== 5. 离散取值 ====
            // 先按中文/英文分号“；;”拆块，再逐块匹配值→说明
            var segs = remark.Split(new[] { '；', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segs)
            {
                var m = _regPair.Match(seg);
                if (!m.Success) continue;        // 过滤不符合格式的文字，如 “先写后清除”

                uint val = ParseValue(m.Groups["val"].Value);
                string txt = m.Groups["txt"].Value.Trim();
                bf.Options.Add(new BitOption
                {
                    Value = val,         // 右移到最低位 >> lo
                    Display = txt
                });
            }

            // ==== 6. 额外操作提示 ====
            // 把未被 _regPair 捕获的残余文字收集成 ExtraNote
            bf.ExtraNote = Regex.Replace(remark, _regPair.ToString(), "")
                               .Replace("；", "")
                               .Replace(";", "")
                               .Trim();

            return bf;
        }
        private static uint ParseValue(string raw)
        {
            raw = raw.Trim();
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(raw.Substring(2), 16);
            if (Regex.IsMatch(raw, @"^[01]+$"))   // 全 0/1 视为二进制
                return Convert.ToUInt32(raw, 2);
            return Convert.ToUInt32(raw);         // 其余按十进制
        }
    }
}
