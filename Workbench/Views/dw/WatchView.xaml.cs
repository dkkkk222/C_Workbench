using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Workbench.Models;
using Workbench.ViewModels.dw;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for WatchView.xaml
    /// </summary>
    public partial class WatchView : UserControl
    {
        private const double PaneWidth = 324.0;
        public WatchView()
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
                if (DataContext is WatchViewModel viewModelOb)
                {
                    // 启动时从 VM 恢复
                    if (viewModelOb.SplitterPositionLeft.Value > 0)
                        SplitterPositionLeft.Width = viewModelOb.SplitterPositionLeft;

                    if (viewModelOb.SplitterPositionRight.Value > 0)
                        SplitterPositionRight.Width = viewModelOb.SplitterPositionRight;

                    if (viewModelOb.SplitterPositionUp.Value > 0)
                        SplitterPositionUp.Height = viewModelOb.SplitterPositionUp;

                    if (viewModelOb.SplitterPositionDown.Value > 0)
                        SplitterPositionDown.Height = viewModelOb.SplitterPositionDown;
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

        private void TabItem_OnRightClickSelect(object sender, MouseButtonEventArgs e)
        {
            if (sender is TabItem ti)
                ti.IsSelected = true;
        }
        private void ColumnSplitter_OnDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (DataContext is WatchViewModel vm)
            {
                // 拖动结束后保存
                vm.SplitterPositionLeft = SplitterPositionLeft.Width;
                vm.SplitterPositionRight = SplitterPositionRight.Width;
            }
        }
        private void RowSplitter_OnDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (DataContext is WatchViewModel vm)
            {
                // 拖动结束后保存
                vm.SplitterPositionUp = SplitterPositionUp.Height;
                vm.SplitterPositionDown = SplitterPositionDown.Height;
            }
        }
    }
}
