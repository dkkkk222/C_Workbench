using PPEC.Communication;
using System.Windows;
using System.Windows.Controls;
using Workbench.ViewModels.dw;

namespace Workbench.Views.dw
{
    /// <summary>
    /// Interaction logic for SingleParamsView.xaml
    /// </summary>
    public partial class SingleParamsView : UserControl
    {
        public SingleParamsView()
        {
            InitializeComponent();
        }

        private void HexValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;
            var u = Utility.ParseHexToUInt(text);
            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;
            if (viewModel.CurrentRegister.DecValue != u)
            {
                viewModel.CurrentRegister.DecValue = u;
            }
        }

        private void DecValueText_LostFocus(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SingleParamsViewModel;
            if (viewModel.CurrentRegister == null)
                return;
            var dec = viewModel.CurrentRegister.DecValue;
            var ui = Utility.ParseHexToUInt(viewModel.CurrentRegister.HexValue);
            if (dec != ui)
            {
                viewModel.CurrentRegister.HexValue = "0x" + dec.ToString("X8");
            }
        }
    }
}
