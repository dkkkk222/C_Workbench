using PPEC.Communication;
using PPEC.Communication.Model;
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
using Workbench.ViewModels.dw;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for BatchParamsView.xaml
    /// </summary>
    public partial class BatchParamsView : UserControl
    {
        public BatchParamsView()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as BatchParamsViewModel;
            viewModel.InitCategoryTree();
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
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var dataGridRow = FindVisualParent<DataGridRow>(textBox);
            if (dataGridRow == null) return;

            var rowDctx = dataGridRow.DataContext as RegisterAddrInfo;
            if (rowDctx == null) return;

            string binaryStr = string.Join("", rowDctx.BinaryList.Select(t => t.Value));
            rowDctx.BinaryStr = binaryStr;
            uint dec = Utility.BinaryToDec(binaryStr);
            rowDctx.DecValue = dec;

            rowDctx.HexValue = Utility.DecToHex(dec);
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                // Recurse up the tree
                return FindVisualParent<T>(parentObject);
            }
        }

        private void DecValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;

            var rowDctx = element.DataContext as RegisterAddrInfo;
            if (rowDctx == null) return;

            var dec = rowDctx.DecValue;

            rowDctx.HexValue = Utility.DecToHex(dec);

            var tuple = Utility.ParseDecToBinary(dec);
            rowDctx.BinaryStr = tuple.binaryString;

            var charArr = tuple.binaryString.ToCharArray();
            var length = charArr.Length;
            rowDctx.BinaryList.Clear();
            for (int i = 0; i < length; i++)
            {
                rowDctx.BinaryList.Add(new BitOption { Value = (uint)Char.GetNumericValue(charArr[i]), Display = (length - 1 - i).ToString() });
            }
        }

        private void HexValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;

            var rowDctx = element.DataContext as RegisterAddrInfo;
            if (rowDctx == null) return;
            var hex = rowDctx.HexValue;

            rowDctx.DecValue = Utility.ParseHexToUInt(hex);

            var tuple = Utility.ParseDecToBinary(rowDctx.DecValue);
            rowDctx.BinaryStr = tuple.binaryString;

            var charArr = tuple.binaryString.ToCharArray();
            var length = charArr.Length;
            rowDctx.BinaryList.Clear();
            for (int i = 0; i < length; i++)
            {
                rowDctx.BinaryList.Add(new BitOption { Value = (uint)Char.GetNumericValue(charArr[i]), Display = (length - 1 - i).ToString() });
            }
        }
    }
}
