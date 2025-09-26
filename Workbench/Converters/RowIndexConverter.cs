using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Workbench.Converters
{
    public class RowIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] 是数据项 (the item)
            // values[1] 是 DataGrid 控件
            if (values.Length < 2 || !(values[0] is object item) || !(values[1] is DataGrid grid))
            {
                return null;
            }

            // 从 DataGrid 的 Items 集合中查找当前数据项的索引
            int index = grid.Items.IndexOf(item);

            // 如果找到了，返回 "索引 + 1"；否则返回空
            return (index != -1) ? (index + 1).ToString() : string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // 不需要反向转换
            throw new NotImplementedException();
        }
    } 
}
