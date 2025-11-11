using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using PPEC.Communication.Model;

namespace Workbench.Models
{
    public class ExcelExportService
    {
        public byte[] ExportTelemetryCodes(List<TelemetryCode> telemetryCodes)
        {
            // 创建工作簿
            IWorkbook workbook = new HSSFWorkbook();

            // 创建样式
            var headerStyle = CreateHeaderStyle(workbook);
            var dataStyle = CreateDataStyle(workbook);

            // 处理间接指令 (Type = "0")
            var indirectCommands = telemetryCodes.Where(x => x.Type == "0").ToList();
            if (indirectCommands.Any())
            {
                CreateIndirectCommandSheet(workbook, indirectCommands, headerStyle, dataStyle);
            }

            // 处理注数指令 (Type = "1")
            var directCommands = telemetryCodes.Where(x => x.Type == "1").ToList();
            if (directCommands.Any())
            {
                CreateDirectCommandSheet(workbook, directCommands, headerStyle, dataStyle);
            }

            // 如果没有数据，创建一个空的工作簿
            if (workbook.NumberOfSheets == 0)
            {
                workbook.CreateSheet("无数据");
            }

            // 转换为字节数组
            using (var memoryStream = new MemoryStream())
            {
                workbook.Write(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private void CreateIndirectCommandSheet(IWorkbook workbook, List<TelemetryCode> commands, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("间接指令");

            // 创建表头
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = { "编号", "指令名称", "指令码", "指令类型" };

            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(i, 15 * 256); // 设置列宽
            }

            // 填充数据
            for (int i = 0; i < commands.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var command = commands[i];

                // 编号 (从1开始)
                ICell cell0 = dataRow.CreateCell(0);
                cell0.SetCellValue(i + 1);
                cell0.CellStyle = dataStyle;

                // 指令名称
                ICell cell1 = dataRow.CreateCell(1);
                cell1.SetCellValue(command.Name ?? "");
                cell1.CellStyle = dataStyle;

                // 指令码
                ICell cell2 = dataRow.CreateCell(2);
                cell2.SetCellValue(command.Code ?? "");
                cell2.CellStyle = dataStyle;

                // 指令类型
                ICell cell3 = dataRow.CreateCell(3);
                cell3.SetCellValue("间接指令");
                cell3.CellStyle = dataStyle;
            }

            // 自动调整列宽（可选）
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private void CreateDirectCommandSheet(IWorkbook workbook, List<TelemetryCode> commands, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("注数指令");

            // 创建表头
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = { "编号", "指令名称", "指令长度(byte)", "指令码", "指令类型" };

            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(i, 15 * 256); // 设置列宽
            }

            // 填充数据
            for (int i = 0; i < commands.Count; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                var command = commands[i];

                // 编号 (从1开始)
                ICell cell0 = dataRow.CreateCell(0);
                cell0.SetCellValue(i + 1);
                cell0.CellStyle = dataStyle;

                // 指令名称
                ICell cell1 = dataRow.CreateCell(1);
                cell1.SetCellValue(command.Name ?? "");
                cell1.CellStyle = dataStyle;

                // 指令长度(byte) - 计算Code的长度
                ICell cell2 = dataRow.CreateCell(2);
                int codeLength = string.IsNullOrEmpty(command.Code) ? 0 : command.Code.Length;
                cell2.SetCellValue(codeLength);
                cell2.CellStyle = dataStyle;

                // 指令码
                ICell cell3 = dataRow.CreateCell(3);
                cell3.SetCellValue(command.Code ?? "");
                cell3.CellStyle = dataStyle;

                // 指令类型
                ICell cell4 = dataRow.CreateCell(4);
                cell4.SetCellValue("注数指令");
                cell4.CellStyle = dataStyle;
            }

            // 自动调整列宽（可选）
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;

            IFont font = workbook.CreateFont();
            font.FontName = "微软雅黑";
            font.FontHeightInPoints = 11;
            font.Boldweight = (short)FontBoldWeight.Bold;
            style.SetFont(font);

            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.Alignment = HorizontalAlignment.Center;

            return style;
        }

        private ICellStyle CreateDataStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            IFont font = workbook.CreateFont();
            font.FontName = "微软雅黑";
            font.FontHeightInPoints = 10;
            style.SetFont(font);

            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;

            return style;
        }
    }
}
