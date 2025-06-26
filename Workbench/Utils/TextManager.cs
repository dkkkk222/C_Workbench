using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Workbench.Utils
{
    public class TextManager
    {
        public static readonly string InitializeText = "正在初始化";
        public const string IconfontUri = "pack://application:,,,/Workbench.Themes;component/Resource/Fonts/#iconfont";

        /// <summary>
        /// 创建IconFont
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public static TextBlock CreateIconFont(string unicode)
        {
            var tbl = new TextBlock();
            tbl.Text = unicode; //\xe631
            tbl.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), IconfontUri);
            tbl.VerticalAlignment = VerticalAlignment.Center;
            tbl.HorizontalAlignment = HorizontalAlignment.Center;
            tbl.FontSize = 16;
            return tbl;
        }
    }
}
