using PPEC.Communication;
using PPEC.Communication.Model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private void HexValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;
            var u = Utility.ParseHexToUInt(text);
            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;
            if (viewModel.CurrentRegister.DecValue != u)
            {
                viewModel.CurrentRegister.DecValue = u;
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

        private void BinaryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 1. 获取事件发送者，并转换为TextBox
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // 2. 获取当前的文本
            string currentText = textBox.Text;

            // 如果文本是空的，我们什么也不做
            if (string.IsNullOrEmpty(currentText))
            {
                return;
            }

            // 3. 检查文本是否只包含 '0' 和 '1'
            bool isValid = true;
            foreach (char c in currentText)
            {
                if (c != '0' && c != '1')
                {
                    isValid = false;
                    break; // 发现一个非法字符就足够了，跳出循环
                }
            }

            // 4. 如果文本不合法，则执行修正操作
            if (!isValid)
            {
                // !!! 关键步骤：避免无限循环 !!!
                // a. 先取消订阅TextChanged事件
                textBox.TextChanged -= BinaryTextBox_TextChanged;

                // b. 将文本设置为"0"
                textBox.Text = "0";

                // c. 将光标移动到文本末尾，这是一种好的用户体验
                textBox.CaretIndex = textBox.Text.Length;

                // d. 重新订阅TextChanged事件
                textBox.TextChanged += BinaryTextBox_TextChanged;
            }
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
    }
}
