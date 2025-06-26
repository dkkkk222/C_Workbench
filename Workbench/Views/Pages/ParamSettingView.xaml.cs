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
using Workbench.Controls;
using Workbench.Models;
using Workbench.Models.Enums;
using Workbench.Models.Pages;
using Workbench.ViewModels.Pages;

namespace Workbench.Views.Pages
{
    /// <summary>
    /// ParamSettingView.xaml 的交互逻辑
    /// </summary>
    public partial class ParamSettingView : UserControl
    {
        public ParamSettingView()
        {
            InitializeComponent();
        }

        private void developTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeVeiwModel = e.NewValue as TreeVeiwModel;
            SwitchScreen(treeVeiwModel.Name);
        }

        private void SwitchScreen(string name)
        {
            foreach (var item in ItemsControlPanel.Items)
            {
                if (item is ParamSettingGroup paragraph && paragraph.Title == name)
                {
                    var container = ItemsControlPanel.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container != null)
                    {
                        GeneralTransform transform = container.TransformToVisual(ItemsControlPanel);
                        Point position = transform.Transform(new Point(0, 0));
                        scrollViewer.ScrollToVerticalOffset(position.Y);
                    }
                }
            }
            if (name == "通讯设置")
            {
                var scrollViewerOffset = scrollViewer.VerticalOffset;
                var point = new Point(0, scrollViewerOffset);
                var scrollPoint = ComSetting.TransformToVisual(scrollViewer).Transform(point);
                scrollViewer.ScrollToVerticalOffset(scrollPoint.Y);
            }
        }

        private void ComboBox_ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var element = source.DataContext as ParamSettingElement;
            if (element == null || !element.IsControl)
                return;

            var viewModel = this.DataContext as ParamSettingViewModel;
            var elements = viewModel.ParamSettingElements;

            var comboBox = sender as PPEC_ComboBox;

            foreach (var group in elements)
            {
                foreach (var item in group.Elements)
                {
                    if (!item.IsControled.Any())
                        continue;

                    HandleChange(item, element.Name, comboBox.SelectedValue);
                }
            }
        }

        private void HandleChange(ParamSettingElement item, string elementName, object selectedValue)
        {
            foreach (var config in item.IsControled)
            {
                if (config.ControlElementName != elementName)
                    continue;
                switch (config.Type)
                {
                    case ControlType.TitleChanged:
                        var title = config.Options.FirstOrDefault(t => t.Value == selectedValue)?.Label;
                        item.Title = title;
                        break;
                }
            }
        }
    }
}
