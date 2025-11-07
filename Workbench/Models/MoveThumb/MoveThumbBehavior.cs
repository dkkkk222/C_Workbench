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
    public class MoveThumbBehavior : Behavior<Thumb>
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
}
