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

namespace Workbench.Controls.Controls.Title
{
    /// <summary>
    /// StandTitle.xaml 的交互逻辑
    /// </summary>
    public partial class StandTitle : UserControl
    {
        public StandTitle()
        {
            InitializeComponent();
        }
        #region property
        public static readonly DependencyProperty StandTextProperty = DependencyProperty.Register(
               "StandText", typeof(string), typeof(StandTitle),
               new PropertyMetadata(""));

        public string StandText
        {
            get => (string)GetValue(StandTextProperty);

            set => SetValue(StandTextProperty, value);
        }

        public static readonly DependencyProperty StandFontSizeProperty = DependencyProperty.Register(
               "StandFontSize", typeof(double), typeof(StandTitle),
               new PropertyMetadata(Convert.ToDouble(16)));

        public double StandFontSize
        {
            get => (double)GetValue(StandFontSizeProperty);

            set => SetValue(StandFontSizeProperty, value);
        }

        public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register(
               "TextWidth", typeof(double), typeof(StandTitle),
               new PropertyMetadata(Convert.ToDouble(3)));

        public double TextWidth
        {
            get => (double)GetValue(TextWidthProperty);

            set => SetValue(TextWidthProperty, value);
        }

        public static readonly DependencyProperty TextHeightProperty = DependencyProperty.Register(
               "TextHeight", typeof(double), typeof(StandTitle),
               new PropertyMetadata(Convert.ToDouble(20)));

        public double TextHeight
        {
            get => (double)GetValue(TextHeightProperty);

            set => SetValue(TextHeightProperty, value);
        }


        public static readonly DependencyProperty BordMarginProperty = DependencyProperty.Register(
               "BordMargin", typeof(Thickness), typeof(StandTitle),
               new PropertyMetadata(new Thickness(0, 0, 0, 0)));

        public Thickness BordMargin
        {
            get => (Thickness)GetValue(BordMarginProperty);

            set => SetValue(BordMarginProperty, value);
        }

        public static readonly DependencyProperty TextMarginProperty = DependencyProperty.Register(
               "TextMargin", typeof(Thickness), typeof(StandTitle),
               new PropertyMetadata(new Thickness(0, 0, 0, 0)));

        public Thickness TextMargin
        {
            get => (Thickness)GetValue(TextMarginProperty);

            set => SetValue(TextMarginProperty, value);
        }
        #endregion
    }
}
