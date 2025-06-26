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
    /// TitleTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class TitleTextBox : UserControl
    {
        public TitleTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty BlockWidthProperty = DependencyProperty.Register("BlockWidth", typeof(double), typeof(TitleTextBox), new PropertyMetadata(0.0));
        public double BlockWidth
        {
            get { return (double)GetValue(BlockWidthProperty); }
            set { SetValue(BlockWidthProperty, value); }
        }

        public static readonly DependencyProperty BlockTextProperty = DependencyProperty.Register("BlockText", typeof(string), typeof(TitleTextBox));
        public string BlockText
        {
            get { return (string)GetValue(BlockTextProperty); }
            set { SetValue(BlockTextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TitleTextBox), new PropertyMetadata(default, OnTextChanged, OnTextCoerceValue));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
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
            if (d is TitleTextBox stb)
            {
                var newVal = stb.ModifyTextValue(obj);
                _firstExec = true;
                //判断精度
                if (stb.Precision > 0)
                {
                    var roundVal = Math.Round(decimal.Parse(newVal.ToString()), stb.Precision, MidpointRounding.AwayFromZero);
                    newVal = roundVal.ToString($"F{stb.Precision}");
                }
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

        public static readonly DependencyProperty TextHeightProperty = DependencyProperty.Register("TextHeight", typeof(double), typeof(TitleTextBox), new PropertyMetadata(25.0));
        public double TextHeight
        {
            get { return (double)GetValue(TextHeightProperty); }
            set { SetValue(TextHeightProperty, value); }
        }

        public new static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(TitleTextBox), new PropertyMetadata(true));
        public new bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(TitleTextBox), new PropertyMetadata(false));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register("Unit", typeof(string), typeof(TitleTextBox));
        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register("TextWidth", typeof(double), typeof(TitleTextBox), new PropertyMetadata(100.0));
        public double TextWidth
        {
            get { return (double)GetValue(TextWidthProperty); }
            set { SetValue(TextWidthProperty, value); }
        }

        public static readonly DependencyProperty DescProperty = DependencyProperty.Register("Desc", typeof(string), typeof(TitleTextBox), new PropertyMetadata(null));
        public string Desc
        {
            get { return (string)GetValue(DescProperty); }
            set { SetValue(DescProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double?), typeof(TitleTextBox));
        public double? Maximum
        {
            get { return (double?)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double?), typeof(TitleTextBox));
        public double? Minimum
        {
            get { return (double?)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty RetainPlacesProperty = DependencyProperty.Register("RetainPlaces", typeof(int?), typeof(TitleTextBox));
        public int? RetainPlaces
        {
            get { return (int?)GetValue(RetainPlacesProperty); }
            set { SetValue(RetainPlacesProperty, value); }
        }

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision", typeof(int), typeof(TitleTextBox), new PropertyMetadata(0));
        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }
    }
}
