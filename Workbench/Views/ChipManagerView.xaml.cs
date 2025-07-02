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
using LinqToDB;
using PPEC.Communication.DB.Model;
using Workbench.Db;
using Workbench.Db.Tables;
using Workbench.ViewModels;

namespace Workbench.Views
{
    /// <summary>
    /// ChipManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ChipManagerView : UserControl
    {
        public ChipManagerViewModel vm { get; set; }
        public ChipManagerView()
        {
            InitializeComponent();

            this.Loaded += ChipManagerView_Loaded;
        }

        private void ChipManagerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            {
                vm = this.DataContext as ChipManagerViewModel;
            }
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tb = sender as FrameworkElement;
            var chip = tb?.DataContext as smls_chip;
            if (chip == null) return;
            vm.ChipName = chip.Name;
            vm.FilePath = chip.FilePath;
            vm.ChipId = chip.Id;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

            var tb = sender as FrameworkElement;
            var chip = tb?.DataContext as Chip;
            if (chip == null) return;
            var result = MessageBox.Show($"确定要删除芯片：{chip.Name} 配置吗？", "确认删除",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                using (var db = new DbContext())
                {
                    var res = db.Chips.Where(t => t.Id == chip.Id).Set(t => t.IsDeleted, "D").Update();
                    MessageBox.Show(res > 0 ? "删除成功!" : "删除失败!");
                    var viewModel = DataContext as ChipManagerViewModel;
                    viewModel.InitChips().GetAwaiter();
                }
            }

        }
    }
}
