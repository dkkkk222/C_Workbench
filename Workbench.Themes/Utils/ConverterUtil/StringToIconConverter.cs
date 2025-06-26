using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Workbench.Themes.Utils.ConverterUtil
{
    public class StringToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return Regex.Unescape(StringToUnicode(value.ToString()));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>  
        /// 字符串转为UniCode码字符串  
        /// </summary>  
        public static string StringToUnicode(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                //这里把格式&#xe625; 转为 \ue625
                return s.Replace(@"&#x", @"\u").Replace(";", "");
            }
            return s;
        }
    }
}
