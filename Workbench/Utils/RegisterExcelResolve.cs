using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.X509;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Workbench.Utils
{
    public class RegisterExcelResolve
    {
        public List<RegisterMeta> Parse(string filePath)
        {
            List<RegisterMeta> target = new List<RegisterMeta>();
            IWorkbook workbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }
            var addressSheet = workbook.GetSheet("寄存器地址");
            for (int i = addressSheet.FirstRowNum + 1; i <= addressSheet.LastRowNum; i++)
            {
                var row = addressSheet.GetRow(i);
                var meta = new RegisterMeta();
                var decAddress = uint.Parse(row.GetCell(0).NumericCellValue.ToString());
                var info = new RegisterAddrInfo()
                {
                    AddressDec = decAddress,
                    AddressHex = decAddress.ToString("X4"),
                    Category = row.GetCell(2).StringCellValue,
                    SubCategory = row.GetCell(3).StringCellValue,
                    Name = row.GetCell(4).StringCellValue.Trim(),
                    RW = row.GetCell(5).StringCellValue,
                    ResetValue = row.GetCell(6).StringCellValue
                };
                meta.AddrInfo = info;
                target.Add(meta);
            }

            var details = new List<BitField>();
            var detailSheet = workbook.GetSheet("寄存器内容说明");
            for (int i = detailSheet.FirstRowNum + 1; i <= detailSheet.LastRowNum; i++)
            {
                var row = detailSheet.GetRow(i);
                var name = GetCellValue(detailSheet, i, 0);
                var bitTuple = GetBitInfo(row.GetCell(1).StringCellValue);
                var remark = row.GetCell(3).StringCellValue.Replace("；", ";").Replace("：", ":").Replace(" ", "").Replace("\n", "");
                var minMaxTuple = GetMinMax(remark);
                details.Add(new BitField
                {
                    Name = name.Trim(),
                    EndBit = bitTuple.endBit,
                    StartBit = bitTuple.startBit,
                    Desc = row.GetCell(2).StringCellValue,
                    FieldType = GetFieldType(remark),
                    Options = GetFieldOptions(remark),
                    RangeMin = minMaxTuple.min,
                    RangeMax = minMaxTuple.max
                });
            }

            var dic = details.GroupBy(t => t.Name).ToDictionary(t => t.Key, t => t.ToList());

            foreach (var ti in target)
            {
                var name = ti.AddrInfo.Name;
                if (dic.ContainsKey(name))
                {
                    ti.AddrInfo.BitFields = dic[name];
                }
                else
                {
                    List<BitField> NoneBitField = new List<BitField>();
                    NoneBitField.Add(new BitField
                    {
                        Name = name,
                        EndBit = 31,
                        StartBit = 0,
                        Desc = "无",
                        FieldType = "None"
                    });
                    ti.AddrInfo.BitFields = NoneBitField;
                }
            }

            return target;
        }

        public List<RegisterMeta> SDPCParse(string filePath, List<RegisterMeta> target)
        {
            IWorkbook workbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }
           
            var systemPowerSheet = workbook.GetSheet("系统能源状态监控表");
            ParseCellToFormula(systemPowerSheet, target);
            var pwr1Sheet = workbook.GetSheet("PWR1状态监控表");
            ParseCellToFormula(pwr1Sheet, target);
            var pwr2Sheet = workbook.GetSheet("PWR2状态监控表");
            ParseCellToFormula(pwr2Sheet, target);
            var pwr3Sheet = workbook.GetSheet("PWR3状态监控表");
            ParseCellToFormula(pwr3Sheet, target);
            return target;
        }

        private void ParseCellToFormula(ISheet systemPowerSheet,List<RegisterMeta> target)
        {
            for (int i = systemPowerSheet.FirstRowNum + 1; i <= systemPowerSheet.LastRowNum; i++)
            {
                var row = systemPowerSheet.GetRow(i);
                var addressHex = row.GetCell(2).StringCellValue;//寄存器地址HEX
                var AddressList = target.Where(x => x.AddrInfo.AddressHex == addressHex).FirstOrDefault();
                if (AddressList == null)
                    continue;
                var bitSE = GetBitInfo(row.GetCell(5).StringCellValue);//bit位起始
                var showFormula = row.GetCell(6).StringCellValue;
                var formula = GetFormulaParam(showFormula);//公式
                var unit = row.GetCell(8).StringCellValue;//单位

                var selectBitFields = AddressList.AddrInfo.BitFields.Where(x => x.StartBit == bitSE.startBit && x.EndBit == bitSE.endBit).FirstOrDefault();
                if (selectBitFields == null)
                    continue;
                selectBitFields.FormParam.ParamName = showFormula;
                selectBitFields.FormParam.ParamA = formula.a;
                selectBitFields.FormParam.ParamB = formula.b;
                selectBitFields.FormParam.ParamC = formula.c;
                selectBitFields.FormParam.UnitName= unit;
            }
        }
        private (uint? min, uint? max) GetMinMax(string remark)
        {
            uint? min = null;
            uint? max = null;

            if (remark.Contains("-"))
            {
                var arr = remark.Split('-');
                min = Utility.HexToUint(arr[0]);
                max = Utility.HexToUint(arr[1]);
            }

            return (min, max);
        }

        private ObservableCollection<BitOption> GetFieldOptions(string remark)
        {
            var options = new ObservableCollection<BitOption>();
            if (remark.Contains(":") && remark.Contains(";"))
            {
                var group = remark.Split(';');
                foreach (var str in group)
                {
                    if (str.Contains(":"))
                    {
                        var arr = str.Split(':');
                        options.Add(new BitOption
                        {
                            Key = arr[0],
                            Display = $"{arr[0]}:{arr[1]}",
                            Label = arr[1]
                        });
                    }
                }
            }
            return options;
        }

        private string GetFieldType(string remark)
        {
            if (remark.Contains("-")) return FieldType.Range;
            if (remark.Contains(":") && remark.Contains(";")) return FieldType.Option;
            return FieldType.None;
        }

        private (int endBit, int startBit) GetBitInfo(string str)
        {
            int ebit = 0;
            int sbit = 0;
            var r = str.Replace("b", "");
            if (r.Contains("-"))
            {
                var arr = r.Split('-');
                ebit = int.Parse(arr[0]);
                sbit = int.Parse(arr[1]);
            }
            else
            {
                ebit = sbit = int.Parse(r);
            }

            return (ebit, sbit);
        }

        private string GetCellValue(ISheet sheet, int rowIndex, int colIndex)
        {
            // 检查单元格是否在合并区域内
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                CellRangeAddress region = sheet.GetMergedRegion(i);
                if (region.IsInRange(rowIndex, colIndex))
                {
                    // 如果是，则获取该区域左上角的单元格
                    IRow firstRow = sheet.GetRow(region.FirstRow);
                    if (firstRow == null) return "";

                    ICell firstCell = firstRow.GetCell(region.FirstColumn);
                    return firstCell.StringCellValue;
                }
            }

            // 如果不在任何合并区域内，则正常获取
            IRow row = sheet.GetRow(rowIndex);
            if (row == null) return "";

            ICell cell = row.GetCell(colIndex);
            return cell.StringCellValue;
        }

        private (double a, double b, string c) GetFormulaParam(string FormulaParam)
        {
            if (string.IsNullOrWhiteSpace(FormulaParam) || !FormulaParam.Contains("y=", StringComparison.OrdinalIgnoreCase))
                return (1, 0, "0");                         // 默认规则

            // y = 0.0006x - 1.2000   ①系数a   ②运算符op   ③常数b
            const string pattern =
                @"y\s*=\s*(?<a>[+-]?\d*\.?\d+)\s*x\s*(?:(?<op>[+\-*/])\s*(?<b>[+-]?\d*\.?\d+))?";

            var m = Regex.Match(FormulaParam, pattern, RegexOptions.IgnoreCase);
            if (!m.Success)
                throw new FormatException($"无法解析：{FormulaParam}");

            double a = double.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
            string c = m.Groups["op"].Success ? m.Groups["op"].Value[0].ToString() : "0";
            double b = m.Groups["b"].Success ? double.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture) : 0;

            return (a, b, c);
        }
    }
}
