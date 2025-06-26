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

namespace Common.Controls
{
    /// <summary>
    /// TitleBar.xaml 的交互逻辑
    /// </summary>
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        public object Title
        {
            get { return (object)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(object), typeof(TitleBar), new PropertyMetadata(null));

        public bool ShowMaximize
        {
            get { return (bool)GetValue(ShowMaximizeProperty); }
            set { SetValue(ShowMaximizeProperty, value); }
        }

        public static readonly DependencyProperty ShowMaximizeProperty =
            DependencyProperty.Register("ShowMaximize", typeof(bool), typeof(TitleBar), new PropertyMetadata(true));

        public bool ShowMinimize
        {
            get { return (bool)GetValue(ShowMinimizeProperty); }
            set { SetValue(ShowMinimizeProperty, value); }
        }

        public static readonly DependencyProperty ShowMinimizeProperty =
            DependencyProperty.Register("ShowMinimize", typeof(bool), typeof(TitleBar), new PropertyMetadata(true));

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void Minimum_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(Window.GetWindow(this));

        }

        private void Maximum_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(parentWindow);
            else
                SystemCommands.MaximizeWindow(parentWindow);
        }
    }
}
