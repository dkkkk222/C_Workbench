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
    public class BoolToGridLengthStarOrZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool on = value is bool b && b;
            return on ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    // true -> 固定像素；false -> 0
    public class BoolToGridLengthPixelOrZeroConverter : IValueConverter
    {
        public double Pixels { get; set; } = 10; // 默认10px，可在XAML里改
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool on = value is bool b && b;
            return on ? new GridLength(Pixels) : new GridLength(0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
             return Binding.DoNothing;
        }
    }
    public class BoolToTextConverter : IValueConverter
    {
        // ConverterParameter 形如 "查看配置|隐藏配置"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = (parameter as string)?.Split('|') ?? Array.Empty<string>();
            var off = parts.Length > 0 ? parts[0] : "查看配置";
            var on = parts.Length > 1 ? parts[1] : "隐藏配置";
            return (value is bool b && b) ? on : off;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
