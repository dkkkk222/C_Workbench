using AvalonDock.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbench.Models;
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
