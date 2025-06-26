using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Workbench.Utils.ConverterUtil
{
    public class BoolToHighLowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将value转换为整数
            if (value is int intValue)
            {
                return intValue == 0 ? "低" : "高";
            }

            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 从"是"/"否"转换回整数值
            if (value is string stringValue)
            {
                return stringValue == "高" ? 1 : 0;
            }

            return 0; // 或者抛出异常或返回一个标记错误或未知值的结果
        }
    }
}
