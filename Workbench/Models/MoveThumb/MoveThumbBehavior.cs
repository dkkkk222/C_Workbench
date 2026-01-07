using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace Workbench.Models.MoveThumb
{
    public class MoveThumbBehavior1 : Behavior<Thumb>
    {
        public string LeftPropertyName { get; set; } = "Left";
        public string TopPropertyName { get; set; } = "Top";

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragDelta += OnDragDelta;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragDelta -= OnDragDelta;
            base.OnDetaching();
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var dc = AssociatedObject.DataContext;
            if (dc == null) return;

            var leftProp = dc.GetType().GetProperty(LeftPropertyName, BindingFlags.Public | BindingFlags.Instance);
            var topProp = dc.GetType().GetProperty(TopPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (leftProp == null || topProp == null) return;

            double left = Convert.ToDouble(leftProp.GetValue(dc) ?? 0d);
            double top = Convert.ToDouble(topProp.GetValue(dc) ?? 0d);

            left = Math.Max(0, left + e.HorizontalChange);
            top = Math.Max(0, top + e.VerticalChange);

            leftProp.SetValue(dc, left);
            topProp.SetValue(dc, top);
        }
    }

    public class MoveThumbBehavior : Behavior<Thumb>
    {
        public string LeftPropertyName { get; set; } = "Left";
        public string TopPropertyName { get; set; } = "Top";

        public string WidthPropertyName { get; set; } = "ChartWidth";
        public string HeightPropertyName { get; set; } = "ChartHeight";

        // 网格设置
        public double GridSize { get; set; } = 10;          // 网格间距
        public double SnapThreshold { get; set; } = 6;      // 吸附阈值（像素）
        public bool SnapToGrid { get; set; } = true;
        public bool SnapToOtherItems { get; set; } = true;

        private ContentPresenter _container;
        private ItemsControl _itemsControl;
        private FrameworkElement _guideHost; // 我们把对齐线画在“画布根 Grid”上（AdornerDecorator 里面那层）

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragStarted += OnDragStarted;
            AssociatedObject.DragDelta += OnDragDelta;
            AssociatedObject.DragCompleted += OnDragCompleted;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragStarted -= OnDragStarted;
            AssociatedObject.DragDelta -= OnDragDelta;
            AssociatedObject.DragCompleted -= OnDragCompleted;
            base.OnDetaching();
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            _container = FindAncestor<ContentPresenter>(AssociatedObject);
            _itemsControl = FindAncestor<ItemsControl>(AssociatedObject);

            // guideHost：找 ScrollViewer 内容里那层 Grid（最稳）
            _guideHost = FindAncestor<Grid>(_itemsControl) ?? (FrameworkElement)_itemsControl;

            // 确保 adorner 创建
            DesignerGuides.GetOrCreate(_guideHost);
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            var adorner = DesignerGuides.GetOrCreate(_guideHost);
            adorner?.Clear();
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var dc = AssociatedObject.DataContext;
            if (dc == null) return;

            if (!TryGetDouble(dc, LeftPropertyName, out double left)) return;
            if (!TryGetDouble(dc, TopPropertyName, out double top)) return;

            // 当前项尺寸（用于对齐“右边缘/中心”）
            //double w = GetDoubleOr(dc, "ChartWidth", 0);
            //double h = GetDoubleOr(dc, "ChartHeight", 0);

            double w = GetDoubleOr(dc, WidthPropertyName, 0);
            double h = GetDoubleOr(dc, HeightPropertyName, 0);

            double newLeft = left + e.HorizontalChange;
            double newTop = top + e.VerticalChange;

            newLeft = Math.Max(0, newLeft);
            newTop = Math.Max(0, newTop);

            double? vGuide = null;
            double? hGuide = null;

            // 1) 先尝试和其他元素对齐
            if (SnapToOtherItems && _itemsControl != null)
            {
                var others = GetOtherItems(dc, _itemsControl);
                // X 对齐（左/中/右）
                (newLeft, vGuide) = SnapAlignX(newLeft, w, others, SnapThreshold);
                // Y 对齐（上/中/下）
                (newTop, hGuide) = SnapAlignY(newTop, h, others, SnapThreshold);
            }

            // 2) 再吸附网格（如果没对齐到其他元素，也可以同时吸附网格）
            if (SnapToGrid && GridSize > 0)
            {
                (newLeft, var vg2) = SnapToGridLine(newLeft, GridSize, SnapThreshold);
                (newTop, var hg2) = SnapToGridLine(newTop, GridSize, SnapThreshold);

                if (vGuide == null) vGuide = vg2;
                if (hGuide == null) hGuide = hg2;
            }

            // 像素级：最终再取整，避免半像素导致发虚
            newLeft = Math.Round(newLeft);
            newTop = Math.Round(newTop);

            SetDouble(dc, LeftPropertyName, newLeft);
            SetDouble(dc, TopPropertyName, newTop);

            // 更新对齐线
            var adorner = DesignerGuides.GetOrCreate(_guideHost);
            adorner?.SetLines(
                vGuide.HasValue ? new[] { vGuide.Value } : null,
                hGuide.HasValue ? new[] { hGuide.Value } : null
            );
        }

        // ----------------- 对齐/吸附算法 -----------------

        private static (double value, double? guide) SnapToGridLine(double value, double grid, double threshold)
        {
            double snapped = Math.Round(value / grid) * grid;
            if (Math.Abs(snapped - value) <= threshold)
                return (snapped, snapped);
            return (value, null);
        }

        private static (double newLeft, double? guideX) SnapAlignX(double left, double width, List<ItemRect> others, double threshold)
        {
            if (width <= 0 || others.Count == 0) return (left, null);

            // 我自己的三个关键点：L/C/R
            double myL = left;
            double myC = left + width / 2.0;
            double myR = left + width;

            double bestAbs = double.MaxValue;
            double bestLeft = left;
            double? bestGuide = null;

            foreach (var o in others)
            {
                foreach (double target in new[] { o.Left, o.CenterX, o.Right })
                {
                    TryBetter(target - myL, target);
                    TryBetter(target - myC, target);
                    TryBetter(target - myR, target);
                }
            }

            return (bestGuide.HasValue ? bestLeft : left, bestGuide);

            void TryBetter(double delta, double guideX)
            {
                double abs = Math.Abs(delta);
                if (abs <= threshold && abs < bestAbs)
                {
                    bestAbs = abs;
                    bestLeft = left + delta;
                    bestGuide = guideX;
                }
            }
        }

        private static (double newTop, double? guideY) SnapAlignY(double top, double height, List<ItemRect> others, double threshold)
        {
            if (height <= 0 || others.Count == 0) return (top, null);

            double myT = top;
            double myC = top + height / 2.0;
            double myB = top + height;

            double bestAbs = double.MaxValue;
            double bestTop = top;
            double? bestGuide = null;

            foreach (var o in others)
            {
                foreach (double target in new[] { o.Top, o.CenterY, o.Bottom })
                {
                    TryBetter(target - myT, target);
                    TryBetter(target - myC, target);
                    TryBetter(target - myB, target);
                }
            }

            return (bestGuide.HasValue ? bestTop : top, bestGuide);

            void TryBetter(double delta, double guideY)
            {
                double abs = Math.Abs(delta);
                if (abs <= threshold && abs < bestAbs)
                {
                    bestAbs = abs;
                    bestTop = top + delta;
                    bestGuide = guideY;
                }
            }
        }

        private List<ItemRect> GetOtherItems(object current, ItemsControl ic)
        {
            var list = new List<ItemRect>();

            IEnumerable items = ic.ItemsSource as IEnumerable ?? ic.Items;
            if (items == null) return list;

            foreach (var it in items)
            {
                if (it == null || ReferenceEquals(it, current)) continue;

                double l = GetDoubleOr(it, LeftPropertyName, double.NaN);
                double t = GetDoubleOr(it, TopPropertyName, double.NaN);
                double w = GetDoubleOr(it, "ChartWidth", double.NaN);
                double h = GetDoubleOr(it, "ChartHeight", double.NaN);

                if (double.IsNaN(l) || double.IsNaN(t) || double.IsNaN(w) || double.IsNaN(h)) continue;

                list.Add(new ItemRect(l, t, w, h));
            }

            return list;
        }

        private readonly struct ItemRect
        {
            public ItemRect(double left, double top, double width, double height)
            {
                Left = left; Top = top; Width = width; Height = height;
            }
            public double Left { get; }
            public double Top { get; }
            public double Width { get; }
            public double Height { get; }
            public double Right => Left + Width;
            public double Bottom => Top + Height;
            public double CenterX => Left + Width / 2.0;
            public double CenterY => Top + Height / 2.0;
        }

        // ----------------- 反射读写 -----------------

        private static bool TryGetDouble(object dc, string propName, out double value)
        {
            value = 0;
            var p = dc.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return false;
            value = Convert.ToDouble(p.GetValue(dc) ?? 0d);
            return true;
        }

        private static double GetDoubleOr(object dc, string propName, double fallback)
        {
            var p = dc.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return fallback;
            try { return Convert.ToDouble(p.GetValue(dc) ?? fallback); }
            catch { return fallback; }
        }

        private static void SetDouble(object dc, string propName, double value)
        {
            var p = dc.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return;
            p.SetValue(dc, value);
        }

        // ----------------- 视觉树查找 -----------------

        private static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
