using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Workbench.ViewModels.dw;

namespace Workbench.Views.dw
{
    /// <summary>
    /// WatchTableListView.xaml 的交互逻辑
    /// </summary>
    public partial class WatchTableListView : UserControl
    {
        public WatchTableListView()
        {
            InitializeComponent();
        }
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var vm = DataContext as WatchTableListViewModel;   // 下文给出的 VM
            if (vm == null) return;

            const double minW = 300;
            const double minH = 180;

            vm.TableWidth = System.Math.Max(minW, vm.TableWidth + e.HorizontalChange);
            vm.TableHeight = System.Math.Max(minH, vm.TableHeight + e.VerticalChange);
        }
    }
}
