using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Workbench.Utils
{
    public class BindingProxy : Freezable
    {
        // 重写这个方法是实现Freezable所必需的
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        // 定义一个依赖属性来存储我们的数据（DataContext）
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}
