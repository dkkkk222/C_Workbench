using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Prism.Commands;
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
        private double _designerGridSize = 10;
        public double DesignerGridSize
        {
            get => _designerGridSize;
            set => SetProperty(ref _designerGridSize, Math.Max(1, value));
        }
        private bool _isGridVisible = true;
        public bool IsGridVisible
        {
            get => _isGridVisible;
            set => SetProperty(ref _isGridVisible, value);
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

        private int gridColumns = 3; // 0 自动
        public int GridColumns
        {
            get => gridColumns;
            set => SetProperty(ref gridColumns, value);
        }

        #region 行列布局
        // —— 单元格尺寸与间距（用于“等距排列”）
        private double _gridCellWidth = 540;
        public double GridCellWidth
        {
            get => _gridCellWidth;
            set => SetProperty(ref _gridCellWidth, Math.Max(50, value));
        }

        private double _gridCellHeight = 360;
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
            ApplyGridLayout();
        }
        private void OnChartsChanged(object sender, NotifyCollectionChangedEventArgs e)
        => WatchGroupsView?.Refresh();
        #endregion

        #region Command
        public DelegateCommand ToggleGridCommand => new DelegateCommand(() =>
        {
            IsGridVisible = !IsGridVisible;
        });
        public DelegateCommand ApplyLayoutCommand => new DelegateCommand(() =>
        {
            ApplyGridLayout();
        });


        private static bool ShouldLayoutItem(object item)
        {
            if (item == null) return false;
            var t = item.GetType();

            // 1) 明确按你的描述：Header == "未选中" 则跳过
            var pHeader = t.GetProperty("Header", BindingFlags.Instance | BindingFlags.Public);
            if (pHeader != null)
            {
                var header = pHeader.GetValue(item) as string;
                if (!string.IsNullOrWhiteSpace(header) && header.Trim() == "未选中")
                    return false;
            }

            // 2) 可选：如果你的 ViewModel 还有“是否参与布局”的标记，就顺带支持一下
            //    （没有的话这段不会生效，不影响）
            string[] flags = { "IsVisibleForLayout", "IsShown", "IsDisplay", "ShouldLayout", "IsVisible" };
            foreach (var name in flags)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.PropertyType == typeof(bool))
                {
                    bool b = (bool)p.GetValue(item);
                    if (!b) return false;
                }
            }

            return true;
        }

        public void ApplyGridLayout()
        {
            var all = WatchGroups == null ? null : WatchGroups.ToList();
            if (all == null || all.Count == 0) return;

            // —— 只拿需要布局的项（过滤掉 Header="未选中" 的那一条）
            var items = all.Where(ShouldLayoutItem).ToList();

            if (items.Count == 0)
            {
                CanvasWidth = GridStartLeft + 400;
                CanvasHeight = GridStartTop + 300;
                return;
            }

            // ====== 新增：网格吸附工具 ======
            double grid = Math.Max(1, DesignerGridSize);

            double SnapRound(double v) => Math.Round(v / grid) * grid;              // 位置：四舍五入
            double SnapUp(double v) => Math.Ceiling(v / grid) * grid;               // 尺寸/间距：向上取整，避免压叠

            // 行列
            int rows = Math.Max(1, GridRows);
            int minCol = Math.Max(1, GridColumns);
            int total = items.Count;
            int cols = Math.Max(minCol, (int)Math.Ceiling(total / (double)rows));

            // 基础参数（全部网格化）
            double cellW = SnapUp(Math.Max(50, GridCellWidth));
            double cellH = SnapUp(Math.Max(50, GridCellHeight));
            double hgap = SnapUp(Math.Max(0, GridGapX));
            double vgap = SnapUp(Math.Max(0, GridGapY));

            double startX = SnapRound(GridStartLeft);
            double startY = SnapRound(GridStartTop);

            // —— 计算每列宽/每行高（用于列/行起点计算）
            var colWidths = new double[cols];
            var rowHeights = new double[rows];

            if (NormalizeSizeOnLayout)
            {
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;

                // 可选：如果你希望布局后尺寸也落到网格上，这里已经是 cellW/cellH（网格化后）
                foreach (var it in items)
                {
                    it.ChartWidth = cellW;
                    it.ChartHeight = cellH;
                }
            }
            else
            {
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;

                // 按“列优先”统计：每列最大宽、每行最大高（然后向上吸附到网格，保证下一列/行起点也在网格上）
                for (int i = 0; i < total; i++)
                {
                    int c = i / rows;
                    int r = i % rows;

                    double w = Math.Max(50, items[i].ChartWidth);
                    double h = Math.Max(50, items[i].ChartHeight);

                    if (w > colWidths[c]) colWidths[c] = w;
                    if (h > rowHeights[r]) rowHeights[r] = h;
                }

                // 关键：列宽/行高向上吸附到网格，确保累加后的 colX/rowY 全部落网格
                for (int c = 0; c < cols; c++) colWidths[c] = SnapUp(colWidths[c]);
                for (int r = 0; r < rows; r++) rowHeights[r] = SnapUp(rowHeights[r]);
            }

            // —— 前缀和：每列的 X 起点、每行的 Y 起点（天然落在网格上）
            var colX = new double[cols];
            var rowY = new double[rows];

            colX[0] = startX;
            for (int c = 1; c < cols; c++)
                colX[c] = colX[c - 1] + colWidths[c - 1] + hgap;

            rowY[0] = startY;
            for (int r = 1; r < rows; r++)
                rowY[r] = rowY[r - 1] + rowHeights[r - 1] + vgap;

            // —— 放置（列优先）
            for (int i = 0; i < total; i++)
            {
                int c = i / rows;
                int r = i % rows;

                items[i].Left = colX[c];  // 已经是网格对齐
                items[i].Top = rowY[r];  // 已经是网格对齐
            }

            // —— 防空首格（保持你原逻辑，shift 也是网格倍数）
            if (cols >= 2)
            {
                bool firstColOccupied = items.Any(it => Math.Abs(it.Left - colX[0]) <= 0.5);
                if (!firstColOccupied)
                {
                    double shift = (colX[1] - colX[0]);
                    foreach (var it in items) it.Left -= shift;
                    for (int c = 0; c < cols; c++) colX[c] -= shift;
                }
            }

            // —— 扩展画布（也向上吸附网格，看起来更“整齐”）
            CanvasWidth = SnapUp(colX[cols - 1] + colWidths[cols - 1] + 200);
            CanvasHeight = SnapUp(rowY[rows - 1] + rowHeights[rows - 1] + 200);
        }

        public void ApplyGridLayout1()
        {
            var all = WatchGroups == null ? null : WatchGroups.ToList();
            if (all == null || all.Count == 0) return;

            // —— 只拿需要布局的项（过滤掉 Header="未选中" 的那一条）
            var items = all.Where(ShouldLayoutItem).ToList();

            // 没有要布局的就收个尾
            if (items.Count == 0)
            {
                CanvasWidth = GridStartLeft + 400;
                CanvasHeight = GridStartTop + 300;
                return;
            }

            int rows = Math.Max(1, GridRows);
            int minCol = Math.Max(1, GridColumns);
            int total = items.Count;

            // 以“行数”为基准，列数按需扩展（满足你的规则：超出就“往后排”）
            int cols = Math.Max(minCol, (int)Math.Ceiling(total / (double)rows));

            // 基础参数
            double cellW = Math.Max(50, GridCellWidth);   // 建议默认 540
            double cellH = Math.Max(50, GridCellHeight);  // 建议默认 360
            double hgap = Math.Max(0, GridGapX);
            double vgap = Math.Max(0, GridGapY);
            double startX = GridStartLeft;
            double startY = GridStartTop;

            // —— 计算每列宽/每行高（统一尺寸 or 自适应）
            var colWidths = new double[cols];
            var rowHeights = new double[rows];

            if (NormalizeSizeOnLayout)
            {
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;

                foreach (var it in items)
                {
                    it.ChartWidth = cellW;
                    it.ChartHeight = cellH;
                }
            }
            else
            {
                // 给个默认步长，避免 0 参与前缀和
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;

                // 按“列优先”索引统计：每列最大宽、每行最大高（不会互相压叠）
                for (int i = 0; i < total; i++)
                {
                    int c = i / rows; // 列优先：先竖着排满 rows，再到下一列
                    int r = i % rows;

                    double w = Math.Max(50, items[i].ChartWidth);
                    double h = Math.Max(50, items[i].ChartHeight);
                    if (w > colWidths[c]) colWidths[c] = w;
                    if (h > rowHeights[r]) rowHeights[r] = h;
                }
            }

            // —— 前缀和：每列的 X 起点、每行的 Y 起点
            var colX = new double[cols];
            var rowY = new double[rows];
            colX[0] = startX;
            for (int c = 1; c < cols; c++)
                colX[c] = colX[c - 1] + colWidths[c - 1] + hgap;

            rowY[0] = startY;
            for (int r = 1; r < rows; r++)
                rowY[r] = rowY[r - 1] + rowHeights[r - 1] + vgap;

            // —— 放置（列优先，保证左上角先占用；且只放“需要布局”的 items）
            for (int i = 0; i < total; i++)
            {
                int c = i / rows;
                int r = i % rows;
                items[i].Left = colX[c];
                items[i].Top = rowY[r];
            }

            // —— 防空首格（仅检查“参与布局”的 items）
            if (cols >= 2)
            {
                bool firstColOccupied = items.Any(it => Math.Abs(it.Left - colX[0]) <= 0.5);
                if (!firstColOccupied)
                {
                    double shift = (colX[1] - colX[0]); // 第一列步长 = colWidths[0] + hgap
                    foreach (var it in items) it.Left -= shift;
                    for (int c = 0; c < cols; c++) colX[c] -= shift;
                }
            }

            // —— 扩展画布（以“参与布局”的末列/末行为准），避免裁剪
            CanvasWidth = colX[cols - 1] + colWidths[cols - 1] + 200;
            CanvasHeight = rowY[rows - 1] + rowHeights[rows - 1] + 200;
        }
        #endregion
    }
}
