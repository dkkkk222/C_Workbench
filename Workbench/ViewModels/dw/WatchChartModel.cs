using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Views.Windows;
using Workbench.Views;

namespace Workbench.ViewModels.dw
{
    public class WatchChartModel:BindableBase
    {
        private string Session_id { get; set; }
        private IDialogService _dialogService;
        public WatchChartModel(string chartName, IDialogService dialogService=null, string session_id = null)
        {
            Session_id = session_id;
            _dialogService = dialogService;
            ChartName = chartName;
        }

        #region 位置
        private double _left;      // Canvas.Left
        private double _top;       // Canvas.Top

        public double Left
        {
            get { return _left; }
            set { SetProperty(ref _left, value); }
        }

        public double Top
        {
            get { return _top; }
            set { SetProperty(ref _top, value); }
        }

        #endregion
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
        public string _chartName;
        public string ChartName
        {
            get=> _chartName;
            set { 
            SetProperty(ref _chartName, value);
            }
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


        private double _chartWidth = 680;   // 初始宽
        private double _chartHeight = 450;   // 初始高
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
                if (value < 450)
                    value = 450;
                SetProperty(ref _chartHeight, value);
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
                WpfPlotControl2.Plot.XLabel(value, 22);
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
                WpfPlotControl2.Plot.YLabel(value, 22);
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
        private WpfPlotSteamBase wpfPlotControl2 = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl2
        {
            get => wpfPlotControl2;
            set => SetProperty(ref wpfPlotControl2, value);
        }

        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl
        {
            get => wpfPlotControl;
            set => SetProperty(ref wpfPlotControl, value);
        }

        public void Inject(IDialogService dialogService)
        {
            _dialogService = dialogService;         // 重建命令
        }

        private DelegateCommand<object> _settingChartLimitCommand;
        [JsonIgnore]
        public DelegateCommand<object> SettingChartLimitCommand =>
            _settingChartLimitCommand ?? (_settingChartLimitCommand = new DelegateCommand<object>(SettingChartLimit));

        private void SettingChartLimit(object o)
        {
            WpfPlotControl.SetXYLimit(MaxX: Chart1MaxX, MinX: Chart1MinX, MaxY: Chart1MaxY, MinY: Chart1MinY);
            WpfPlotControl2.SetXYLimit(MaxX: Chart1MaxX, MinX: Chart1MinX, MaxY: Chart1MaxY, MinY: Chart1MinY);
        }

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
                        ChartName = ShowName;
                    }
                    if (NameType == "Chart")
                    {
                        ChartName = ShowName;
                    }

                }
            }, nameof(RenameWindow));
        }));
    }
}
