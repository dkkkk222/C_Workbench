using AvalonDock.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Workbench.Models;
using Workbench.Utils;
using Workbench.ViewModels;
using Workbench.ViewModels.Content;

namespace Workbench.Views.Content
{
    /// <summary>
    /// ContentView.xaml 的交互逻辑
    /// </summary>
    public partial class ContentView : UserControl
    {
        public ContentView()
        {
            InitializeComponent();
        }

        private async void DocTab_PreviewMouseLeftButtonDown(
           object sender, MouseButtonEventArgs e)
        {
            try
            {
                var tab = sender as LayoutDocumentTabItem;
                var next = tab.LayoutItem?.Model as AvaDocument;
                if (next == null) return;

                var vm = DataContext as ContentViewModel; // 你的 VM
                if (vm.ActiveDocument?.Project?.PPEC_Id != next.Project?.PPEC_Id &&
                    vm._projectManager.CurrentProject.IsConnecting)
                {
                    var r = MessageBox.Show(
                        "切换芯片后必须断开原有芯片连接，确认断开？",
                        "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (r == MessageBoxResult.No)
                    {
                        e.Handled = true;        // ★ 直接拦截
                        return;
                    }

                    // 先拦截，异步断开后再手动激活
                    e.Handled = true;
                    await vm.AsyncDisConnect();
                    next.IsActive = true;        // 断开成功后再切
                }
            }
            catch (Exception ex)
            { 
            
            }
           
        }

        private void CloseOthers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as LayoutDocumentItem;
            var viewModel = DataContext as ContentViewModel;
            var tab = viewModel.Documents.FirstOrDefault(t => t.ContentId == nodeData.ContentId);
            viewModel.Documents.Clear();
            viewModel.Documents.Add(tab);
        }

        private void CloseAll_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ContentViewModel;
            viewModel.Documents.Clear();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as LayoutDocumentItem;
            var viewModel = DataContext as ContentViewModel;
            var tab = viewModel.Documents.FirstOrDefault(t => t.ContentId == nodeData.ContentId);
            if (tab != null)
            {
                viewModel.Documents.Remove(tab);
            }

        }

        private void CloseLeft_Click(object sender, RoutedEventArgs e)
        {
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as LayoutDocumentItem;
            var viewModel = DataContext as ContentViewModel;
            var documents = viewModel.Documents;
            var clickedDocument = documents.FirstOrDefault(t => t.ContentId == nodeData.ContentId);
            var index = documents.IndexOf(clickedDocument);
            var removeList = documents.Take(index).ToList();
            foreach (var item in removeList)
            {
                documents.Remove(item);
            }
        }

        private void CloseRight_Click(object sender, RoutedEventArgs e)
        {
            var source = (FrameworkElement)e.OriginalSource;
            var nodeData = source.DataContext as LayoutDocumentItem;
            var viewModel = DataContext as ContentViewModel;
            var documents = viewModel.Documents;
            var clickedDocument = documents.FirstOrDefault(t => t.ContentId == nodeData.ContentId);
            var index = documents.IndexOf(clickedDocument);
            var removeList = documents.Skip(index + 1).ToList();
            foreach (var item in removeList)
            {
                documents.Remove(item);
            }
        }
    }
}
