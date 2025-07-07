using System.Windows.Controls;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for WatchView.xaml
    /// </summary>
    public partial class WatchView : UserControl
    {
        public WatchView()
        {
            InitializeComponent();
            InitPlot();
        }

        private void InitPlot()
        {
            double[] dataX = { 1, 2, 3, 4, 5 };
            double[] dataY = { 1, 4, 9, 16, 25 };
            WpfPlot1.Plot.Add.Scatter(dataX, dataY);
            WpfPlot1.Refresh();
        }
    }
}
