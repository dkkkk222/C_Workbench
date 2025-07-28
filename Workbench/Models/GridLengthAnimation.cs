using System; 
using System.Windows;
using System.Windows.Media.Animation;

namespace Workbench.Models
{
    // GridLength 的动画（.NET 4.6.2 可用
    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength?), typeof(GridLengthAnimation));

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength?), typeof(GridLengthAnimation));

        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register(nameof(EasingFunction), typeof(IEasingFunction), typeof(GridLengthAnimation));

        /// <summary> 起始值（可空，不设则以当前值为起点） </summary>
        public GridLength? From
        {
            get => (GridLength?)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        /// <summary> 结束值（必须设置或由外部代码设置） </summary>
        public GridLength? To
        {
            get => (GridLength?)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        /// <summary> 缓动函数（可选） </summary>
        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock clock)
        {
            // 取起止值（Pixel）
            double from = (From ?? (defaultOriginValue is GridLength gl1 ? gl1 : new GridLength(0))).Value;
            double to = (To ?? (defaultDestinationValue is GridLength gl2 ? gl2 : new GridLength(0))).Value;

            // 进度
            double p = clock.CurrentProgress ?? 0.0;
            if (EasingFunction != null) p = EasingFunction.Ease(p);

            double v = from + (to - from) * p;
            if (double.IsNaN(v) || double.IsInfinity(v)) v = to;

            return new GridLength(Math.Max(0, v), GridUnitType.Pixel);
        }
    }

}
