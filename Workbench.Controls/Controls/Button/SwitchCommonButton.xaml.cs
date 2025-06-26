using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Workbench.Controls
{
    /// <summary>
    /// SwitchCommonButton.xaml 的交互逻辑
    /// </summary>
    public partial class SwitchCommonButton : UserControl
    {
        public SwitchCommonButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsCheckedProperty =
           DependencyProperty.Register("IsChecked", typeof(bool), typeof(SwitchCommonButton),
               new PropertyMetadata(false));
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        private void CheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // 防止事件冒泡

            CheckBox cb = sender as CheckBox;
            if (cb != null)
            {
                // 切换 CheckBox 的选中状态
                cb.IsChecked = !cb.IsChecked;
            }
        }
    }
}
