using log4net;
using Newtonsoft.Json;
using NPOI.XSSF.UserModel;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Utils;
using Workbench.ViewModels.dw;
using Workbench.Views;
using Workbench.Views.Windows;

namespace Workbench.Models.dw
{
    public class WatchGroup : BindableBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WatchGroup));
        private IDialogService _dialogService;
        public string Session_id { get; set; }
        public WatchGroup(IDialogService dialogService,string session_id)
        {
            _dialogService = dialogService;
            Session_id = session_id;
        }
        private string _id;
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        private int _order;
        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
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

        #region TabHeight
        private double _tableWidth = 540;   // 初始宽
        private double _tableHeight = 360;   // 初始高
        public double TableWidth
        {
            get => _tableWidth;
            set
            {
                if (value < 540)
                    value = 540;
                SetProperty(ref _tableWidth, value);
            }
        }
        public double TableHeight
        {
            get => _tableHeight;
            set
            {
                if (value < 360)
                    value = 360;
                SetProperty(ref _tableHeight, value);
            }
        }
       
        private string _ChartXName = "间距";   // 初始高
        public string ChartXName
        {
            get => _ChartXName;
            set
            {
                SetProperty(ref _ChartXName, value);
                WpfPlotControl.Plot.XLabel(value, 22);
                WpfPlotControl.Plot.Axes.Right.Label.Text = value;
                WpfPlotControl2.Plot.XLabel(value, 22);
                WpfPlotControl2.Plot.Axes.Right.Label.Text = value;
            }
        }
        private string _ChartYName = "幅值";   // 初始宽
        public string ChartYName
        {
            get => _ChartYName;
            set
            {
                SetProperty(ref _ChartYName, value);
                WpfPlotControl.Plot.YLabel(value, 22);
                WpfPlotControl.Plot.Axes.Top.Label.Text = value;
                WpfPlotControl2.Plot.YLabel(value, 22);
                WpfPlotControl2.Plot.Axes.Top.Label.Text = value;
            }
        }

        private double _chartWidth = 680;   // 初始宽
        private double _chartHeight = 500;   // 初始高
        public double ChartWidth
        {
            get => _chartWidth;
            set
            {
                if (value < 680)
                    value = 680;
                SetProperty(ref _chartWidth, value);
                
            }
        }
        public double ChartHeight
        {
            get => _chartHeight;
            set
            {
                if (value < 500)
                    value = 500;
                SetProperty(ref _chartHeight, value);
            }
        }
        #endregion
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
        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl 
        { 
            get=> wpfPlotControl;
            set=>SetProperty(ref wpfPlotControl,value); 
        }

        private WpfPlotSteamBase wpfPlotControl2 = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl2
        {
            get => wpfPlotControl2;
            set => SetProperty(ref wpfPlotControl2, value);
        }
        #region Method
        private void HistoryToExcel(string path)
        {
            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Sheet1");
                var headerRow = sheet.CreateRow(0);
                string[] headerColumns = new string[] { "名称", "寄存器地址", "解析范围", "解析要求", "解析结果", "原始值(DEC)", "原始值(bit)", "单位" };
                for (int i = 0; i < headerColumns.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(headerColumns[i]);
                }

                int startRow = 1;
                foreach (var history in BitFields)
                {
                    var row = sheet.CreateRow(startRow);
                    row.CreateCell(0).SetCellValue(history.Desc);
                    row.CreateCell(1).SetCellValue(history.AddressHexName);
                    row.CreateCell(2).SetCellValue("b"+history.StartBit+ "-"+ "b" + history.EndBit);
                    row.CreateCell(3).SetCellValue(history.FormParam.ParamName);
                    row.CreateCell(4).SetCellValue(history.Result);
                    row.CreateCell(5).SetCellValue(history.Value);
                    row.CreateCell(6).SetCellValue(history.ReadBinary);
                    row.CreateCell(7).SetCellValue(history.FormParam.UnitName);
                    startRow++;
                }

                for (int i = 0; i < headerColumns.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                string fileName = "数据监测表-"+this.TableName+".xlsx";
                string filePath = Path.Combine(path, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }
                MessageBox.Show("下载成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void Inject(IDialogService dialogService)
        {
            _dialogService = dialogService;         // 重建命令
        }
        #endregion
        [JsonIgnore]
        public DelegateCommand SettingDefaultWHCommand => new DelegateCommand(() =>
        {
            TableWidth = 540;
            TableHeight = 360;
        });
        [JsonIgnore]
        public DelegateCommand SettingDefaultChartWHCommand => new DelegateCommand(() =>
        {
            ChartWidth = 680;
            ChartHeight = 360;
        });
        [JsonIgnore]
        public DelegateCommand HistoryDownloadCommand => new DelegateCommand(() =>
        {
            if (!BitFields.Any())
            {
                MessageBox.Show("无数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var fbd = new FolderBrowserDialog();
            fbd.Description = "请选择保存路径";
            var result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = fbd.SelectedPath;
                ExporterExcel exporterExcel = new ExporterExcel();
                exporterExcel.ExportSessionToExcel_MergedByTimeAndId(Session_id, path);
                //HistoryToExcel(path);
            }
        });

        private DelegateCommand<string> _renameCommand;
        [JsonIgnore]
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
        [JsonIgnore]
        public DelegateCommand<BitField> AddToChartCommand => new DelegateCommand<BitField>((e) =>
        {
            OnAddToChart(e); OnAddToChart2(e);
        });

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
        private void OnAddToChart2(BitField field)
        {
            if (field == null) return;

            var matched = BitFields.FirstOrDefault(f => f.AddressHexName == field.AddressHexName && f.StartBit == field.StartBit);

            var plotConfig = WpfPlotControl2.Plot.GetPlottables();
            var legLabel = plotConfig.Where(x => (x as Scatter).LegendText.Equals(field.Desc)).FirstOrDefault();

            if (legLabel == null)
            {
                //添加通道信息到波形图 
                WpfPlotControl2.AddSignalData(field.Desc);
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
        [JsonIgnore]
        public DelegateCommand<object> SettingChartLimitCommand =>
            _settingChartLimitCommand ?? (_settingChartLimitCommand = new DelegateCommand<object>(SettingChartLimit));

        private void SettingChartLimit(object o)
        {
            WpfPlotControl.SetXYLimit(MaxX: Chart1MaxX,MinX:Chart1MinX,MaxY:Chart1MaxY,MinY:Chart1MinY);
            WpfPlotControl2.SetXYLimit(MaxX: Chart1MaxX, MinX: Chart1MinX, MaxY: Chart1MaxY, MinY: Chart1MinY);
        }
    }
}
