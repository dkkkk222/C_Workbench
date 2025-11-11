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
using PPEC.Communication.Model;
using Prism.Commands;
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

        private ICollectionView _watchGroupsView;
        public ICollectionView WatchGroupsView
        {
            get => _watchGroupsView;
            private set => SetProperty(ref _watchGroupsView, value);
        }

        private static bool IsPlaceholder(WatchGroup m)
      => m != null && string.Equals(m.Header, "未选中", StringComparison.Ordinal);
        #region ItemsControl 布局控制

        private int gridRows = 2;
        public int GridRows
        {
            get => gridRows;
            set => SetProperty(ref gridRows, value);
        }

        private int gridColumns = 3; // 0 表示自动
        public int GridColumns
        {
            get => gridColumns;
            set => SetProperty(ref gridColumns, value);
        }

        #region 行列布局
        // 单元格尺寸与间距（默认跟你表格 540×360）
        private double _gridCellWidth = 540;
        public double GridCellWidth { get => _gridCellWidth; set => SetProperty(ref _gridCellWidth, Math.Max(50, value)); }

        private double _gridCellHeight = 360;
        public double GridCellHeight { get => _gridCellHeight; set => SetProperty(ref _gridCellHeight, Math.Max(50, value)); }

        private double _gridGapX = 16;
        public double GridGapX { get => _gridGapX; set => SetProperty(ref _gridGapX, Math.Max(0, value)); }

        private double _gridGapY = 16;
        public double GridGapY { get => _gridGapY; set => SetProperty(ref _gridGapY, Math.Max(0, value)); }

        private double _gridStartLeft = 8;
        public double GridStartLeft { get => _gridStartLeft; set => SetProperty(ref _gridStartLeft, value); }

        private double _gridStartTop = 8;
        public double GridStartTop { get => _gridStartTop; set => SetProperty(ref _gridStartTop, value); }

        // 画布尺寸
        private double _canvasWidth = 5000;
        public double CanvasWidth { get => _canvasWidth; set => SetProperty(ref _canvasWidth, value); }

        private double _canvasHeight = 3000;
        public double CanvasHeight { get => _canvasHeight; set => SetProperty(ref _canvasHeight, value); }

        // 是否修改行列就自动应用布局
        private bool _autoApplyLayout = false;
        public bool AutoApplyLayout { get => _autoApplyLayout; set => SetProperty(ref _autoApplyLayout, value); }

        // 首次/常用建议：统一尺寸避免覆盖
        private bool _normalizeSizeOnLayout = true;
        public bool NormalizeSizeOnLayout { get => _normalizeSizeOnLayout; set => SetProperty(ref _normalizeSizeOnLayout, value); }

        #endregion
        private bool useWrapLayout = false;
        /// <summary>
        /// false = UniformGrid 等分；true = WrapPanel 流式布局
        /// </summary>
        public bool UseWrapLayout
        {
            get => useWrapLayout;
            set => SetProperty(ref useWrapLayout, value);
        }

        private Orientation itemOrientation = Orientation.Horizontal;
        /// <summary>
        /// WrapPanel 排布方向（横/竖）
        /// </summary>
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

        // 如使用 WrapPanel 并希望统一卡片尺寸，可设置这两个值
        private double cellWidth = 560;
        public double CellWidth
        {
            get => cellWidth;
            set => SetProperty(ref cellWidth, value);
        }

        private double cellHeight = 220;
        public double CellHeight
        {
            get => cellHeight;
            set => SetProperty(ref cellHeight, value);
        }

        #endregion

        private double _tableWidth = 520;   // 初始宽
        public double TableWidth
        { 
            get => _tableWidth; 
            set 
            {
                SetProperty(ref _tableWidth, value);
            }
        }

        private double _tableHeight = 360;   // 初始高
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

            // ★ 列表页用独立视图（不会影响默认视图）
            WatchGroupsView = new ListCollectionView(WatchViewModel.WatchGroups);
            WatchGroupsView.Filter = o => o is WatchGroup m && !IsPlaceholder(m);

            // ★关键：按 Order 排序（拖拽时交换 Order 才会重排）
            WatchGroupsView.SortDescriptions.Clear();
            WatchGroupsView.SortDescriptions.Add(
                new SortDescription(nameof(WatchGroup.Order), ListSortDirection.Ascending));

            WatchGroupsView.Refresh();

            WatchViewModel.WatchGroups.CollectionChanged += OnChartsChanged;
            ApplyGridLayout();
        }

        private void OnChartsChanged(object sender, NotifyCollectionChangedEventArgs e)
      => WatchGroupsView?.Refresh();

        #endregion
        #region Command
        public DelegateCommand ApplyLayoutCommand => new DelegateCommand(() =>
        {
            ApplyGridLayout();
        });
        #endregion

        private static bool ShouldLayoutItem(WatchGroup it)
        {
            if (it == null) return false;
            // 仅在“应用布局”时过滤：Header == "未选中" 不参与布局，但不会从 ItemsControl 移除
            if (!string.IsNullOrWhiteSpace(it.Header) && it.Header.Trim() == "未选中")
                return false;

            // 如果你还有其它的控制字段，可在此补充
            return true;
        }

        public void ApplyGridLayout()
        {
            var all = WatchGroups.ToList();
            var items = all.Where(ShouldLayoutItem).ToList();   // 仅布局可见项
            if (items.Count == 0) return;

            int rows = Math.Max(1, GridRows);
            int minCols = Math.Max(1, GridColumns);
            int total = items.Count;

            // 以“行”为基准，列数按需扩展（多的往后排）
            int cols = Math.Max(minCols, (int)Math.Ceiling(total / (double)rows));

            // 基础参数
            double cellW = Math.Max(50, GridCellWidth);
            double cellH = Math.Max(50, GridCellHeight);
            double hgap = Math.Max(0, GridGapX);
            double vgap = Math.Max(0, GridGapY);
            double startX = GridStartLeft;
            double startY = GridStartTop;

            // 列宽/行高
            var colWidths = new double[cols];
            var rowHeights = new double[rows];

            if (NormalizeSizeOnLayout)
            {
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;
                foreach (var it in items)
                {
                    it.TableWidth = cellW;
                    it.TableHeight = cellH;
                }
            }
            else
            {
                for (int c = 0; c < cols; c++) colWidths[c] = cellW;
                for (int r = 0; r < rows; r++) rowHeights[r] = cellH;

                // 列优先索引：确保左上角不空
                for (int i = 0; i < total; i++)
                {
                    int c = i / rows; // 先竖着排满 rows，再换列
                    int r = i % rows;
                    var it = items[i];
                    double w = Math.Max(50, it.TableWidth);
                    double h = Math.Max(50, it.TableHeight);
                    if (w > colWidths[c]) colWidths[c] = w;
                    if (h > rowHeights[r]) rowHeights[r] = h;
                }
            }

            // 前缀和：每列X、每行Y
            var colX = new double[cols];
            var rowY = new double[rows];
            colX[0] = startX;
            for (int c = 1; c < cols; c++)
                colX[c] = colX[c - 1] + colWidths[c - 1] + hgap;

            rowY[0] = startY;
            for (int r = 1; r < rows; r++)
                rowY[r] = rowY[r - 1] + rowHeights[r - 1] + vgap;

            // 放置：列优先（保证左上角先占用）
            for (int i = 0; i < total; i++)
            {
                int c = i / rows;
                int r = i % rows;
                var it = items[i];
                it.Left = colX[c];
                it.Top = rowY[r];
            }

            // 画布扩展（按参与布局的末列/末行计算）
            CanvasWidth = colX[cols - 1] + colWidths[cols - 1] + 400;
            CanvasHeight = rowY[rows - 1] + rowHeights[rows - 1] + 400;
        }
    }
}
