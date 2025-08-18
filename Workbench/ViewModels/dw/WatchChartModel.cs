using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Workbench.Controls.Controls.Scottplot;

namespace Workbench.ViewModels.dw
{
    public class WatchChartModel:BindableBase
    {
        public WatchChartModel(string chartName)
        {
            ChartName= chartName;
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
        private double _chartHeight = 360;   // 初始高
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
                if (value < 360)
                    value = 360;
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
        private WpfPlotSteamBase wpfPlotControl2 = new WpfPlotSteamBase("监测图", "X", "Y", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl2
        {
            get => wpfPlotControl2;
            set => SetProperty(ref wpfPlotControl2, value);
        }

        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "X", "Y", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl
        {
            get => wpfPlotControl;
            set => SetProperty(ref wpfPlotControl, value);
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
    }
}
