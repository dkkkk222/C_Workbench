using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Utils;
using Workbench.Views.Content;

namespace Workbench.Views.Content.ButtonBar
{
    [Module(ModuleName = nameof(ButtonBarModule))]
    public class ButtonBarModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {   
            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion(RegionNames.ButtonBarRegion, typeof(ButtonBarView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ButtonBarView>();
        }
    }
}
