using PPEC.Communication;
using PPEC.Communication.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Workbench.Utils;
using Workbench.ViewModels.dw;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for SingleParamsView.xaml
    /// </summary>
    public partial class SingleParamsView : UserControl
    {
        public SingleParamsView()
        {
            InitializeComponent();
        }
        private static readonly Regex _hexRegex = new Regex("^[0-9a-fA-F]+$",
                                                    RegexOptions.Compiled);
        private void HexBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_hexRegex.IsMatch(e.Text);   // 只要有 1 个非法字符就拦截
        }
        private void HexValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;
            try
            {
                var u = Utility.ParseHexToUInt(text);
                var viewModel = DataContext as SingleParamsViewModel;
                if (viewModel.CurrentRegister == null)
                    return;
                if (viewModel.CurrentRegister.DecValue != u)
                {
                    viewModel.CurrentRegister.DecValue = u;
                    textBox.Text = "0x" + viewModel.CurrentRegister.DecValue.ToString("X8");
                }

                var bs = Utility.BinaryToDec(viewModel.CurrentRegister.BinaryStr);
                if (u != bs)
                {
                    var tuple = Utility.ParseDecToBinary(u);
                    viewModel.CurrentRegister.BinaryStr = tuple.binaryString;
                    var list = tuple.binaryArray.Select(t => new ObservableCollection<BitOption>(t));
                    viewModel.CurrentRegister.BinaryArray.Clear();
                    viewModel.CurrentRegister.BinaryArray.AddRange(list);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("输入值异常");
                return;
            }
         
            
        }

        private void DecValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;
            var dec = viewModel.CurrentRegister.DecValue;
            var ui = Utility.ParseHexToUInt(viewModel.CurrentRegister.HexValue);
            if (dec != ui)
            {
                viewModel.CurrentRegister.HexValue = Utility.DecToHex(dec);
            }

            var bs = Utility.BinaryToDec(viewModel.CurrentRegister.BinaryStr);
            if (dec != bs)
            {
                var tuple = Utility.ParseDecToBinary(dec);
                viewModel.CurrentRegister.BinaryStr = tuple.binaryString;
                var list = tuple.binaryArray.Select(t => new ObservableCollection<BitOption>(t));
                viewModel.CurrentRegister.BinaryArray.Clear();
                viewModel.CurrentRegister.BinaryArray.AddRange(list);
            }
        }

        private void BinaryTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // ✅ 只允许输入 0 或 1
            if (e.Text == "0" || e.Text == "1")
            {
                // 替换为当前输入字符
                textBox.Text = e.Text;
                textBox.CaretIndex = 1;
            }
            else
            {
                textBox.Text = textBox.Text.Replace(e.Text, "");
            }

            // ❗❗ 不管是不是合法字符，都标记为已处理（禁止系统自动插入）
            e.Handled = true;
        }
        private void BinaryTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pasteText = (string)e.DataObject.GetData(typeof(string));
                if (pasteText == "0" || pasteText == "1")
                {
                    var textBox = sender as TextBox;
                    textBox.Text = pasteText;
                    textBox.CaretIndex = 1;
                }
            }

            // 无论是否处理，禁止系统默认粘贴行为
            e.CancelCommand();
        }
        private void BinaryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;

            var arr = viewModel.CurrentRegister.BinaryArray;
            var binaryStr = Utility.BinaryArrayToString(arr);
            viewModel.CurrentRegister.BinaryStr = binaryStr;

            //更新Dec
            var dec = Utility.BinaryToDec(binaryStr);
            if (dec != viewModel.CurrentRegister.DecValue)
            {
                viewModel.CurrentRegister.DecValue = dec;
            }

            //更新Hex
            var hex = Utility.DecToHex(dec);
            if (hex != viewModel.CurrentRegister.HexValue)
            {
                viewModel.CurrentRegister.HexValue = hex;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var bf = fe.DataContext as BitField;

            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;

            var tbx = sender as TextBox;
            var text = tbx.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var result = text.PadLeft(bf.Length, '0');
                bf.WriteBinary = result;
                viewModel.UpdateBinaryString(bf.Name, bf.EndBit, bf.StartBit, result);
            }
        }

        private void NumericUpDown_LostFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var bf = fe.DataContext as BitField;

            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;

            var tbx = sender as HandyControl.Controls.NumericUpDown;
            var value = (int)tbx.Value;
            var text = Utility.DecToFixedWidthBinary(value, bf.Length);
            if (!string.IsNullOrEmpty(text))
            {
                //var result = text.PadLeft(bf.Length, '0');
                //bf.WriteBinary = result;
                viewModel.UpdateBinaryString(bf.Name, bf.EndBit, bf.StartBit, text);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            BindingExpression be = HexValueText.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();
            await Task.Delay(200);
            var viewModel = DataContext as SingleParamsViewModel;
            await viewModel.SendRegister();
        }
    }
}
