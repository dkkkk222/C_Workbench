using System.Windows;
using System.Windows.Controls;
using Workbench.ViewModels.Content.Tabs;
using Workbench.ViewModels.dw;

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

        public Style SingleParamsStyle
        {
            get;
            set;
        }

        public Style BatchParamsStyle
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

            if (item is SingleParamsViewModel)
                return SingleParamsStyle;

            if (item is BatchParamsViewModel)
                return BatchParamsStyle;

            return base.SelectStyle(item, container);
        }
    }
}
