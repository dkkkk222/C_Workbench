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

        //private void BinaryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    if (textBox == null) return;

        //    textBox.TextChanged -= BinaryTextBox_TextChanged;

        //    string input = textBox.Text;

        //    // 从后往前找，找到最后一个 '0' 或 '1'
        //    char finalChar = '0'; // 默认 fallback
        //    bool found = false;

        //    for (int i = input.Length - 1; i >= 0; i--)
        //    {
        //        if (input[i] == '0' || input[i] == '1')
        //        {
        //            finalChar = input[i];
        //            found = true;
        //            break;
        //        }
        //    }

        //    // 如果没找到合法字符，设为 '0'
        //    textBox.Text = found ? finalChar.ToString() : "0";

        //    textBox.CaretIndex = 1; // 设置光标在末尾
        //    textBox.TextChanged += BinaryTextBox_TextChanged;
        //}
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
                textBox.Text=textBox.Text.Replace(e.Text, "");
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

        private async void DecValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;

            var rowDctx = element.DataContext as RegisterAddrInfo;
            if (rowDctx == null) return;

            var dec = rowDctx.DecValue;

            rowDctx.HexValue = Utility.DecToHex(dec);

            var tuple = Utility.ParseDecToBinary(dec);
            rowDctx.BinaryStr = tuple.binaryString;

            var newList = await Task.Run(() =>
            {
                var charArr = tuple.binaryString.ToCharArray();
                var length = charArr.Length;

                var list = new List<BitOption>(length);
                for (int i = 0; i < length; i++)
                {
                    list.Add(new BitOption
                    {
                        Value = (uint)Char.GetNumericValue(charArr[i]),
                        Display = (length - 1 - i).ToString()
                    });
                }
                return list;
            });
            rowDctx.BinaryList.Clear();
            foreach (var item in newList)
            {
                rowDctx.BinaryList.Add(item);
            }
        }


        private async void HexValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;

            var rowDctx = element.DataContext as RegisterAddrInfo;
            if (rowDctx == null) return;
            var hex = rowDctx.HexValue;
            try
            {
                rowDctx.DecValue = Utility.ParseHexToUInt(hex);

                var tuple = Utility.ParseDecToBinary(rowDctx.DecValue);
                rowDctx.BinaryStr = tuple.binaryString;

                var newList = await Task.Run(() =>
                {
                    var charArr = tuple.binaryString.ToCharArray();
                    var length = charArr.Length;

                    var list = new List<BitOption>(length);
                    for (int i = 0; i < length; i++)
                    {
                        list.Add(new BitOption
                        {
                            Value = (uint)Char.GetNumericValue(charArr[i]),
                            Display = (length - 1 - i).ToString()
                        });
                    }
                    return list;
                });
                rowDctx.BinaryList.Clear();
                foreach (var item in newList)
                {
                    rowDctx.BinaryList.Add(item);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("输入值异常");
                return;
            }
                
        }
    }
}
