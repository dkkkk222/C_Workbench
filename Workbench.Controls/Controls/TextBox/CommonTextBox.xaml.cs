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
    /// CommonTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class CommonTextBox : UserControl
    {
        public CommonTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CommonTextBox), new PropertyMetadata(""));
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleWidthProperty =
            DependencyProperty.Register("TitleWidth", typeof(double), typeof(CommonTextBox), new PropertyMetadata(100d));
        public double TitleWidth
        {
            get { return (double)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(CommonTextBox));
        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(CommonTextBox));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty RetainPlacesProperty =
            DependencyProperty.Register("RetainPlaces", typeof(int?), typeof(CommonTextBox));
        public int? RetainPlaces
        {
            get { return (int?)GetValue(RetainPlacesProperty); }
            set { SetValue(RetainPlacesProperty, value); }
        }

        public new static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height", typeof(double), typeof(CommonTextBox), new PropertyMetadata(25d));
        public new double Height
        {
            get { return (double)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }
    }
}
