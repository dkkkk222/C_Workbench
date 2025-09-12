using PPEC.Communication;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private GridLength _lastRightWidth = new GridLength(1, GridUnitType.Star);
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
            if (e.PropertyName == "IsConfigPaneOpen")
            {
                // 右侧详情开合：不用动画，直接设置列宽
                Dispatcher.Invoke(() =>
                {
                    bool open = GetIsConfigPaneOpen();
                    SetRightPane(open);
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
        private bool GetIsConfigPaneOpen()
        {
            dynamic vm = DataContext;
            try { return (bool)(vm?.IsConfigPaneOpen ?? false); }
            catch { return false; }
        }
        private void ApplyInitialWidth()
        {
            LeftCol.Width = GetIsLeftOpen()
                ? new GridLength(PaneWidth, GridUnitType.Pixel)
                : new GridLength(0, GridUnitType.Pixel);

            // 右侧（根据 VM 当前状态）
            SetRightPane(GetIsConfigPaneOpen());
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

        /// <summary>
        /// 根据开关设置右侧详情与分隔列宽度，并记忆用户上次拖动的宽度
        /// </summary>
        private void SetRightPane(bool open)
        {
            if (RightCol == null || SepCol == null) return;

            if (open)
            {
                // 展开：恢复上次宽度（首次用 *）
                if (_lastRightWidth.Value <= 0)
                {
                    _lastRightWidth = new GridLength(1, GridUnitType.Star);
                }

                RightCol.Width = _lastRightWidth;
                SepCol.Width = new GridLength(10); // 分隔列
            }
            else
            {
                // 收起：先记住当前宽度，再置 0
                _lastRightWidth = RightCol.Width;
                RightCol.Width = new GridLength(0);
                SepCol.Width = new GridLength(0);
            }
        }

        /// <summary>
        /// 分隔条拖动完成后，记住右侧列当前宽度，供下次展开还原
        /// </summary>
        private void DetailSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (RightCol != null && RightCol.Width.Value > 0)
            {
                _lastRightWidth = RightCol.Width; // 很可能是像素宽度
            }
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

        #region DataGridAllKey
        private bool _cornerFixed;
        private INotifyCollectionChanged _itemsNotify;
        private EventHandler _statusChangedHandler;
        private void SequenceGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            // 1) 订阅 ItemsSource 的集合变化（增删行时触发）
            HookItemsSource();
            _statusChangedHandler = (s, __) =>
            {
                if (SequenceGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    TrySetCornerHeader();
            };
            // 2) 行容器生成完成后再试（虚拟化/主题下常见）
            SequenceGrid.ItemContainerGenerator.StatusChanged += _statusChangedHandler;

            // 3) 再加一个布局兜底（模板晚创建的主题场景）
            SequenceGrid.LayoutUpdated += SequenceGrid_LayoutUpdated;

            // 初次尝试（若加载时就有行）
            TrySetCornerHeader();
        }
       
        private void SequenceGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if (_cornerFixed) return;
            TrySetCornerHeader();
        }

        private void HookItemsSource()
        {
            if (_itemsNotify != null)
                _itemsNotify.CollectionChanged -= Items_CollectionChanged;

            _itemsNotify = SequenceGrid.ItemsSource as INotifyCollectionChanged;
            if (_itemsNotify != null)
                _itemsNotify.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 集合变更后延后尝试（确保可视元素已创建）
            Dispatcher.BeginInvoke(new Action(TrySetCornerHeader),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void TrySetCornerHeader()
        {
            if (_cornerFixed) return;

            SequenceGrid.ApplyTemplate();

            // 不依赖具体名字，直接在 DataGrid 的可视树里找“左上角那颗按钮”
            // 1) 先找 DataGrid 的 ColumnHeadersPresenter
            var headersPresenter = FindDescendant<DataGridColumnHeadersPresenter>(SequenceGrid);
            if (headersPresenter == null) return;

            // 2) 再向上找它所在的 Grid（一般左上角按钮与它同级）
            var parent = VisualTreeHelper.GetParent(headersPresenter);
            if (parent == null) return;

            // 3) 在这个容器里找第一个 ButtonBase，当作左上角按钮
            var btn = FindDescendant<ButtonBase>(parent);
            if (btn == null) return; // 还没创建出来，再等下一次
            btn.Template = BuildCornerButtonTemplate(btn.GetType());
            // 设置文本与样式
            //btn.Content = "序号";
            // 若不想触发全选，放开任一行：
             btn.IsHitTestVisible = false;
            // btn.IsEnabled = false;

            _cornerFixed = true;

            SequenceGrid.ItemContainerGenerator.StatusChanged -= _statusChangedHandler;
            SequenceGrid.LayoutUpdated -= SequenceGrid_LayoutUpdated; // 设到位后取消兜底
        }

        // --- 可视树工具 ---
        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var deep = FindDescendant<T>(child);
                if (deep != null) return deep;
            }
            return null;
        }

        private void SequenceGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_itemsNotify != null)
                _itemsNotify.CollectionChanged -= Items_CollectionChanged;
            SequenceGrid.LayoutUpdated -= SequenceGrid_LayoutUpdated;
            SequenceGrid.ItemContainerGenerator.StatusChanged -= null; // 如果你有字段存委托可在此移除
        }

        private ControlTemplate BuildCornerButtonTemplate(Type targetType)
        {
            var tpl = new ControlTemplate(targetType);

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetResourceReference(Border.BackgroundProperty, "DataGridHeadBackgroundColor");
            border.SetResourceReference(Border.BorderBrushProperty, "BorderLineBrush");
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            border.SetValue(Border.SnapsToDevicePixelsProperty, true);

            // 关键：充满单元格
            border.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            // 跟随行头宽度
            var wBind = new Binding("RowHeaderWidth")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1)
            };
            border.SetBinding(FrameworkElement.WidthProperty, wBind);

            // 跟随表头高度（若 ColumnHeaderHeight 未设置则为 NaN，Stretch 也能正常填充；
            // 你若已在 XAML 设置 ColumnHeaderHeight=30，则这里会严格等高）
            var hBind = new Binding("ColumnHeaderHeight")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1)
            };
            border.SetBinding(FrameworkElement.HeightProperty, hBind);

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.TextProperty, "序号");
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            text.SetValue(TextBlock.FontSizeProperty, 13.0);
            text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(text);
            tpl.VisualTree = border;
            return tpl;
        }
        #endregion

        private void SequenceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as BatchParamsViewModel;
            var selItem = SequenceGrid.SelectedItem as RegisterAddrInfo;
            viewModel.ChangeIsConfigPaneOpen(selItem);
        }
    }
}
