using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Workbench.Converters
{
    public class DataGridRowIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var item = values[0];                   // 当前行的数据项
            var grid = values.Length > 1 ? values[1] as DataGrid : null;

            if (item == null || grid == null)
                return string.Empty;

            // 关键：用 DataGrid.Items.IndexOf 获取在“当前视图”里的索引
            int index = grid.Items.IndexOf(item);
            return (index >= 0) ? (index + 1).ToString() : string.Empty; // 1 基
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
