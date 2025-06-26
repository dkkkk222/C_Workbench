using AvalonDock.Layout;
using System.Windows;
using System.Windows.Controls;
using Workbench.ViewModels.Content.Tabs;

namespace Workbench.AvalonDock
{
    public class PanesTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HomeViewTemplate
        {
            get;
            set;
        }

        public DataTemplate DevelopViewTemplate
        {
            get;
            set;
        }

        public DataTemplate DebugViewTemplate
        {
            get;
            set;
        }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var itemAsLayoutContent = item as LayoutContent;

            if (item is HomeViewModel)
                return HomeViewTemplate;

            if (item is DevelopViewModel)
                return DevelopViewTemplate;

            if (item is DebugViewModel)
                return DebugViewTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
