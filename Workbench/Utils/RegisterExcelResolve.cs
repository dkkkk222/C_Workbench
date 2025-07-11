using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

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
                    Name = row.GetCell(4).StringCellValue,
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
                    Name = name,
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
            }

            return target;
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
    }
}
