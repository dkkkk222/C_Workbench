using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Model;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Models.dw;
using Workbench.ViewModels.Content.Tabs;

namespace Workbench.ViewModels.dw
{
    public class WatchTableListViewModel : BindableBase, IDialogAware
    {
        private ObservableCollection<WatchGroup> _watchGroups = new ObservableCollection<WatchGroup>();
        public ObservableCollection<WatchGroup> WatchGroups
        {
            get => _watchGroups;
            set => SetProperty(ref _watchGroups, value);
        }

        private double _tableWidth = 520;   // 初始宽
        private double _tableHeight = 360;   // 初始高
        public double TableWidth
        { 
            get => _tableWidth; 
            set 
            {
                SetProperty(ref _tableWidth, value);
            }
        }
        public double TableHeight
        {
            get => _tableHeight; 
            set 
            {
                SetProperty(ref _tableHeight, value);
            }
        }

        public WatchViewModel watchViewModel;
        public WatchViewModel WatchViewModel
        {
            get => watchViewModel;
            set
            {
                SetProperty(ref watchViewModel, value);
            }
        }

        #region Method
        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }
       
        public void OnDialogOpened(IDialogParameters parameters)
        {
            WatchViewModel = parameters.GetValue<WatchViewModel>("viewModel");
            WatchGroups = WatchViewModel.WatchGroups;

        }

        #endregion
    }
}
