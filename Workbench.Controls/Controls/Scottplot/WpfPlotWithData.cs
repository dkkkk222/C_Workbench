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
using Microsoft.SqlServer.Server;
using Prism.Services.Dialogs;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace Workbench.Controls.Controls.Scottplot
{
    public class WpfPlotSteamBase : WpfPlot
    {// 类字段
        private double _currentX;          // 当前最新的 OADate
        private double _stepPerPoint;      // 每个点的增量（以 OADate 为单位）

        private static string PlotFont = "Noto Sans TC";
        public int MaxPointCount = 500;
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
            Plot.XLabel(XAxisName, 22);
            Plot.YLabel(YAxisName, 22);

            this.Plot.Axes.Bottom.Label.Alignment = Alignment.UpperRight;
            //this.Plot.Axes.Bottom.Label.Alignment = Alignment.MiddleRight;
            //this.Plot.Axes.Bottom.Label.OffsetX = 270;
            this.Plot.Axes.Left.Label.Alignment = Alignment.UpperRight;
            //this.Plot.Axes.Left.Label.OffsetY = -140;
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
            MaxPointCount = defaultXCount;
            xs = Enumerable.Range(0, defaultXCount).Select(i => (double)i).ToList();
            Refresh();
            DeviceIP = deviceIP;
            DeviceProt = deviceProt;

            
            base.MouseRightButtonDown -= WpfPlotSteamBase_MouseRightButtonDown;
            base.MouseRightButtonDown += WpfPlotSteamBase_MouseRightButtonDown;
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

            item = new MenuItem() { Header = "自动缩放" };
            item.Click += (o, i) => Autoscale();
            cm.Items.Add(item);

            item = new MenuItem() { Header = "新窗口打开" };
            item.Click += (o, i) => OpenInNewWindow();
            cm.Items.Add(item);

            item = new MenuItem() { Header = "复制到剪贴板" };
            item.Click += (o, i) => CopyImageToClipboard();
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
            {
                try
                {
                    ImageFormat format;
                    format = ImageFormats.FromFilename(sfd.FileName);
                    PixelSize lastRenderSize = Plot.RenderManager.LastRender.FigureRect.Size;
                    Plot.Save(sfd.FileName, (int)lastRenderSize.Width, (int)lastRenderSize.Height, format);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Image save failed", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }        
            }
                
        }
        public void CopyImageToClipboard()
        {
            PixelSize lastRenderSize = Plot.RenderManager.LastRender.FigureRect.Size;
            ScottPlot.Image bmp = Plot.GetImage((int)lastRenderSize.Width, (int)lastRenderSize.Height);
            byte[] bmpBytes = bmp.GetImageBytes();

            using MemoryStream ms = new();
            ms.Write(bmpBytes, 0, bmpBytes.Length);
            BitmapImage bmpImage = new();
            bmpImage.BeginInit();
            bmpImage.StreamSource = ms;
            bmpImage.EndInit();
            Clipboard.SetImage(bmpImage);
        }
        public void OpenInNewWindow()
        {
            WpfPlotViewer.Launch(Plot, "Interactive Plot");
            Refresh();
        }
        public void Autoscale()
        {
            Plot.Axes.AutoScale();
            Refresh();
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

        public void SetXYLimit(int MaxX = 5000, int MinX = 0, int MaxY = 300, int MinY = -300)
        {
            if (MinX < MaxX && MaxX > MinX)
            {
                Plot.Axes.SetLimits(left: MinX, right: MaxX);
            }
            if (MinY < MaxY && MaxY > MinY)
            {
                Plot.Axes.SetLimits(bottom: MinY, top: MaxY);
                //this.Plot.YAxis.SetSizeLimit(MinY, MaxY);
            }

            Refresh();
        }

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
                var listDouble = ListY[name];

                listDouble.Add(value);
                if (listDouble.Count > MaxPointCount)
                {
                    int excessCount = listDouble.Count - MaxPointCount;
                    listDouble.RemoveRange(0, excessCount);  // 移除最前面多余的数
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
                if (isAuto)
                {
                    //Plot.Axes.AutoScale(true);
                    //if (xs != null && xs.Count > 1 && xs.First() < xs.Last())
                    //    Plot.Axes.SetLimitsX(xs.First(), xs.Last());
                }
                else
                {

                    //Plot.Axes.AutoScaleX();
                }
                Refresh();
            }
            catch (Exception ex)
            { }
        }
    }
}
