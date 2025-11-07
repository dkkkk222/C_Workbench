using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;

namespace Workbench.Models.MoveThumb
{
    public class ResizeThumbBehavior : Behavior<Thumb>
    {
        public string WidthPropertyName { get; set; } = "ChartWidth";
        public string HeightPropertyName { get; set; } = "ChartHeight";
        public double MinWidth { get; set; } = 200;
        public double MinHeight { get; set; } = 150;

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

            var wProp = dc.GetType().GetProperty(WidthPropertyName, BindingFlags.Public | BindingFlags.Instance);
            var hProp = dc.GetType().GetProperty(HeightPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (wProp == null || hProp == null) return;

            double w = Convert.ToDouble(wProp.GetValue(dc) ?? 0d);
            double h = Convert.ToDouble(hProp.GetValue(dc) ?? 0d);

            w = Math.Max(MinWidth, w + e.HorizontalChange);
            h = Math.Max(MinHeight, h + e.VerticalChange);

            wProp.SetValue(dc, w);
            hProp.SetValue(dc, h);
        }
    }
}
