using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Workbench.Controls.Models;

namespace Workbench.Controls
{
    /// <summary>
    /// PPEC_Pin.xaml 的交互逻辑
    /// </summary>
    public partial class Pin : UserControl
    {
        public Pin()
        {
            InitializeComponent();

            //TopBorders = Enumerable.Range(49, 16).Select(i => i).Reverse().ToList();
            //LeftSideBorders = Enumerable.Range(1, 16).Select(i => i).ToList();
            //RightSideBorders = Enumerable.Range(33, 16).Select(i => i).Reverse().ToList();
            //BottomBorders = Enumerable.Range(17, 16).Select(i => i).ToList();

            double parentWidth = parentCanvas.Width;
            double parentHeight = parentCanvas.Height;

            double childWidth = childCanvas.Width;
            double childHeight = childCanvas.Height;

            // 计算子 Canvas 应该位于的 Top 和 Left 值
            double newLeft = (parentWidth - childWidth) / 2;
            double newTop = (parentHeight - childHeight) / 2;

            // 应用计算出的 Top 和 Left 值到子 Canvas
            Canvas.SetLeft(childCanvas, newLeft);
            Canvas.SetTop(childCanvas, newTop);

            //计算顶部引脚坐标
            CalcuateTopBorders(parentWidth, parentHeight, childWidth, childHeight);

            //计算底部引脚坐标
            CalcuateBottomBorders(parentWidth, parentHeight, childWidth, childHeight);

            //计算左侧引脚坐标
            CalcuateLeftSideBorders(parentWidth, parentHeight, childWidth, childHeight);

            //计算右侧引脚坐标
            CalcuateRightSideBorders(parentWidth, parentHeight, childWidth, childHeight);
        }

        private void CalcuateRightSideBorders(double parentWidth, double parentHeight, double childWidth, double childHeight)
        {
            var left = (parentWidth - childWidth) / 2;
            left += childWidth + 1;

            var top = (parentHeight - childHeight) / 2;

            Canvas.SetLeft(rightBorder, left);
            Canvas.SetTop(rightBorder, top);
        }

        private void CalcuateLeftSideBorders(double parentWidth, double parentHeight, double childWidth, double childHeight)
        {
            var left = (parentWidth - childWidth) / 2;
            left -= 151;

            var top = (parentHeight - childHeight) / 2;

            Canvas.SetLeft(leftBorder, left);
            Canvas.SetTop(leftBorder, top);
        }

        private void CalcuateBottomBorders(double parentWidth, double parentHeight, double childWidth, double childHeight)
        {
            var left = (parentWidth - childWidth) / 2;

            var top = (parentHeight - childHeight) / 2;
            //减去Border的高度
            top += childHeight + 1;

            Canvas.SetLeft(bottomBorder, left);
            Canvas.SetTop(bottomBorder, top);
        }

        private void CalcuateTopBorders(double parentWidth, double parentHeight, double childWidth, double childHeight)
        {
            var left = (parentWidth - childWidth) / 2;

            var top = (parentHeight - childHeight) / 2;
            // 150 1 8(Mrgin)
            top -= 159;

            Canvas.SetLeft(topBorder, left);
            Canvas.SetTop(topBorder, top);
        }

        public ObservableCollection<PPEC_Pin> TopBorders
        {
            get { return (ObservableCollection<PPEC_Pin>)GetValue(TopBordersProperty); }
            set { SetValue(TopBordersProperty, value); }
        }

        public static readonly DependencyProperty TopBordersProperty =
            DependencyProperty.Register("TopBorders", typeof(ObservableCollection<PPEC_Pin>), typeof(Pin), new PropertyMetadata(null));

        public ObservableCollection<PPEC_Pin> LeftSideBorders
        {
            get { return (ObservableCollection<PPEC_Pin>)GetValue(LeftSideBordersProperty); }
            set { SetValue(LeftSideBordersProperty, value); }
        }

        public static readonly DependencyProperty LeftSideBordersProperty =
            DependencyProperty.Register("LeftSideBorders", typeof(ObservableCollection<PPEC_Pin>), typeof(Pin), new PropertyMetadata(null));

        public ObservableCollection<PPEC_Pin> RightSideBorders
        {
            get { return (ObservableCollection<PPEC_Pin>)GetValue(RightSideBordersProperty); }
            set { SetValue(RightSideBordersProperty, value); }
        }

        public static readonly DependencyProperty RightSideBordersProperty =
            DependencyProperty.Register("RightSideBorders", typeof(ObservableCollection<PPEC_Pin>), typeof(Pin), new PropertyMetadata(null));

        public ObservableCollection<PPEC_Pin> BottomBorders
        {
            get { return (ObservableCollection<PPEC_Pin>)GetValue(BottomBordersProperty); }
            set { SetValue(BottomBordersProperty, value); }
        }

        public static readonly DependencyProperty BottomBordersProperty =
            DependencyProperty.Register("BottomBorders", typeof(ObservableCollection<PPEC_Pin>), typeof(Pin), new PropertyMetadata(null));
    }
}
