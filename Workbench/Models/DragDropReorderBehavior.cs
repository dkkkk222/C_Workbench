using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using HandyControl.Expression.Shapes;
using System.Windows.Data;
using Workbench.ViewModels.dw;

namespace Workbench.Models
{
    public class DragDropReorderBehavior : Behavior<ItemsControl>
    {
        private Point _dragStart;
        private bool _isDragging;
        private object _dragItem;

        // 可选：是否允许跨不同 ItemsControl 拖入（默认只在本列表内交换）
        public bool AllowCrossItemsControl { get; set; } = false;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= OnMouseMove;
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
            base.OnDetaching();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _dragItem = GetItemUnderMouse(e.OriginalSource as DependencyObject, e.GetPosition(AssociatedObject));
            //_dragItem = GetItemUnderMouse(e.OriginalSource as DependencyObject);
            var targetItem = GetItemUnderMouse(e.OriginalSource as DependencyObject, e.GetPosition(AssociatedObject));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragItem == null || _isDragging)
                return;

            var current = e.GetPosition(null);
            if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            _isDragging = true;
            try
            {
                var data = new DataObject("DragDropReorderBehavior_Item", _dragItem);
                DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
            }
            finally
            {
                _isDragging = false;
                _dragItem = null;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("DragDropReorderBehavior_Item"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (!AllowCrossItemsControl)
            {
                // 只允许同一个 ItemsControl 内拖拽
                var src = e.OriginalSource as DependencyObject;
                var container = ItemsControl.ContainerFromElement(AssociatedObject, src as DependencyObject);
                // 只要是在当前 ItemsControl 上方，就允许 Move
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }

            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("DragDropReorderBehavior_Item")) return;

            var src = e.Data.GetData("DragDropReorderBehavior_Item");
            var dst = GetItemUnderMouse(e.OriginalSource as DependencyObject, e.GetPosition(AssociatedObject));
            if (src == null || dst == null || Equals(src, dst)) return;

            // 占位项直接忽略（防止拖到“未选中”）
            if ((src as WatchChartModel)?.Id == "placeholder" || (dst as WatchChartModel)?.Id == "placeholder")
                return;

            var view = AssociatedObject.ItemsSource as ICollectionView
                       ?? CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
            SwapOrderProperty(src, dst, view); // 交换 Order 并 Refresh()
        }

        private object GetItemUnderMouse(DependencyObject src, Point? posOverride = null)
        {
            // 先走正常路径
            var container = ItemsControl.ContainerFromElement(AssociatedObject, src);
            if (container != null)
                return AssociatedObject.ItemContainerGenerator.ItemFromContainer(container);

            // ★回退：用坐标命中（适配 HwndHost/自绘控件）
            var p = posOverride ?? Mouse.GetPosition(AssociatedObject);
            for (int i = 0; i < AssociatedObject.Items.Count; i++)
            {
                var cp = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (cp == null) continue;
                var topLeft = cp.TranslatePoint(new System.Windows.Point(0, 0), AssociatedObject);
                var bounds = new Rect(topLeft, cp.RenderSize);
                if (bounds.Contains(p))
                    return AssociatedObject.Items[i];
            }
            return null;
        }

        private static void SwapInList(IList list, object a, object b)
        {
            if (list == null || a == null || b == null) return;
            int i = list.IndexOf(a);
            int j = list.IndexOf(b);
            if (i < 0 || j < 0 || i == j) return;

            // 直接交换下标（ObservableCollection<T> 也实现了 IList）
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        private void MoveToEnd(object item)
        {
            var (list, view) = ResolveListAndView(AssociatedObject);
            if (list == null || item == null) return;

            if (view != null && view.SortDescriptions?.Count > 0)
            {
                // 排序视图：改 Order 为最大 + 1
                BumpOrderToEnd(item, view);
                return;
            }

            int idx = list.IndexOf(item);
            if (idx >= 0 && idx != list.Count - 1)
            {
                // 移动到末尾
                list.RemoveAt(idx);
                list.Add(item);
            }
        }

        private static Tuple<IList, ICollectionView> ResolveListAndView(ItemsControl ic)
        {
            var src = ic.ItemsSource;
            if (src is IList list)
                return Tuple.Create(list, (ICollectionView)null);

            if (src is ICollectionView cv)
            {
                var sc = cv.SourceCollection as IList;
                return Tuple.Create(sc, cv);
            }

            // 绑定为空时，可能直接用 Items
            return Tuple.Create(ic.Items as IList, (ICollectionView)null);
        }

        /// <summary>
        /// 若你使用了 CollectionView 的 Sort（按 Order 排序），请给项类加一个 int Order 属性。
        /// 这里交换两个项的 Order 值并刷新视图。
        /// </summary>
        private static void SwapOrderProperty(object a, object b, ICollectionView view)
        {
            if (a == null || b == null || view == null) return;

            var prop = TypeDescriptor.GetProperties(a)["Order"];
            if (prop == null || prop.PropertyType != typeof(int) || prop.IsReadOnly) return;

            var propB = TypeDescriptor.GetProperties(b)["Order"];
            if (propB == null) return;

            int oa = (int)prop.GetValue(a);
            int ob = (int)propB.GetValue(b);

            prop.SetValue(a, ob);
            propB.SetValue(b, oa);

            view.Refresh();
        }

        private static void BumpOrderToEnd(object item, ICollectionView view)
        {
            if (item == null || view == null) return;
            var prop = TypeDescriptor.GetProperties(item)["Order"];
            if (prop == null || prop.PropertyType != typeof(int) || prop.IsReadOnly) return;

            int max = int.MinValue;
            foreach (var it in view.SourceCollection)
            {
                var pi = TypeDescriptor.GetProperties(it)["Order"];
                if (pi != null && pi.PropertyType == typeof(int))
                {
                    int val = (int)pi.GetValue(it);
                    if (val > max) max = val;
                }
            }
            prop.SetValue(item, max + 1);
            view.Refresh();
        }
    }
}
