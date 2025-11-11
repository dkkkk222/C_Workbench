using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.X509;
using PPEC.Communication;
using PPEC.Communication.Common;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
                    ti.AddrInfo.BitFields = new ObservableCollection<BitField>(dic[name]);
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
                    ti.AddrInfo.BitFields = new ObservableCollection<BitField>(NoneBitField);
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

        #region 遥测
        public List<TelemetryMeta> Telemetry(string filePath)
        {
            List<TelemetryMeta> listTM = new List<TelemetryMeta>();
            IWorkbook workbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }
            var indirectSheet = workbook.GetSheet("间接指令");
            var IndirectTelemetry = ParseCellToTelemetry(indirectSheet);
            var noteInstructionSheet = workbook.GetSheet("注数指令");
            var NoteInstructionTelemetry = ParseCellToTelemetry2(noteInstructionSheet);
            listTM.AddRange(IndirectTelemetry);
            listTM.AddRange(NoteInstructionTelemetry);
            return listTM;
        }
        public (List<TelemetryMonitAnalysisMeta>, List<TelemetryTag>) TelemetryMonit(string filePath)
        {
            List<TelemetryMonitAnalysisMeta> listTMA = new List<TelemetryMonitAnalysisMeta>();
            List<TelemetryTag> listTag = new List<TelemetryTag>();
            IWorkbook workbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }
            var indirectSheet = workbook.GetSheet("遥测监控表");
            for (int i = indirectSheet.FirstRowNum + 1; i <= indirectSheet.LastRowNum; i++)
            {
                var row = indirectSheet.GetRow(i);
                var name = row.GetCell(1).StringCellValue;//代号
                var code = row.GetCell(2).StringCellValue;//数据位置
                var bit = row.GetCell(3).StringCellValue;//解析内容(bit)
                var bitLen = GetCellValue(indirectSheet,i,4); //row.GetCell(4).StringCellValue;//解析内容(bit长度)
                var showFormula = row.GetCell(5).StringCellValue;//解析要求
                var unit = row.GetCell(6).StringCellValue;//单位
                var formula = GetFormulaParam(showFormula);//公式
         
                var res = FormulaParser.Parse(showFormula);
                string aa = res.ToString();
                TelemetryMonitAnalysisMeta tmam = new TelemetryMonitAnalysisMeta();
                tmam.ShowFormParam = showFormula;
                if (formula.c=="99")
                {
                    tmam.KeyValuePairs = formula.d;
                }
                else
                {
                    tmam.FormParam = res;
                }                    

                tmam.CodeName = name;
                tmam.DateLocation = code;

                tmam.StartLocaltion = GetByteInfo(code, "byte").startBit;
                tmam.EndLocaltion = GetByteInfo(code, "byte").endBit;
                tmam.LocaltionLen = tmam.EndLocaltion - tmam.StartLocaltion+1;

                tmam.BitName = bit;
                tmam.StartBit = GetBitInfo(bit, "b").startBit;
                tmam.EndBit = GetBitInfo(bit, "b").endBit;

                tmam.BitLength = int.Parse(bitLen);

                tmam.Unit = unit;
                listTMA.Add(tmam);
            }
            //var teleTagSheet = workbook.GetSheet("遥测查询标识");
            //if(teleTagSheet!=null)
            //{
            //    for (int i = teleTagSheet.FirstRowNum + 1; i <= teleTagSheet.LastRowNum; i++)
            //    {
            //        var row = teleTagSheet.GetRow(i);
            //        var name = row.GetCell(0).StringCellValue;//标识
            //        TelemetryTag tag= new TelemetryTag();
            //        tag.Name = name;

            //        listTag.Add(tag);
            //    }
            //}
            
                return (listTMA, listTag);
        }
        #endregion

        private void ParseCellToFormula(ISheet systemPowerSheet,List<RegisterMeta> target)
        {
            for (int i = systemPowerSheet.FirstRowNum + 1; i <= systemPowerSheet.LastRowNum; i++)
            {
                var row = systemPowerSheet.GetRow(i);
                var addressHex = row.GetCell(2).StringCellValue;//寄存器地址HEX
                var AddressList = target.Where(x => x.AddrInfo.AddressHex == addressHex).FirstOrDefault();
                if (AddressList == null)
                    continue;
                var bitSE = GetBitInfo(row.GetCell(4).StringCellValue);//bit位起始
                var showFormula = row.GetCell(5).StringCellValue;
                var formula = GetFormulaParam(showFormula);//公式
                var unit = row.GetCell(6).StringCellValue;//单位

                var selectBitFields = AddressList.AddrInfo.BitFields.Where(x => x.StartBit == bitSE.startBit && x.EndBit == bitSE.endBit).FirstOrDefault();
                if (selectBitFields == null)
                    continue;
                selectBitFields.FormParam.ParamName = showFormula;
                selectBitFields.FormParam.ParamA = formula.a;
                selectBitFields.FormParam.ParamB = formula.b;
                selectBitFields.FormParam.ParamC = formula.c;
                selectBitFields.FormParam.UnitName= unit;
                selectBitFields.FormParam.ParamDic = formula.d;
            }
        }

        private List<TelemetryMeta> ParseCellToTelemetry(ISheet systemPowerSheet)
        {
            List<TelemetryMeta> listTM = new List<TelemetryMeta>();
            for (int i = systemPowerSheet.FirstRowNum + 1; i <= systemPowerSheet.LastRowNum; i++)
            {
                var row = systemPowerSheet.GetRow(i);
                var name = row.GetCell(1).StringCellValue;//指令名称
                var code = row.GetCell(2).StringCellValue;//指令码
                var type = row.GetCell(3).StringCellValue;//指令类型
                TelemetryMeta tempTM= new TelemetryMeta();
                tempTM.CommandId = i;
                tempTM.CommandName=name;
                tempTM.CommandCode=code;
                tempTM.CommandType = type == "间接指令" ? TelemetryCommandType.IndirectCommand : TelemetryCommandType.NoteInstruction;
                tempTM.CommandLength = code.Length;
                listTM.Add(tempTM);
            }
            return listTM;
        }

        private List<TelemetryMeta> ParseCellToTelemetry2(ISheet systemPowerSheet)
        {
            List<TelemetryMeta> listTM = new List<TelemetryMeta>();
            for (int i = systemPowerSheet.FirstRowNum + 1; i <= systemPowerSheet.LastRowNum; i++)
            {
                var row = systemPowerSheet.GetRow(i);
                if(row.GetCell(1)==null)
                    continue;
                var name = row.GetCell(1).StringCellValue;//指令名称
                var code = row.GetCell(3).StringCellValue;//指令码
                var type = row.GetCell(4).StringCellValue;//指令类型
                TelemetryMeta tempTM = new TelemetryMeta();
                tempTM.CommandId = i;
                tempTM.CommandName = name;
                tempTM.CommandCode = code;
                tempTM.CommandType = type == "间接指令" ? TelemetryCommandType.IndirectCommand : TelemetryCommandType.NoteInstruction;
                tempTM.CommandLength = code.Length;
                listTM.Add(tempTM);
            }
            return listTM;
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

        private (int endBit, int startBit) GetBitInfo(string str,string replaceStr)
        {
            int ebit = 0;
            int sbit = 0;
            var r = str.Replace(replaceStr, "");
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

        private (int endBit, int startBit) GetByteInfo(string str, string replaceStr)
        {
            int ebit = 0;
            int sbit = 0;
            var r = str.Replace(replaceStr, "");
            if (r.Contains("-"))
            {
                var arr = r.Split('-');
                ebit = int.Parse(arr[1]);
                sbit = int.Parse(arr[0]);
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
            return GetCellValueAsString(cell);// cell.StringCellValue;
        }

        private string GetCellValueAsString(ICell cell)
        {
            if (cell == null) return "";

            try
            {
                // 使用ToString()方法，NPOI会自动处理各种类型
                return cell.ToString()?.Trim() ?? "";
            }
            catch
            {
                // 如果ToString失败，使用备用方法
                switch (cell.CellType)
                {
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString();
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.String:
                        return cell.StringCellValue?.Trim() ?? "";
                    default:
                        return "";
                }
            }
        }

        private (double a, double b, string c, Dictionary<string, string> d) GetFormulaParam(string FormulaParam)
        {
            if (string.IsNullOrWhiteSpace(FormulaParam) || !FormulaParam.Contains("y=", StringComparison.OrdinalIgnoreCase))
            {
                var returnd= UtilHelper.ParseExcelDataToDictionary(FormulaParam);
                string returnC = "0";
                if(returnd!=null)
                {
                    returnC = "99";
                }
                return (1, 0, returnC, returnd);                         // 默认规则
            }
                

            // y = 0.0006x - 1.2000   ①系数a   ②运算符op   ③常数b
            const string pattern =
                @"y\s*=\s*(?<a>[+-]?\d*\.?\d+)\s*x\s*(?:(?<op>[+\-*/])\s*(?<b>[+-]?\d*\.?\d+))?";

            var m = Regex.Match(FormulaParam, pattern, RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return (-1, -1, "-1", null);
                //throw new FormatException($"无法解析：{FormulaParam}");
            }
                

            double a = double.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
            string c = m.Groups["op"].Success ? m.Groups["op"].Value[0].ToString() : "0";
            double b = m.Groups["b"].Success ? double.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture) : 0;

            return (a, b, c, null);
        }
    }
}
