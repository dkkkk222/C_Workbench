using System.Windows;
using System.Windows.Controls;
using Workbench.Models.Pages;
using Workbench.Utils;

namespace Workbench.Views.Pages
{
    public class ParamSettingTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var frameworkElement = container as FrameworkElement;
            if (item is ParamSettingElement pse && frameworkElement != null)
            {
                if (pse.Type == Constants.Select)
                    return frameworkElement.FindResource(Constants.Select) as DataTemplate;
                if (pse.Type == Constants.TextBox)
                    return frameworkElement.FindResource(Constants.TextBox) as DataTemplate;
                // 更多条件
            }
            return null;
        }
    }
}
