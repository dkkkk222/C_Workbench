using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        #region ItemsControl 布局控制

        private int gridRows = 2;
        public int GridRows
        {
            get => gridRows;
            set => SetProperty(ref gridRows, value);
        }

        private int gridColumns = 0; // 0 表示自动
        public int GridColumns
        {
            get => gridColumns;
            set => SetProperty(ref gridColumns, value);
        }

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

        }

        #endregion
    }
}
