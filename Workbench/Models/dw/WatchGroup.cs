using Newtonsoft.Json;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Views;
using Workbench.Views.Windows;

namespace Workbench.Models.dw
{
    public class WatchGroup : BindableBase
    {
        private readonly IDialogService _dialogService;
        public WatchGroup(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }
        private string _id;
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _header;
        public string Header
        {
            get { return _header; }
            set { SetProperty(ref _header, value); }
        }

        private string _tableName = "状态监测表";
        public string TableName
        {
            get { return _tableName; }
            set { SetProperty(ref _tableName, value); }
        }

        private string _chartName = "状态监测图";
        public string ChartName
        {
            get { return _chartName; }
            set { SetProperty(ref _chartName, value); }
        }

        #region ChartPropety
        public int chart1MaxX = 5000;
        public int Chart1MaxX
        {
            get => chart1MaxX;
            set => SetProperty(ref chart1MaxX, value);
        }
        public int chart1MinX = 0;
        public int Chart1MinX
        {
            get => chart1MinX;
            set => SetProperty(ref chart1MinX, value);
        }

        public int chart1MaxY = 300;
        public int Chart1MaxY
        {
            get => chart1MaxY;
            set => SetProperty(ref chart1MaxY, value);
        }
        public int chart1MinY = -300;
        public int Chart1MinY
        {
            get => chart1MinY;
            set => SetProperty(ref chart1MinY, value);
        }
        #endregion

        private ObservableCollection<BitField> _bitFields = new ObservableCollection<BitField>();
        public ObservableCollection<BitField> BitFields
        {
            get { return _bitFields; }
            set { SetProperty(ref _bitFields, value); }
        }

        private ObservableCollection<TableColumn> _tableColumns = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableColumn> TableColumns
        {
            get { return _tableColumns; }
            set { SetProperty(ref _tableColumns, value); }
        }

        //[JsonIgnore]
        //public WpfPlot PlotControl { get; } = new WpfPlot();
        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "X", "Y", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl 
        { 
            get=> wpfPlotControl;
            set=>SetProperty(ref wpfPlotControl,value); 
        } 

        private DelegateCommand<string> _renameCommand;
        public DelegateCommand<string> RenameCommand => _renameCommand ?? (_renameCommand = new DelegateCommand<string>((param) =>
        {
            IDialogParameters dialogParameters = new DialogParameters();
            dialogParameters.Add("NameType", param);
            _dialogService.Show(nameof(RenameView), dialogParameters, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    var NameType = r.Parameters.GetValue<string>("NameType");
                    var ShowName = r.Parameters.GetValue<string>("ShowName");
                    if (NameType == "Table")
                    {
                        TableName = ShowName;
                    }
                    if (NameType == "Chart")
                    {
                        ChartName = ShowName;
                    }
                    
                }
            }, nameof(RenameWindow));
        }));

        private DelegateCommand<BitField> _addToChartCommand;
        public DelegateCommand<BitField> AddToChartCommand =>
            _addToChartCommand ?? (_addToChartCommand = new DelegateCommand<BitField>(OnAddToChart));

        private void OnAddToChart(BitField field)
        {
            if (field == null) return;

            var matched=BitFields.FirstOrDefault(f => f.AddressHexName == field.AddressHexName&&f.StartBit==field.StartBit);

            var plotConfig = WpfPlotControl.Plot.GetPlottables();
            var legLabel = plotConfig.Where(x => (x as Scatter).LegendText.Equals(field.Desc)).FirstOrDefault();
            
            if (legLabel == null)
            {
                //添加通道信息到波形图
                WpfPlotControl.AddSignalData(field.Desc);
            }
            else
            {
                if (!field.IsSelected)
                {
                    legLabel.IsVisible = false;
                }
                else
                {
                    legLabel.IsVisible = true;
                }
            }
        }

        private DelegateCommand<object> _settingChartLimitCommand;
        public DelegateCommand<object> SettingChartLimitCommand =>
            _settingChartLimitCommand ?? (_settingChartLimitCommand = new DelegateCommand<object>(SettingChartLimit));

        private void SettingChartLimit(object o)
        {
            WpfPlotControl.SetXYLimit(MaxX: Chart1MaxX,MinX:Chart1MinX,MaxY:Chart1MaxY,MinY:Chart1MinY);
        }
    }
}
