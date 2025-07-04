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
    public class RowIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value 是传递过来的 DataGridRow 对象
            if (value is DataGridRow row)
            {
                // GetIndex() 方法返回该行在 DataGrid 中的从0开始的索引
                // 我们+1使其从1开始显示
                return row.GetIndex() + 1;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 不需要反向转换，所以抛出异常
            throw new NotImplementedException();
        }
    }
}
