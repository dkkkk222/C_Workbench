using System.Windows;
using System.Windows.Controls;
using Workbench.ViewModels.Content.Tabs;

namespace Workbench.AvalonDock
{
    public class PanesStyleSelector : StyleSelector
    {

        public Style HomeStyle
        {
            get;
            set;
        }

        public Style DevelopStyle
        {
            get;
            set;
        }

        public Style DebugStyle
        {
            get;
            set;
        }

        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            if (item is HomeViewModel)
                return HomeStyle;

            if (item is DevelopViewModel)
                return DevelopStyle;

            if (item is DebugViewModel)
                return DebugStyle;

            return base.SelectStyle(item, container);
        }
    }
}
