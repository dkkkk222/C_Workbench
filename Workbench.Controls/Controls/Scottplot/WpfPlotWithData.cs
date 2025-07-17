using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Controls;
using ScottPlot.WPF;
using ScottPlot.Plottables;
using ScottPlot;
using System.Text.RegularExpressions;

namespace Workbench.Controls.Controls.Scottplot
{
    public class WpfPlotSteamBase : WpfPlot
    {// 类字段
        private double _currentX;          // 当前最新的 OADate
        private double _stepPerPoint;      // 每个点的增量（以 OADate 为单位）

        private static string PlotFont = "Noto Sans TC";
        public const int MaxPointCount = 500;
        private static Regex LabelReg = new Regex(@"(?<=:)\d+(\.\d+)?", RegexOptions.Compiled);
        public string Label;
        public bool IsUpdate { get; set; } = true;
        private List<double> xs { get; set; } = new List<double>();
        //Dictionary<string, List<double>> ListX = new Dictionary<string, List<double>>();
        Dictionary<string, List<double>> ListY = new Dictionary<string, List<double>>();
        public Plot _Plot;

        public string DeviceIP { get; set; }
        public int DeviceProt { get; set; }
        public WpfPlotSteamBase(string plotName, string XAxisName, string YAxisName, double? xMin = 0, double? xMax = 500, double? yMin = -10, double? yMax = 10, int defaultXCount = 500, int tickDensityRatio = 1, string deviceIP = null, int deviceProt = 0, int intervalMs = 5, DateTime? start = null)
        {
            DeviceIP = deviceIP;
            DeviceProt = deviceProt;
            Plot.Axes.Title.Label.FontName = PlotFont;
            Name = plotName;

            _Plot = Plot;
            Plot.Axes.SetLimits(xMin,  xMax,  yMin,  yMax);
            this.Plot.Grid.IsVisible = true;
            Plot.Legend.IsVisible = true;

            //base.MouseRightButtonDown -= WpfPlotSteamBase_MouseRightButtonDown;
            //base.MouseRightButtonDown += WpfPlotSteamBase_MouseRightButtonDown;
            Plot.XLabel("间距(s)", 22);
            Plot.YLabel("幅值(V)", 22);
            Plot.Font.Automatic();

            //// change figure colors
            //Plot.FigureBackground.Color = Color.FromHex("#0C2A56");
            //Plot.DataBackground.Color = Color.FromHex("#0C2A56");

            //// change axis and grid colors
            //Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            //Plot.Grid.MajorLineColor = Color.FromHex("#404040");

            //// change legend colors
            //Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            //Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
            //Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

            xs = Enumerable.Range(0, defaultXCount).Select(i => (double)i).ToList();
            Refresh();
            DeviceIP = deviceIP;
            DeviceProt = deviceProt;
        }
        #region 右键
        private void WpfPlotSteamBase_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuItem item;
            var cm = new ContextMenu();

            //item = new MenuItem() { Header = "刷新" };
            //item.Click += (o, i) => ResetThisPlot();
            //cm.Items.Add(item);

            item = new MenuItem() { Header = "暂停" };
            item.Click += (o, i) => StopUpdate();
            cm.Items.Add(item);

            item = new MenuItem() { Header = "开始" };
            item.Click += (o, i) => StartUpdate();
            cm.Items.Add(item);

            item = new MenuItem() { Header = "保存图片" };
            item.Click += (o, i) => SaveImage();
            cm.Items.Add(item);

            cm.IsOpen = true;
        }
        public void SaveImage()
        {
            var sfd = new SaveFileDialog
            {
                FileName = "ScottPlot.png",
                Filter = "PNG Files (*.png)|*.png" +
                            "|JPG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                            "|BMP Files (*.bmp)|*.bmp" +
                            "|All files (*.*)|*.*"
            };

            if (sfd.ShowDialog() is true)
                Plot.Save(sfd.FileName, 80, 80);
        }
        public void StopUpdate()
        {
            IsUpdate = false;
        }
        public void StartUpdate()
        {
            IsUpdate = true;
        }
        #endregion

        public void Init(int i, Plot Plot, DateTime start)
        {
            Plot.Axes.Title.Label.FontName = PlotFont;
            Plot.Legend.IsVisible = true;
        }

        public Scatter AddSignalData(string name)
        {
            if (ListY.ContainsKey(name))
                return null;
            List<double> ys = new List<double>();
            //List<double> _xs = new List<double>();
            var newScatter = Plot.Add.ScatterLine(xs, ys);
            newScatter.LegendText = name;
            newScatter.LineWidth = 2;
            newScatter.MarkerShape = MarkerShape.FilledCircle;

            ListY.Add(name, ys);
            //ListX.Add(name, _xs);
            return newScatter;
        }
        public void ClearDate(string name)
        {
            if (ListY.ContainsKey(name))
            {
                var listDouble = ListY[name];
                listDouble.Clear();
            }
        }
        public void UpdateData(string name, double[] value, DateTime xdate=default)
        {
            if (!IsUpdate)
                return;
            if (value == null)
                return;
            if (ListY.ContainsKey(name))
            {
                //for (int i = 0; i < value.Length; i++)
                //{
                //    _currentX += _stepPerPoint;    // 在上一个基础上加步长
                //    xs.Add(_currentX);
                //}
                var addX = (int)(xs.Max(x => x) + 1);
                var addX1=Enumerable.Range(addX, value.Count()).Select(i => (double)i).ToList();
                xs.AddRange(addX1);

                var listDouble = ListY[name];
                
                listDouble.AddRange(value);
                if (listDouble.Count > MaxPointCount)
                {
                    int excessCount = listDouble.Count - MaxPointCount;
                    listDouble.RemoveRange(0, excessCount);  // 移除最前面多余的数
                    xs.RemoveRange(0, excessCount);
                }

            }
        }

        public void AddPointForY()
        {
            int maxPoint = xs.Count;// ListY.Values.Max(x=>x.Count);
            //执行补点操作
            foreach (var kv in ListY)
            {
                string lineName = kv.Key;
                List<double> lineY = kv.Value;
                if (lineY.Count < maxPoint)
                {
                    lineY.AddRange(Enumerable.Repeat(0d, maxPoint - lineY.Count));
                }
            }
        }

        public void UpdateData(string name, double value, DateTime xdate=default)
        {
            if (!IsUpdate)
                return;
            if (ListY.ContainsKey(name))
            {
                //_currentX += _stepPerPoint;    // 在上一个基础上加步长
                //xs.Add(_currentX);
              

                var listDouble = ListY[name];

                listDouble.Add(value);
             
                if (listDouble.Count > MaxPointCount)
                {
                    int excessCount = listDouble.Count - MaxPointCount;
                    listDouble.RemoveRange(0, excessCount);  // 移除最前面多余的数

                    var addX = xs.Max(x => x) + 1;
                    xs.Add(addX);
                    xs.RemoveRange(0, excessCount);
              
                    //double xMax = xs.Max(x=>x);
                    //double xMin = xs.Min(x => x);
                    //Plot.Axes.SetLimitsX(xMin, xMax);
                }
                

            }
        }
        
        public void PlotAxisAutoData()
        {
            Plot.Axes.AutoScale();
        }
        public void RefreshData(bool isAuto = false)
        {
            if (!IsUpdate)
                return;
            try
            {
                double xMax = xs.Max(x=>x);
                double xMin = xs.Min(x => x);
                Plot.Axes.SetLimitsX(xMin, xMax);
                 

                // 自动缩放 Y 轴
                Plot.Axes.AutoScaleY();
                Refresh();
            }
            catch (Exception ex)
            { }
        }
    }
}
