using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HarfBuzzSharp;
using LinqToDB;
using log4net;
using Newtonsoft.Json;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using ScottPlot.Plottables;
using Workbench.Communication;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Db;
using Workbench.Db.Tables;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;
using Workbench.ViewModels.dw;
using LinqToDB.Async;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryMonitViewModel : AvaDocument
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TelemetryMonitViewModel));
        /// <summary>
        /// 发送遥控查询指令
        /// </summary>
        public System.Timers.Timer _timer = new System.Timers.Timer();
        /// <summary>
        /// 更新Chart
        /// </summary>
        public System.Timers.Timer _ChartDataTimer = new System.Timers.Timer();
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private HistoryRecorderL2db _historyRecorderL2Db { get; set; }
        public TelemetryMonitViewModel(IEventAggregator eventAggregator, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            _timer.Interval = 1000; // 设置触发间隔
            _timer.Elapsed += _timer_Elapsed; ; // 设置触发事件

            _ChartDataTimer.Interval = 1000;
            _ChartDataTimer.Elapsed += ChartDataTimer_Tick;
            _projectManager.CurrentProject.EnsureSession();
            _historyRecorderL2Db = _projectManager.CurrentProject.HistoryRecorderL2Db;
            
            EventListener();
            SelectedCycle = CycleSource[0];
            InitData();
            InitListen();
        }

        public async void InitData()
        {
            try
            {
                //TagSource.Clear();
                //using (var db = new DbContext())
                //{
                //    var monitCode = await db.TelemetryTagTs.Where(t => t.ChipId == _projectManager.CurrentProject.Chip.ChipId).ToListAsync();
                //    TagSource.AddRange(monitCode);
                //}
                //if(TagSource!=null&& TagSource.Count>0)
                //    SelectTag = TagSource[0];
                if (_projectManager.CurrentProject.TelemetryMonitViewGrid.SelectedCycle!=null)
                {
                    CycleSource.Where(x => x.Label == _projectManager.CurrentProject.TelemetryMonitViewGrid.SelectedCycle.Label).FirstOrDefault();
                }
                ProjectTag = _projectManager.CurrentProject.TelemetryMonitViewGrid.ProjectTag;
                SplitterPositionLeft = _projectManager.CurrentProject.TelemetryMonitViewGrid.SplitterPositionLeft;
                SplitterPositionRight = _projectManager.CurrentProject.TelemetryMonitViewGrid.SplitterPositionRight;             

            }
            catch (Exception ex)
            {

            }

        }
        public void InitListen()
        {
            _eventAggregator.GetEvent<SaveProjectEvent>().Subscribe(e => {

                e.TelemetryMonitViewGrid.ProjectTag = ProjectTag;
                e.TelemetryMonitViewGrid.SelectedCycle = SelectedCycle;
                e.TelemetryMonitViewGrid.SplitterPositionLeft = SplitterPositionLeft;
                e.TelemetryMonitViewGrid.SplitterPositionRight = SplitterPositionRight;
            });
        }
        public System.Windows.GridLength splitterPositionLeft = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft
        {
            get => splitterPositionLeft;
            set => SetProperty(ref splitterPositionLeft, value);
        }

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, value);
        }

        private int _busy2;
        private void ChartDataTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref _busy2, 1) == 1) return;
            try
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    var listLable=WpfPlotControl.Plot.GetPlottables().OfType<Scatter>().ToList();
                    foreach(var lab in listLable)
                    {
                        var showLine=ShowTelemetryList.Where(x => x.Name == lab.LegendText).FirstOrDefault();
                        if(showLine!=null)
                        {
                            //chart.WpfPlotControl2.UpdateData(field.Desc, field.Result);
                            WpfPlotControl.UpdateData(lab.LegendText, showLine.SourceData);
                        }
                    }
                    WpfPlotControl.RefreshData(false);
                });
                var Record=(_projectManager.CurrentProject.CommService as PcmuUartService).GetLastTelemetry(1).FirstOrDefault();
               
                if (Record!=null)
                    _historyRecorderL2Db.EnqueueFrame(DateTime.Now, Record.Values);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _busy2, 0);
            }
        }
        private int _busy1;
        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref _busy1, 1) == 1) return;
            try
            {
                var convertProjectTag = UtilsFunc.HexStringToBytes(ProjectTag);
                _projectManager.CurrentProject.CommService?.QueryTelemetryOnceAsync(1000, convertProjectTag[0]);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    SelectCount= (_projectManager.CurrentProject.CommService as PcmuUartService).SelectCount;
                    ReturnCount = (_projectManager.CurrentProject.CommService as PcmuUartService).ReceiveCountSuc;
                });
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _busy1, 0);
            }
        }

        #region property
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

        private string _ChartXName = "间距";   // 初始高
        public string ChartXName
        {
            get => _ChartXName;
            set
            {
                SetProperty(ref _ChartXName, value);
                WpfPlotControl.Plot.XLabel(value, 22);
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
            }
        }
        #endregion
        public int _selectCount;
        public int SelectCount
        {
            get=> _selectCount; 
            set=>SetProperty(ref _selectCount,value);
        }

        public int _returnCount;
        public int ReturnCount
        {
            get => _returnCount;
            set => SetProperty(ref _returnCount, value);
        }

        private bool _IsStart=false;
        public bool IsStart
        {
            get => _IsStart;
            set => SetProperty(ref _IsStart, value);
        }
        private List<byte> _ProjectList=new List<byte>();
        public List<byte> ProjectList
        {
            get => _ProjectList;
            set => SetProperty(ref _ProjectList, value);
        }

        private string _ProjectTag="FF";
        public string ProjectTag
        {
            get => _ProjectTag;
            set=>SetProperty(ref _ProjectTag, value);
        }

        public ObservableCollection<OptionModel> _CycleSource = new ObservableCollection<OptionModel>()
        {
            new OptionModel()
        {
            Label = "100ms", Value = 0
        },
            new OptionModel()
        {
            Label = "500ms", Value = 1
        },
            new OptionModel()
        {
            Label = "1000ms", Value = 2
        }, new OptionModel()
        {
            Label = "1500ms", Value = 3
        },
            new OptionModel()
        {
            Label = "2000ms", Value = 4
        },
        };
        public ObservableCollection<OptionModel> CycleSource
        {
            get => _CycleSource;
            set => SetProperty(ref _CycleSource, value);
        }

        private OptionModel _SelectedCycle;
        /// <summary>
        /// 选择的档位
        /// </summary>
        public OptionModel SelectedCycle
        {
            get => _SelectedCycle;
            set
            {
                if (SetProperty(ref _SelectedCycle, value))
                {
                    int ms = 1000;
                    switch(value.Value)
                    {
                        case 0:
                            ms = 100;
                            break;
                        case 1:
                            ms = 500;
                            break;
                        case 2:
                            ms = 1000;
                            break;
                        case 3:
                            ms = 1500;
                            break;
                        case 4:
                            ms = 2000;
                            break;
                    }
                    _timer.Interval = ms;
                }
            }
        }

        //public ObservableCollection<TelemetryTagTable> tagSource=new ObservableCollection<TelemetryTagTable>();
        //public ObservableCollection<TelemetryTagTable> TagSource
        //{
        //    get => tagSource;
        //    set => SetProperty(ref tagSource, value);
        //}

        //public TelemetryTagTable selectTag;
        //public TelemetryTagTable SelectTag
        //{
        //    get => selectTag;
        //    set
        //    {
        //        if(SetProperty(ref selectTag, value))
        //        {

        //        }
        //        ProjectTag = value.Name;
        //    }
        //}

        private ObservableCollection<TelemetrySliceField> showTelemetryList = new ObservableCollection<TelemetrySliceField>();
        public ObservableCollection<TelemetrySliceField> ShowTelemetryList
        {
            get => showTelemetryList;
            set => SetProperty(ref showTelemetryList, value);
        }

        private ObservableCollection<TelemetryMonit> _bitFields = new ObservableCollection<TelemetryMonit>();
        public ObservableCollection<TelemetryMonit> BitFields
        {
            get { return _bitFields; }
            set { SetProperty(ref _bitFields, value); }
        }

        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl
        {
            get => wpfPlotControl;
            set => SetProperty(ref wpfPlotControl, value);
        }
        #endregion

        #region Command
        private DelegateCommand<object> _settingChartLimitCommand;
        [JsonIgnore]
        public DelegateCommand<object> SettingChartLimitCommand =>
            _settingChartLimitCommand ?? (_settingChartLimitCommand = new DelegateCommand<object>(SettingChartLimit));
        public DelegateCommand StartAllCommand => new DelegateCommand(() =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                System.Windows.Forms.MessageBox.Show("当前工程未连接", "提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(ProjectTag))
            {
                HandyControl.Controls.MessageBox.Show("请输入遥控标识!");
                return;
            }
            if (!_timer.Enabled)
            {
                _historyRecorderL2Db.Start();
                _timer.Start();
                _ChartDataTimer.Start();
            }
               
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsStart = true;
            });
            
        });
        public DelegateCommand StopAllCommand => new DelegateCommand(() =>
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _ChartDataTimer.Stop();
                _historyRecorderL2Db.Stop();
            }
               
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsStart = false;
            });
        });

        private DelegateCommand _closeCommand;
        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        public DelegateCommand<object> CheckedCommand => new DelegateCommand<object>((e) =>
        {
            ChangeChartVisible(WpfPlotControl,true,(e as TelemetrySliceField).Name);
        });
        public DelegateCommand<object> UnCheckedCommand => new DelegateCommand<object>((e) =>
        {
            ChangeChartVisible(WpfPlotControl, false, (e as TelemetrySliceField).Name);
        });

        public DelegateCommand HistoryDownloadCommand => new DelegateCommand(() =>
        {
            try
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "请选择保存路径";
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = fbd.SelectedPath;
                    string currentSessionID = null;
                    if (_projectManager != null)
                    {
                        currentSessionID = _projectManager.CurrentProject.HistoryRecorderL2DbSessionId;
                    }

                    HistoryExcelExporter_BySeq.ExportSessionToExcel_MergedByTimeAndSeq(null,currentSessionID, path);
                    //HistoryToExcel(path);
                }
            }
            catch (Exception ex)
            {
            }
        });
        #endregion

        #region Method
        public void EventListener()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Subscribe(() =>
            {
                _timer.Stop();
                _ChartDataTimer.Stop();
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    IsStart = false;
                });
            });
            _eventAggregator.GetEvent<OnConnctedEvent>().Subscribe(() =>
            {
                ShowTelemetryList.Clear();
                if (_projectManager.CurrentProject.CommService is PcmuUartService puService)
                {
                    ShowTelemetryList.AddRange(puService._tlmSlices);
                }

            });
           
        }
        private void SettingChartLimit(object o)
        {
            WpfPlotControl.SetXYLimit(MaxX: Chart1MaxX, MinX: Chart1MinX, MaxY: Chart1MaxY, MinY: Chart1MinY);
        }
        public void ChangeChartVisible(WpfPlotSteamBase chart, bool isShow, string paramName)
        {
            var legLabel = chart.Plot.GetPlottables().OfType<Scatter>().FirstOrDefault(s => s.LegendText == paramName);
            if (legLabel != null)
            {
                legLabel.IsVisible = isShow;
            }
            else
            {
                chart.AddSignalData(paramName);
            }
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() => chart.RefreshData());
            //chart.RefreshData();
        }

        public override async void LoadData()
        {
            ShowTelemetryList.Clear();
            if (_projectManager.CurrentProject.CommService is PcmuUartService puService)
            {
                ShowTelemetryList.AddRange(puService._tlmSlices);
            }
            else
            {
                List<TelemetryMonit> ltm = new List<TelemetryMonit>();
                List<TelemetrySliceField> ltsf = new List<TelemetrySliceField>();
                string firstCode = "";

                using (var db = new DbContext())
                {
                    var monitCode =await db.TelemetryMonits.Where(t => t.ChipId == _projectManager.CurrentProject.Chip.ChipId).ToListAsync();
                    ltm.AddRange(monitCode);
                }

                // 配置位切片解析（示例：请按你的真实遥测表配置）
                foreach (var monit in ltm)
                {
                    TelemetrySliceField tsf = new TelemetrySliceField();
                    tsf.Name = monit.Name;
                    tsf.StartByte = monit.StartByte;
                    tsf.ByteCount = monit.ByteLen;
                    tsf.BitStart = monit.StartBit;
                    tsf.BitLength = monit.BitLen;
                    tsf.Order = ByteOrder.BE;
                    switch (monit.ByteLen)
                    {
                        case 1:
                            tsf.As = TargetType.U8;
                            break;
                        case 2:
                            tsf.As = TargetType.U16;
                            break;
                        case 4:
                            tsf.As = TargetType.U32;
                            break;
                    }
                    tsf.Unit = monit.Unit;
                    tsf.ShowStr = monit.FormulaShow;
                    tsf.ParamA = monit.ParamA;
                    tsf.ParamB = monit.ParamB;
                    tsf.ParamC = monit.ParamC;
                    tsf.ParamSign = monit.ParamSign;

                    ltsf.Add(tsf);
                }

                ShowTelemetryList.AddRange(ltsf);
            }

        }
        #endregion

    }
}
