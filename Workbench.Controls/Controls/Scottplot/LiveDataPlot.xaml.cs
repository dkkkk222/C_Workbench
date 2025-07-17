using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Workbench.Controls.Controls.Scottplot
{
    /// <summary>
    /// LiveDataPlot.xaml 的交互逻辑
    /// </summary>
    public partial class LiveDataPlot : UserControl
    {
        public LiveDataPlot()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty PlotDataProperty = DependencyProperty.Register(
        "PlotData", typeof(WpfPlotSteamBase), typeof(LiveDataPlot), new PropertyMetadata(ListChartsPropertyChanged));

        public WpfPlotSteamBase PlotData
        {
            get => (WpfPlotSteamBase)GetValue(PlotDataProperty);
            set => SetValue(PlotDataProperty, value);
        }

        private static void ListChartsPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var listCharts = e.NewValue as WpfPlotSteamBase;
        }
    }
}
