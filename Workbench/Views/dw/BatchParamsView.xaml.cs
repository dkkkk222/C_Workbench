using PPEC.Communication;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Workbench.Models;
using Workbench.ViewModels.dw;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for BatchParamsView.xaml
    /// </summary>
    public partial class BatchParamsView : UserControl
    {
        private const double PaneWidth = 324.0;
        public BatchParamsView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                var vm = DataContext as INotifyPropertyChanged;
                ApplyInitialWidth();

                // 监听 VM 属性变化（IsLeftOpen 改变时播放动画）
                if (vm != null)
                {
                    vm.PropertyChanged -= VmOnPropertyChanged;
                    vm.PropertyChanged += VmOnPropertyChanged;
                }

                // DataContext 变化时重新订阅
                DataContextChanged += (_, __) =>
                {
                    var oldVm = vm;
                    if (oldVm != null)
                        oldVm.PropertyChanged -= VmOnPropertyChanged;

                    vm = DataContext as INotifyPropertyChanged;
                    if (vm != null)
                        vm.PropertyChanged += VmOnPropertyChanged;

                    ApplyInitialWidth();
                };
            };
        }

        #region 展开收起动画
        private void VmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsLeftOpen")
            {
                Dispatcher.Invoke(() =>
                {
                    bool isOpen = GetIsLeftOpen();
                    AnimatePane(isOpen);
                });
            }
        }

        private bool GetIsLeftOpen()
        {
            // 你的 ViewModel 里 bool 属性名为 IsLeftOpen
            dynamic vm = DataContext;
            try { return (bool)(vm?.IsLeftOpen ?? false); }
            catch { return false; }
        }

        private void ApplyInitialWidth()
        {
            LeftCol.Width = GetIsLeftOpen()
                ? new GridLength(PaneWidth, GridUnitType.Pixel)
                : new GridLength(0, GridUnitType.Pixel);
        }

        private void AnimatePane(bool open)
        {
            double from = LeftCol.ActualWidth;                  // 当前实际宽度
            double to = open ? PaneWidth : 0.0;               // 目标宽度

            if (Math.Abs(from - to) < 0.5)                      // 已在目标值，就不动画
            {
                LeftCol.Width = new GridLength(to, GridUnitType.Pixel);
                return;
            }

            var anim = new GridLengthAnimation
            {
                From = new GridLength(from, GridUnitType.Pixel),
                To = new GridLength(to, GridUnitType.Pixel),
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // 关键：FillBehavior=Stop，动画完成后手动落到终值，避免“保持值”占坑
            Timeline.SetDesiredFrameRate(anim, 60);
            anim.FillBehavior = FillBehavior.Stop;
            anim.Completed += (s, e) =>
            {
                // 落到终值并清除动画
                LeftCol.Width = new GridLength(to, GridUnitType.Pixel);
                LeftCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
            };

            LeftCol.BeginAnimation(ColumnDefinition.WidthProperty, anim);
        }
        #endregion
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


        private async void NumericUpDown_LostFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var bf = fe.DataContext as BitField;

            var viewModel = DataContext as BatchParamsViewModel;
            if (viewModel.WriteCurrentRegister == null)
                return;

            var tbx = sender as HandyControl.Controls.NumericUpDown;
            var value = (int)tbx.Value;
            var text = Utility.DecToFixedWidthBinary(value, bf.Length);
            if (!string.IsNullOrEmpty(text))
            {
                //var result = text.PadLeft(bf.Length, '0');
                //bf.WriteBinary = result;
               await viewModel.UpdateWriteRegister(bf.Name, bf.EndBit, bf.StartBit, text);
            }
        }
        private async void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var bf = fe.DataContext as BitField;

            var viewModel = DataContext as BatchParamsViewModel;
            if (viewModel.WriteCurrentRegister == null)
                return;

            var tbx = sender as TextBox;
            var text = tbx.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var result = text.PadLeft(bf.Length, '0');
                bf.WriteBinary = result;
                await viewModel.UpdateWriteRegister(bf.Name, bf.EndBit, bf.StartBit, result);
            }
        }
        private void SequenceGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 每次行被实现/重用时设置正确的行号（0 基 + 1）
            e.Row.Header = e.Row.GetIndex() + 1;
        }
        private void SequenceGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // 排序后行索引会改变——让 DataGrid 排序完成再刷新行头
            e.Handled = false; // 交给默认排序
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var grid = (DataGrid)sender;
                // 更新已实现的行的行头（虚拟化场景足够用）
                for (int i = 0; i < grid.Items.Count; i++)
                {
                    var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(i);
                    if (row != null) row.Header = i + 1;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
