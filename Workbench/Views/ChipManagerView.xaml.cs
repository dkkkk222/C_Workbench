using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using HandyControl.Expression.Shapes;
using LinqToDB;
using PPEC.Communication.DB.Model;
using Workbench.Db;
using Workbench.Db.Tables;
using Workbench.Models;
using Workbench.ViewModels;
using Workbench.Views.Windows;

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
        private UserManualWindow _userManualWindow;
        private void ShowDoc_Click(object sender, RoutedEventArgs e)
        {
            var tb = sender as FrameworkElement;
            var chip = tb?.DataContext as Chip;
            if (chip == null) return;

            if (string.IsNullOrEmpty(chip.DocFilePath))
            {
                MessageBox.Show($"未找到【芯片手册】，请上传", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            //var userManualName = "workbench_user_manual";
            string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "ChipDoc");
            string dest = System.IO.Path.Combine(destDir, chip.DocFilePath);
            var path = dest;// $"{AppDomain.CurrentDomain.BaseDirectory}Resource\\{userManualName}.pdf";
            if (!File.Exists(path))
            {
                MessageBox.Show($"未找到【芯片手册】，请上传", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_userManualWindow == null)
                _userManualWindow = new UserManualWindow(dest);
            if (_userManualWindow.IsVisible)
            {
                if (_userManualWindow.WindowState == WindowState.Minimized)
                {
                    _userManualWindow.WindowState = WindowState.Normal;
                }
                _userManualWindow.Activate();
            }
            else
            {
                _userManualWindow = new UserManualWindow(dest);
                _userManualWindow.Show();
            }
        }
        private async void UpdateDoc_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var chip = fe?.DataContext as Chip;
            if (chip == null) return;

            var open = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择手册（PDF）",
                Filter = "PDF 文件 (*.pdf)|*.pdf|所有文件 (*.*)|*.*",
                Multiselect = false
            };
            if (open.ShowDialog() != true) return;

            string src = open.FileName;
            string newName = System.IO.Path.GetFileName(src); // 本次选择的文件名
            string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "ChipDoc");
            Directory.CreateDirectory(destDir);

            // 历史文件名（可能为空）
            string oldName = string.IsNullOrWhiteSpace(chip.DocFilePath)
                ? null
                : System.IO.Path.GetFileName(chip.DocFilePath);

            string dest;

            if (!string.IsNullOrEmpty(oldName) &&
                string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                // 1) 与之前同名：覆盖旧文件
                dest = System.IO.Path.Combine(destDir, oldName);

                if (File.Exists(dest))
                {
                    var result = MessageBox.Show("同名手册已存在，是否覆盖该文件？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
            }
            else
            {
                // 2) 与之前不同名（或之前为空）：新建为新文件名，不动旧文件
                dest = System.IO.Path.Combine(destDir, newName);

                // 若目录里已存在同名（但不是“之前那份”），为避免误覆盖，自动生成不重名文件名
                if (File.Exists(dest))
                    dest = MakeUniquePath(destDir, newName); // 见下面的辅助函数
            }

            // 源与目标相同则不必复制
            if (string.Equals(System.IO.Path.GetFullPath(src), System.IO.Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("源文件与目标文件相同，无需上传。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var s = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var d = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None); // Create=覆盖/新建
                await s.CopyToAsync(d);

                // 记录最终保存的“文件名”（仅文件名）
                chip.DocFilePath = System.IO.Path.GetFileName(dest);

                if (DataContext is ChipManagerViewModel vm)
                    await vm.UpdateChipDoc(chip);

                MessageBox.Show("上传成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static string MakeUniquePath(string dir, string fileName)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string ext = System.IO.Path.GetExtension(fileName);
            string cand = System.IO.Path.Combine(dir, fileName);
            int i = 1;
            while (System.IO.File.Exists(cand))
                cand = System.IO.Path.Combine(dir, $"{name}({i++}){ext}");
            return cand;
        }
        //private async void UpdateDoc_Click(object sender, RoutedEventArgs e)
        //{
        //    var tb = sender as FrameworkElement;
        //    var chip = tb?.DataContext as Chip;
        //    if (chip == null) return;

        //    var open = new Microsoft.Win32.OpenFileDialog
        //    {
        //        Title = "选择手册（PDF）",
        //        Filter = "PDF 文件 (*.pdf)|*.pdf|所有文件 (*.*)|*.*",
        //        Multiselect = false
        //    };

        //    if (open.ShowDialog() != true) return;
        //    var fileName = open.FileName;
        //    var selectFileName= System.IO.Path.GetFileName(fileName);
        //    string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "ChipDoc");
        //    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        //        Directory.CreateDirectory(destDir);
        //    string finalName = string.IsNullOrWhiteSpace(chip.DocFilePath)
        //                        ? System.IO.Path.GetFileName(fileName)
        //                        : System.IO.Path.GetFileName(chip.DocFilePath);

        //    string dest = System.IO.Path.Combine(destDir, finalName);

        //    if (System.IO.File.Exists(dest))
        //    {
        //        var result = MessageBox.Show("手册文件已存在，是否覆盖?", "提示",
        //            MessageBoxButton.YesNo, MessageBoxImage.Question);
        //        if (result != MessageBoxResult.Yes) return;
        //    }

        //    if (string.Equals(System.IO.Path.GetFullPath(fileName), System.IO.Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
        //    {
        //        MessageBox.Show("源文件与目标文件相同，无需上传。", "提示",
        //            MessageBoxButton.OK, MessageBoxImage.Information);
        //        return;
        //    }

        //    try
        //    {
        //        using var s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        //        using var d = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None); // Create = 覆盖
        //        await s.CopyToAsync(d);

        //        // 更新绑定字段为最终文件名（仅文件名，便于后续组合路径）
        //        chip.DocFilePath = selectFileName;

        //        if (DataContext is ChipManagerViewModel vm)
        //            await vm.UpdateChipDoc(chip);

        //        MessageBox.Show("上传成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"上传失败：{ex.Message}", "错误",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }

        //}
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
        private void Rebuild_Click(object sender, RoutedEventArgs e)
        {
            var tb = sender as FrameworkElement;
            var chip = tb?.DataContext as Chip;
            if (chip == null) return;
            var result = MessageBox.Show($"确定要重建芯片{chip.Name}参数关系吗？该重建会用当前工程参数信息覆盖所选芯片，请慎重操作！", "确认重建",
                               MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                vm.RebuildChipMetadataAsync(chip.Id);
        }
    }
}
