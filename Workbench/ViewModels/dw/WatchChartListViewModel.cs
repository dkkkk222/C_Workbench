using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        #region 布局控制（与前一页一致）

        private int gridRows = 2;
        public int GridRows
        {
            get => gridRows;
            set => SetProperty(ref gridRows, value);
        }

        private int gridColumns = 0; // 0 自动
        public int GridColumns
        {
            get => gridColumns;
            set => SetProperty(ref gridColumns, value);
        }

        private bool useWrapLayout = false; // false=UniformGrid, true=WrapPanel
        public bool UseWrapLayout
        {
            get => useWrapLayout;
            set => SetProperty(ref useWrapLayout, value);
        }

        private Orientation itemOrientation = Orientation.Horizontal;
        public Orientation ItemOrientation
        {
            get => itemOrientation;
            set => SetProperty(ref itemOrientation, value);
        }

        private HorizontalAlignment itemHorizontalAlignment = HorizontalAlignment.Center;
        public HorizontalAlignment ItemHorizontalAlignment
        {
            get => itemHorizontalAlignment;
            set => SetProperty(ref itemHorizontalAlignment, value);
        }

        private VerticalAlignment itemVerticalAlignment = VerticalAlignment.Center;
        public VerticalAlignment ItemVerticalAlignment
        {
            get => itemVerticalAlignment;
            set => SetProperty(ref itemVerticalAlignment, value);
        }

        private HorizontalAlignment itemContainerHorizontalAlignment = HorizontalAlignment.Stretch;
        public HorizontalAlignment ItemContainerHorizontalAlignment
        {
            get => itemContainerHorizontalAlignment;
            set => SetProperty(ref itemContainerHorizontalAlignment, value);
        }

        private VerticalAlignment itemContainerVerticalAlignment = VerticalAlignment.Stretch;
        public VerticalAlignment ItemContainerVerticalAlignment
        {
            get => itemContainerVerticalAlignment;
            set => SetProperty(ref itemContainerVerticalAlignment, value);
        }

        // WrapPanel 统一卡片尺寸（可按需用）
        private double cellWidth = 700;   // 结合 MinWidth=680，略大一些好看
        public double CellWidth
        {
            get => cellWidth;
            set => SetProperty(ref cellWidth, value);
        }

        private double cellHeight = 320;  // 结合 MinHeight=300
        public double CellHeight
        {
            get => cellHeight;
            set => SetProperty(ref cellHeight, value);
        }

        #endregion

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
