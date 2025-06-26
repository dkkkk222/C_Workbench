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
    /// PPEC_TextBox.xaml 的交互逻辑
    /// </summary>
    public partial class PPEC_TextBox : UserControl
    {
        public PPEC_TextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PPEC_TextBox), new PropertyMetadata(""));
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleWidthProperty =
            DependencyProperty.Register("TitleWidth", typeof(double), typeof(PPEC_TextBox), new PropertyMetadata(100d));
        public double TitleWidth
        {
            get { return (double)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty = 
            DependencyProperty.Register("Unit", typeof(string), typeof(PPEC_TextBox));
        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register("Text", typeof(string), typeof(PPEC_TextBox), new PropertyMetadata(default, OnTextChanged, OnTextCoerceValue));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty RetainPlacesProperty = 
            DependencyProperty.Register("RetainPlaces", typeof(int?), typeof(PPEC_TextBox));
        public int? RetainPlaces
        {
            get { return (int?)GetValue(RetainPlacesProperty); }
            set { SetValue(RetainPlacesProperty, value); }
        }

        public new static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height", typeof(double), typeof(PPEC_TextBox), new PropertyMetadata(25d));
        public new double Height
        {
            get { return (double)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static bool _firstExec = false;
        private static object OnTextCoerceValue(DependencyObject d, object obj)
        {
            if (_firstExec)
            {
                _firstExec = false;
                return obj;
            }
            if (d is PPEC_TextBox stb)
            {
                var newVal = stb.ModifyTextValue(obj);
                _firstExec = true;
                d.SetCurrentValue(TextProperty, newVal);
                return newVal;
            }
            return obj;
        }

        private object ModifyTextValue(object obj)
        {
            if (RetainPlaces == null) return obj;
            if (decimal.TryParse(obj.ToString(), out decimal value))
            {
                return value.ToString(GetRetainString(RetainPlaces.Value));
            }
            return obj;
        }

        private string GetRetainString(int places)
        {
            if (places == 0) return "#0";
            StringBuilder sb = new StringBuilder();
            sb.Append("#0.");
            for (int i = 0; i < places; i++)
            {
                sb.Append("0");
            }
            return sb.ToString();
        }
    }
}
