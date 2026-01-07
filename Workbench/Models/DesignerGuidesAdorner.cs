using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace Workbench.Models
{
    public sealed class DesignerGuidesAdorner : Adorner
    {
        private readonly List<double> _vLines = new List<double>();
        private readonly List<double> _hLines = new List<double>();

        public DesignerGuidesAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        public void SetLines(IEnumerable<double> vLines, IEnumerable<double> hLines)
        {
            _vLines.Clear();
            _hLines.Clear();
            if (vLines != null) _vLines.AddRange(vLines);
            if (hLines != null) _hLines.AddRange(hLines);
            InvalidateVisual();
        }

        public void Clear()
        {
            if (_vLines.Count == 0 && _hLines.Count == 0) return;
            _vLines.Clear();
            _hLines.Clear();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var fe = AdornedElement as FrameworkElement;
            if (fe == null) return;

            // 虚线更像设计器
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 122, 204)), 1);
            pen.DashStyle = DashStyles.Dash;
            pen.Freeze();

            double w = fe.ActualWidth;
            double h = fe.ActualHeight;

            foreach (double x in _vLines.Distinct())
                dc.DrawLine(pen, new Point(x, 0), new Point(x, h));

            foreach (double y in _hLines.Distinct())
                dc.DrawLine(pen, new Point(0, y), new Point(w, y));
        }
    }

    public static class DesignerGuides
    {
        private static readonly DependencyProperty GuidesAdornerProperty =
            DependencyProperty.RegisterAttached("GuidesAdorner",
                typeof(DesignerGuidesAdorner),
                typeof(DesignerGuides),
                new PropertyMetadata(null));

        public static DesignerGuidesAdorner GetOrCreate(FrameworkElement host)
        {
            if (host == null) return null;

            var adorner = (DesignerGuidesAdorner)host.GetValue(GuidesAdornerProperty);
            if (adorner != null) return adorner;

            var layer = AdornerLayer.GetAdornerLayer(host);
            if (layer == null) return null;

            adorner = new DesignerGuidesAdorner(host);
            layer.Add(adorner);
            host.SetValue(GuidesAdornerProperty, adorner);
            return adorner;
        }
    }
}
