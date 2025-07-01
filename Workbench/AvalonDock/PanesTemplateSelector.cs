using AvalonDock.Layout;
using System.Windows;
using System.Windows.Controls;
using Workbench.ViewModels.Content.Tabs;
using Workbench.ViewModels.dw;

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

        public DataTemplate SingleParamsViewTemplate
        {
            get;
            set;
        }

        public DataTemplate BatchParamsViewTemplate
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

            if (item is SingleParamsViewModel)
            {
                return SingleParamsViewTemplate;
            }

            if (item is BatchParamsViewModel)
            {
                return BatchParamsViewTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
