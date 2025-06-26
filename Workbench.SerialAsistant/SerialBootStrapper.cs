using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Workbench.SerialAsistant.ViewModels;
using Workbench.SerialAsistant.Views;
using Workbench.SerialAsistant.Windows;

namespace Workbench.SerialAsistant
{
    public class SerialBootStrapper
    {
        private readonly IContainerRegistry _containerRegistry;
        public SerialBootStrapper(IContainerProvider containerProvider)
        {
            _containerRegistry = containerProvider as IContainerRegistry;
        }

        public void OnStart()
        {
            RegisterTypes();
        }

        private void RegisterTypes()
        {
            _containerRegistry.RegisterDialogWindow<MainWindow>(nameof(MainWindow));
            _containerRegistry.RegisterDialog<MainView, MainViewModel>();
        }
    }
}
