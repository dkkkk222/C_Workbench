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

        #region 行列布局
        // —— 单元格尺寸与间距（用于“等距排列”）
        private double _gridCellWidth = 420;
        public double GridCellWidth
        {
            get => _gridCellWidth;
            set => SetProperty(ref _gridCellWidth, Math.Max(50, value));
        }

        private double _gridCellHeight = 320;
        public double GridCellHeight
        {
            get => _gridCellHeight;
            set => SetProperty(ref _gridCellHeight, Math.Max(50, value));
        }

        private double _gridGapX = 16;
        public double GridGapX
        {
            get => _gridGapX;
            set => SetProperty(ref _gridGapX, Math.Max(0, value));
        }

        private double _gridGapY = 16;
        public double GridGapY
        {
            get => _gridGapY;
            set => SetProperty(ref _gridGapY, Math.Max(0, value));
        }

        // 起始坐标（左上角偏移）
        private double _gridStartLeft = 8;
        public double GridStartLeft
        {
            get => _gridStartLeft;
            set => SetProperty(ref _gridStartLeft, value);
        }

        private double _gridStartTop = 8;
        public double GridStartTop
        {
            get => _gridStartTop;
            set => SetProperty(ref _gridStartTop, value);
        }

        // 画布尺寸（你已有）
        private double _canvasWidth = 5000;
        public double CanvasWidth
        {
            get => _canvasWidth;
            set => SetProperty(ref _canvasWidth, value);
        }

        private double _canvasHeight = 3000;
        public double CanvasHeight
        {
            get => _canvasHeight;
            set => SetProperty(ref _canvasHeight, value);
        }

        // 是否在修改行列时自动应用布局
        private bool _autoApplyLayout = true;
        public bool AutoApplyLayout
        {
            get => _autoApplyLayout;
            set => SetProperty(ref _autoApplyLayout, value);
        }

        // （可选）是否把控件大小统一为单元格大小
        private bool _normalizeSizeOnLayout = false;
        public bool NormalizeSizeOnLayout
        {
            get => _normalizeSizeOnLayout;
            set => SetProperty(ref _normalizeSizeOnLayout, value);
        }
        #endregion
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
            foreach (var c in WatchViewModel.WatchChartGroups)
                if (c.Id == "placeholder")
                    c.Order = int.MinValue;
            WatchGroups = WatchViewModel.WatchChartGroups;

            // ★ 列表页用独立视图（不会影响默认视图）
            WatchGroupsView = new ListCollectionView(WatchViewModel.WatchChartGroups);
            WatchGroupsView.Filter = o => o is WatchChartModel m && !IsPlaceholder(m);

            // ★关键：按 Order 排序（拖拽时交换 Order 才会重排）
            WatchGroupsView.SortDescriptions.Clear();
            WatchGroupsView.SortDescriptions.Add(
                new SortDescription(nameof(WatchChartModel.Order), ListSortDirection.Ascending));

            WatchGroupsView.Refresh();

            // 监听集合变化，自动刷新过滤
            WatchViewModel.WatchChartGroups.CollectionChanged += OnChartsChanged;
           
        }
        private void OnChartsChanged(object sender, NotifyCollectionChangedEventArgs e)
        => WatchGroupsView?.Refresh();
        #endregion

        #region Command
        public void ApplyGridLayout()
        {
            var items = WatchGroups?.ToList();
            if (items == null || items.Count == 0) return;

            int rows = Math.Max(1, GridRows);
            int colsMin = Math.Max(1, GridColumns);

            int total = items.Count;
            // 以行数固定为基准，计算需要的列数
            int neededCols = (int)Math.Ceiling(total / (double)rows);
            int cols = Math.Max(colsMin, neededCols);

            double cellW = Math.Max(50, GridCellWidth);
            double cellH = Math.Max(50, GridCellHeight);
            double hgap = Math.Max(0, GridGapX);
            double vgap = Math.Max(0, GridGapY);

            // 可选：统一尺寸
            if (NormalizeSizeOnLayout)
            {
                foreach (var it in items)
                {
                    it.ChartWidth = cellW;
                    it.ChartHeight = cellH;
                }
            }

            // 行优先：第 i 个元素 → (row = i % rows, col = i / rows)
            for (int i = 0; i < total; i++)
            {
                int r = i % rows;
                int c = i / rows;

                var it = items[i];
                it.Left = GridStartLeft + c * (cellW + hgap);
                it.Top = GridStartTop + r * (cellH + vgap);
            }

            // 扩展画布，避免被裁剪
            CanvasWidth = GridStartLeft + cols * (cellW + hgap) + 200;
            CanvasHeight = GridStartTop + rows * (cellH + vgap) + 200;
        }
        #endregion
    }
}
