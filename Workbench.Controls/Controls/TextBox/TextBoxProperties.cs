using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Workbench.Controls.TextBox
{
    public class TextBoxProperties
    {
        public static readonly DependencyProperty IsNumericOnlyProperty = DependencyProperty.RegisterAttached("IsNumericOnly", typeof(bool), typeof(TextBoxProperties), new PropertyMetadata(false, OnIsNumericOnlyChanged));

        public static readonly DependencyProperty MinimumValueProperty = DependencyProperty.RegisterAttached("MinimumValue", typeof(double?), typeof(TextBoxProperties));

        public static readonly DependencyProperty MaximumValueProperty = DependencyProperty.RegisterAttached("MaximumValue", typeof(double?), typeof(TextBoxProperties));

        public static readonly DependencyProperty RetainPlacesProperty = DependencyProperty.RegisterAttached("RetainPlaces", typeof(int?), typeof(TextBoxProperties));

        public static bool GetIsNumericOnly(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNumericOnlyProperty);
        }

        public static void SetIsNumericOnly(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNumericOnlyProperty, value);
        }

        public static double? GetMinimumValue(DependencyObject obj)
        {
            return (double?)obj.GetValue(MinimumValueProperty);
        }

        public static void SetMinimumValue(DependencyObject obj, double? value)
        {
            obj.SetValue(MinimumValueProperty, value);
        }

        public static double? GetMaximumValue(DependencyObject obj)
        {
            return (double?)obj.GetValue(MaximumValueProperty);
        }

        public static void SetMaximumValue(DependencyObject obj, double? value)
        {
            obj.SetValue(MaximumValueProperty, value);
        }

        public static int? GetRetainPlaces(DependencyObject obj)
        {
            return (int?)obj.GetValue(RetainPlacesProperty);
        }

        public static void SetRetainPlaces(DependencyObject obj, int? value)
        {
            obj.SetValue(RetainPlacesProperty, value);
        }

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.LostFocus += TextBox_LostFocus; ;
                    DataObject.AddPastingHandler(textBox, TextBox_OnPaste);
                }
                else
                {
                    textBox.LostFocus -= TextBox_LostFocus;
                    DataObject.RemovePastingHandler(textBox, TextBox_OnPaste);
                }
            }
        }

        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tbx)
            {
                if (tbx.IsReadOnly) return;
                var max = GetMaximumValue(tbx);
                var min = GetMinimumValue(tbx);
                //var retainPlaces = GetRetainPlaces(tbx);
                var text = tbx.Text;
                if (!IsNumericInput(text))
                {
                    //e.Handled = true;
                    if (min != null)
                    {
                        tbx.Text = min.ToString();
                    }
                    else
                    {
                        tbx.Text = "0";
                    }
                }
                else
                {
                    var res = decimal.Parse(text);
                    tbx.Text = res.ToString();
                    if (max != null && res > (decimal)max)
                    {
                        //e.Handled = true;
                        tbx.Text = max.ToString();
                    }

                    if (min != null && res < (decimal)min)
                    {
                        //e.Handled = true;
                        tbx.Text = min.ToString();
                    }
                }
                //if (retainPlaces != null) {
                //    tbx.Text = decimal.Parse(tbx.Text).ToString(GetRetainString(retainPlaces.Value));
                //}
            }
        }

        private static string GetRetainString(int places)
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

        private static void TextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var tbx = (System.Windows.Controls.TextBox)sender;
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                var max = GetMaximumValue(tbx);
                var min = GetMinimumValue(tbx);
                //var retainPlaces = GetRetainPlaces(tbx);
                if (!IsNumericInput(pastedText))
                {
                    e.CancelCommand();
                    tbx.Text = "0";
                }
                else
                {
                    var res = decimal.Parse(pastedText);
                    tbx.Text = res.ToString();
                    if (max != null && res > (decimal)max)
                    {
                        e.CancelCommand();
                        tbx.Text = max.ToString();
                    }

                    if (min != null && res < (decimal)min)
                    {
                        e.CancelCommand();
                        tbx.Text = min.ToString();
                    }
                }
                //if (retainPlaces != null) {
                //    tbx.Text = decimal.Parse(tbx.Text).ToString(GetRetainString(retainPlaces.Value));
                //}
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsNumericInput(string text)
        {
            return Regex.IsMatch(text, @"^-?\d+(\.\d+)?$");
        }
    }


}
