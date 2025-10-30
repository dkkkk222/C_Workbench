using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Workbench.Converters
{
    public class IntToStatusConverter : IValueConverter
    {
        // 默认状态映射
        private static readonly Dictionary<string, string> DefaultStatusMapping = new Dictionary<string, string>
        {
            { "0", "间接指令" },
            { "1", "注数指令" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string intValue)
            {
                // 如果提供了参数，使用参数作为自定义映射
                //if (parameter is string customMapping)
                //{
                //    return GetCustomStatus(intValue, customMapping);
                //}

                // 使用默认映射
                if (DefaultStatusMapping.TryGetValue(intValue, out string status))
                {
                    return status;
                }

                return $"未知状态({intValue})";
            }

            return "无效值";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetCustomStatus(string value, string mappingType)
        {
            // 根据不同的映射类型返回不同的状态
            switch (mappingType.ToLower())
            {
                case "device":
                    return value switch
                    {
                        "1" => "运行中",
                        "2" => "停止",
                        "3" => "故障",
                        "4" => "维护",
                        _ => $"未知设备状态({value})"
                    };

                case "alarm":
                    return value switch
                    {
                        "1" => "正常",
                        "2" => "警告",
                        "3" => "严重警告",
                        "4" => "紧急",
                        _ => $"未知报警状态({value})"
                    };

                default:
                    return DefaultStatusMapping.TryGetValue(value, out string status) ? status : $"未知状态({value})";
            }
        }
    }
}
