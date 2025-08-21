using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Models.dw;

namespace Workbench.ViewModels.dw
{
    public class WatchChartListViewModel : BindableBase, IDialogAware
    {
        private ObservableCollection<WatchChartModel> _watchGroups = new ObservableCollection<WatchChartModel>();
        public ObservableCollection<WatchChartModel> WatchGroups
        {
            get => _watchGroups;
            set => SetProperty(ref _watchGroups, value);
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

        // ★ 列表专用的“过滤后视图”，不影响原集合（供 ComboBox 使用）
        private ICollectionView _watchGroupsView;
        public ICollectionView WatchGroupsView
        {
            get => _watchGroupsView;
            private set => SetProperty(ref _watchGroupsView, value);
        }
        private static bool IsPlaceholder(WatchChartModel m)
       => m != null && string.Equals(m.Header, "未选中", StringComparison.Ordinal);
        #region Method
        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            WatchViewModel.WatchChartGroups.CollectionChanged -= OnChartsChanged;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            WatchViewModel = parameters.GetValue<WatchViewModel>("viewModel");
            WatchGroups = WatchViewModel.WatchChartGroups;

            // ★ 列表页用独立视图（不会影响默认视图）
            WatchGroupsView = new ListCollectionView(WatchViewModel.WatchChartGroups);
            WatchGroupsView.Filter = o => o is WatchChartModel m && !IsPlaceholder(m);
            WatchGroupsView.Refresh();

            // 监听集合变化，自动刷新过滤
            WatchViewModel.WatchChartGroups.CollectionChanged += OnChartsChanged;
        }
        private void OnChartsChanged(object sender, NotifyCollectionChangedEventArgs e)
        => WatchGroupsView?.Refresh();
        #endregion
    }
}
