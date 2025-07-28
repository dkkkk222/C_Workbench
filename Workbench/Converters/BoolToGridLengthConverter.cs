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
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double w = 324; // 展开宽度
            if (parameter != null && double.TryParse(parameter.ToString(), out var p)) w = p;
            return (value is bool b && b) ? new GridLength(w) : new GridLength(0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
