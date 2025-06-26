using log4net;
using Microsoft.Web.WebView2.Core;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
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
using Workbench.Models.Data;
using Workbench.Utils;
using Workbench.ViewModels.Content.Develop;
using Workbench.ViewModels.Content.Tabs;
using Workbench.ViewModels.Pages;

namespace Workbench.Views.Content.Tabs
{
    /// <summary>
    /// DevelopView.xaml 的交互逻辑
    /// </summary>
    public partial class DevelopView : UserControl
    {
        public DevelopView()
        {
            InitializeComponent();
        }
    }
}
