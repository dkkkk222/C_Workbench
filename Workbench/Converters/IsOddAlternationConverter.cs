using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Workbench.Converters
{
    public class IsOddAlternationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 空值/Unset 防守
            if (value == null || value == DependencyProperty.UnsetValue)
                return false;

            int idx;
            if (value is int) idx = (int)value;
            else if (!int.TryParse(value.ToString(), out idx)) idx = 0;

            return (idx % 2) == 1; // 奇数行为 true，偶数行为 false
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
