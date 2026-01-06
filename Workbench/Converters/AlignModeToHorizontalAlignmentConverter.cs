using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Workbench.Models;

namespace Workbench.Converters
{
    public class AlignModeToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlignMode m)
            {
                return m switch
                {
                    AlignMode.Left => HorizontalAlignment.Left,
                    AlignMode.Right => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center
                };
            }
            return HorizontalAlignment.Center;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class AlignModeToTextAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlignMode m)
            {
                return m switch
                {
                    AlignMode.Left => TextAlignment.Left,
                    AlignMode.Right => TextAlignment.Right,
                    _ => TextAlignment.Center
                };
            }
            return TextAlignment.Center;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
