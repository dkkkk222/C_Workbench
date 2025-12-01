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
using Workbench.Models.dw;
using Prism.Services.Dialogs;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using HandyControl.Controls;
using Workbench.Views.dw;
using Workbench.Views.Windows;
using Workbench.Views.Telemetry;
using NPOI.SS.Formula.Functions;

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
        public System.Timers.Timer _refTime = new System.Timers.Timer();
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private string session_id { get; set; }
        private HistoryRecorderL2db _historyRecorderL2Db { get; set; }
        public TelemetryMonitViewModel(IEventAggregator eventAggregator, ProjectManager projectManager, IDialogService dialogService)
        {
            _projectManager = projectManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _timer.Interval = 1000; // 设置触发间隔
            _timer.Elapsed += _timer_Elapsed; ; // 设置触发事件

            _ChartDataTimer.Interval = 200;
            _ChartDataTimer.Elapsed += ChartDataTimer_Tick;

            _refTime.Interval = 500; // 设置触发间隔
            _refTime.Elapsed += RefTime_Tick; // 设置触发事件
            session_id = _projectManager.CurrentProject.ActiveSessionId;
            WatchTelemetryChartGroups = _projectManager.CurrentProject.WatchTelemetryChartGroups;
            
            _projectManager.CurrentProject.EnsureSession();
            _historyRecorderL2Db = _projectManager.CurrentProject.HistoryRecorderL2Db;
            NormalizeWatchCharts();
            EventListener();
            SelectedCycle = CycleSource[0];
            InitData();
            InitListen();
            TableColumns = InitTableColumns();
        
            foreach (var group in WatchTelemetryChartGroups)
            {
                group.Inject(_dialogService);
            }

            #region Chart处理
            _watchChartGroupsForTab = new ListCollectionView(WatchTelemetryChartGroups);
            _watchChartGroupsForTab.Filter = o => o is WatchChartModel m && !IsPlaceholder(m);
            if (WatchTelemetryChartGroups != null && _chartGroupsChangedHandler != null)
                WatchTelemetryChartGroups.CollectionChanged -= _chartGroupsChangedHandler;
            _chartGroupsChangedHandler = (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    _watchChartGroupsForTab?.Refresh();
                    SyncCurrentChartTab();
                    HasRealCharts = _watchChartGroupsForTab?.Count > 0;
                });
            };
            WatchTelemetryChartGroups.CollectionChanged += _chartGroupsChangedHandler;

            _watchChartGroupsForTab.Refresh();
            SyncCurrentChartTab();
            HasRealCharts = _watchChartGroupsForTab.Count > 0;
            #endregion
            InitOrderAndSort();
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
                SplitterPositionLeft1 = _projectManager.CurrentProject.TelemetryMonitViewGrid.SplitterPositionLeft;
                SplitterPositionRight = _projectManager.CurrentProject.TelemetryMonitViewGrid.SplitterPositionRight;             
                SplitterPositionRight2 = _projectManager.CurrentProject.TelemetryMonitViewGrid.SplitterPositionRight2;             

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
                e.TelemetryMonitViewGrid.SplitterPositionLeft = SplitterPositionLeft1;
                e.TelemetryMonitViewGrid.SplitterPositionRight = SplitterPositionRight;
                e.TelemetryMonitViewGrid.SplitterPositionRight2 = SplitterPositionRight2; 
            });
            _eventAggregator.GetEvent<UpdateTelemetryMonitCodeEvent>().Subscribe(async () =>
            {
                LoadData();
            });
        }

        private ObservableCollection<TableColumn> _tableColumns = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableColumn> TableColumns
        {
            get { return _tableColumns; }
            set { SetProperty(ref _tableColumns, value); }
        }

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }
         

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1.3, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, Normalize(value));
        }
        public System.Windows.GridLength splitterPositionRight2 = new System.Windows.GridLength(0.3, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight2
        {
            get => splitterPositionRight2;
            set => SetProperty(ref splitterPositionRight2, Normalize(value));
        }
        
        public System.Windows.GridLength splitterPositionLeft1 = new System.Windows.GridLength(0.3, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft1
        {
            get => splitterPositionLeft1;
            set => SetProperty(ref splitterPositionLeft1, Normalize(value));
        }

        private static GridLength Normalize(GridLength value)
        {
            if (value.IsStar) return value;
            const double min = 1;
            if (value.Value < min) return new GridLength(min);
            return value;
        }
        #region Chart
        private NotifyCollectionChangedEventHandler _chartGroupsChangedHandler;
        private static bool IsPlaceholder(WatchChartModel m)
 => m != null && string.Equals(m.Header, "未选中", StringComparison.Ordinal);
        // TabControl 专用视图（独立于默认视图，不会影响 ComboBox）
        private readonly ListCollectionView _watchChartGroupsForTab;
        public ICollectionView WatchChartGroupsForTab => _watchChartGroupsForTab;

        private ObservableCollection<WatchChartModel> _watchTelemetryChartGroups = new ObservableCollection<WatchChartModel>();
        /// <summary>
        /// 数据监测图列表
        /// </summary>
        public ObservableCollection<WatchChartModel> WatchTelemetryChartGroups
        {
            get => _watchTelemetryChartGroups;
            set => SetProperty(ref _watchTelemetryChartGroups, value);
        }

        private WatchChartModel _currentChartTab = null;
        public WatchChartModel CurrentChartTab
        {
            get => _currentChartTab;
            set => SetProperty(ref _currentChartTab, value);
        }

        private bool _hasRealCharts;
        public bool HasRealCharts
        {
            get => _hasRealCharts;
            set => SetProperty(ref _hasRealCharts, value);
        }
        #endregion
        private int _busy2;
        private void ChartDataTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref _busy2, 1) == 1) return;
            try
            {
                //System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                //    var listLable=WpfPlotControl.Plot.GetPlottables().OfType<Scatter>().ToList();
                //    foreach(var lab in listLable)
                //    {
                //        var showLine=ShowTelemetryList.Where(x => x.Name == lab.LegendText).FirstOrDefault();
                //        if(showLine!=null)
                //        { 
                //            WpfPlotControl.UpdateData(lab.LegendText, showLine.SourceData);
                //        }
                //    }
                //    WpfPlotControl.RefreshData(false);
                //});
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    foreach (var dataItem in ShowTelemetryList)
                    {
                        var chart = WatchTelemetryChartGroups.FirstOrDefault(x => x.Id == dataItem.TableId);
                        if (chart != null)
                        {
                            chart.WpfPlotControl2.UpdateData(dataItem.Name, dataItem.SourceData);
                            chart.WpfPlotControl.UpdateData(dataItem.Name, dataItem.SourceData);
                        }
                    }
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
        private int _busy4;
        private void RefTime_Tick(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _busy4, 1) == 1) return;
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        foreach (var chartTab in WatchTelemetryChartGroups.ToArray())
                        {
                            chartTab.WpfPlotControl2.RefreshData(false);
                            chartTab.WpfPlotControl.RefreshData(false);
                        }
                    });
                }
            }
            catch (Exception ex)
            { _log.Error(ex); }
            finally
            {
                Interlocked.Exchange(ref _busy4, 0);
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

        private string _selectedChartValue = null;
        /// <summary>
        /// 数据监测图列表
        /// </summary>
        public string SelectedChartValue
        {
            get => _selectedChartValue;
            set => SetProperty(ref _selectedChartValue, value);
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
        public DelegateCommand<SelectionChangedEventArgs> ChartTableChangeCommand => new DelegateCommand<SelectionChangedEventArgs>((e) =>
        {
            try
            {
                if (e == null) return;

                var selectedItem = e.AddedItems?.OfType<WatchChartModel>().FirstOrDefault();
                if (selectedItem == null) return;
                var cb = e.Source as System.Windows.Controls.ComboBox ?? e.OriginalSource as System.Windows.Controls.ComboBox;
                if (cb == null) return;

                var row = cb.DataContext as TelemetrySliceField;
                if (row == null) return;

                var selectChart = WatchTelemetryChartGroups.FirstOrDefault(x => x.Id == row.TableId);
                var selectTable = WatchTelemetryChartGroups.FirstOrDefault(x => x.Id == selectedItem.Id);

                if (selectedItem.Header == "未选中" && selectedItem.Id != row.TableId)
                {
                    if (selectChart != null)
                    {
                        ChangeChartVisible(selectChart.WpfPlotControl2, false, row.Name);
                        ChangeChartVisible(selectChart.WpfPlotControl, false, row.Name);
                    }
                    //row.SelectedChartValue = null;
                    row.TableId = null;
                    return;
                }

                if (selectedItem.Id != row.TableId)
                {
                    if (selectChart != null)
                    {
                        ChangeChartVisible(selectChart.WpfPlotControl2, false, row.Name);
                        ChangeChartVisible(selectChart.WpfPlotControl, false, row.Name);
                    }
                    if (selectTable != null)
                    {
                        ChangeChartVisble2(selectTable.WpfPlotControl2, row.Name);
                        ChangeChartVisble2(selectTable.WpfPlotControl, row.Name);
                        row.TableId = selectedItem.Id;
                        // row.SelectedChartValue = selectedItem.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

        });
        public DelegateCommand<object> CloseChartCommand => new DelegateCommand<object>((e) =>
        {
            if (e is WatchChartModel chartToClose)
            {
                RemoveTabChart(chartToClose);
                // 从 WatchGroups 集合中移除选中的选项卡
                WatchTelemetryChartGroups.Remove(chartToClose);
                // （可选）如果需要，更新 CurrentTab 指向另一个有效选项卡
                if (CurrentChartTab == chartToClose)
                {
                    CurrentChartTab = WatchTelemetryChartGroups.FirstOrDefault();
                }
            }
        });
        public DelegateCommand<object> CloseOthersChartCommand => new DelegateCommand<object>((e) =>
        {
            if (e is WatchChartModel chartToClose)
            {
                foreach (var group in WatchTelemetryChartGroups.ToArray())
                {
                    if (group.Id == chartToClose.Id || group.Id == "placeholder")
                        continue;
                    RemoveTabChart(group);
                    WatchTelemetryChartGroups.Remove(group);
                }
            }
        });
        public DelegateCommand<object> CloseAllChartCommand => new DelegateCommand<object>((e) =>
        {
            foreach (var group in WatchTelemetryChartGroups.ToArray())
            {
                if (group.Id == "placeholder")
                    continue;
                RemoveTabChart(group);
                WatchTelemetryChartGroups.Remove(group);
            }
        });
        public void RemoveChartWhereClose(WatchGroup thisGroup)
        {
            try
            {
                foreach (var field in thisGroup.BitFields)
                {
                    var selectChart = WatchTelemetryChartGroups.FirstOrDefault(x => x.Id == field.TableId);
                    if (selectChart != null)
                    {
                        ChangeChartVisible(selectChart.WpfPlotControl2, false, field.Desc);
                        ChangeChartVisible(selectChart.WpfPlotControl, false, field.Desc);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }
        private DelegateCommand<object> _settingChartLimitCommand;
        [JsonIgnore]
        public DelegateCommand<object> SettingChartLimitCommand =>
            _settingChartLimitCommand ?? (_settingChartLimitCommand = new DelegateCommand<object>(SettingChartLimit));
        public DelegateCommand StartAllCommand => new DelegateCommand(() =>
        {
            if (!(_projectManager.CurrentProject.CommService is PcmuUartService))
            {
                HandyControl.Controls.MessageBox.Show("系统连接才能使用遥控监控，请重试!");
                return;
            }
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
                _refTime.Start();
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
                _refTime.Stop();
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

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                //param.IsCheck = !param.IsCheck;
                if(param.Title== Constants.AllCheck)
                { 
                    if(param.IsCheck)
                    {
                        foreach(var treeItem in SingleParamTrees)
                        {
                            treeItem.IsCheck = true;
                        }
                    }
                    else
                    {
                        foreach (var treeItem in SingleParamTrees)
                        {
                            treeItem.IsCheck = false;
                        }
                    }
                }
                else
                {
                    if(!param.IsCheck)
                    {
                        var allCheckItem = SingleParamTrees.Where(x => x.Title == Constants.AllCheck).FirstOrDefault();
                        if (allCheckItem.IsCheck)
                        {
                            allCheckItem.IsCheck = false;
                        }
                    }
                }
                ChangeList();
            }));

        public DelegateCommand AddWatchGroupChartCommand => new DelegateCommand(() =>
        {
            var baseName = $"表{WatchTelemetryChartGroups.Where(x => x.Id != "placeholder").Count() + 1}";
            //var header = MakeUniqueHeaderChart(baseName);
            var header = GetMaxNumForNameChart();
            var maxOrder = WatchTelemetryChartGroups.Any(c => c.Id != "placeholder")
       ? WatchTelemetryChartGroups.Where(c => c.Id != "placeholder").Max(c => c.Order)
       : 0;

            WatchChartModel wpfPlotControl = new WatchChartModel("监测图", _dialogService, session_id)
            {
                Id = Guid.NewGuid().ToString("N"),
                Header = header,
                Order = maxOrder + 1,
            };
            WatchTelemetryChartGroups.Add(wpfPlotControl);
            if (CurrentChartTab == null || IsPlaceholder(CurrentChartTab))
            {
                CurrentChartTab = wpfPlotControl;
            }
            _watchChartGroupsForTab.Refresh();
            HasRealCharts = _watchChartGroupsForTab.Count > 0;
        });

        public DelegateCommand ShowChartGroupCommand => new DelegateCommand(() =>
        {
            IDialogParameters dialogParameters = new DialogParameters();
            dialogParameters.Add("viewModel", this);
            _dialogService.Show(nameof(TelemetryChartListView), dialogParameters, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                }
            }, nameof(ShowChartListWindows));
        });

        #endregion

        #region Method
        public void ChangeChartVisble2(WpfPlotSteamBase chart, string paramName)
        {
            var existing = chart.Plot.GetPlottables().OfType<Scatter>().FirstOrDefault(s => s.LegendText == paramName);
            if (existing != null)
            {
                existing.IsVisible = true;
            }
            else
            {
                chart.AddSignalData(paramName);
            }
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() => chart.RefreshData());
            //chart.RefreshData();
        }
        private string GetMaxNumForNameChart()
        {
            var tables = new HashSet<string>(WatchTelemetryChartGroups.Where(x => x.Id != "placeholder").Select(x => x.Header));
            string tableName = "";
            if (tables.Count == 0)
            {
                var countTable = WatchTelemetryChartGroups.Where(x => x.Id != "placeholder").Count() + 1;
                tableName = $"图{countTable}";
            }
            else
            {
                // 用正则提取数字部分，然后转成 int
                int maxNumber = tables
                    .Select(s => int.Parse(Regex.Match(s, @"\d+").Value))
                    .Max();
                tableName = $"图{maxNumber + 1}";
            }

            return tableName;
        }
        public void RemoveTabChart(WatchChartModel thisChartTab)
        {
            var chartParams = thisChartTab.WpfPlotControl.Plot.GetPlottables().OfType<Scatter>();
            var chartParams2 = thisChartTab.WpfPlotControl2.Plot.GetPlottables().OfType<Scatter>();
            
                foreach (var field in ShowTelemetryList)
                {
                    var isHave = chartParams.Where(x => x.LegendText == field.Name).FirstOrDefault();
                    var isHave2 = chartParams2.Where(x => x.LegendText == field.Name).FirstOrDefault();
                    if (isHave != null)
                    {
                        field.TableId = null;
                        field.SelectedChartValue = null;
                    }
                }
           
        }
        private static WatchChartModel CreatePlaceholder() => new WatchChartModel("监测图")
        {
            Id = "placeholder",
            Header = "未选中"
        };
        private void NormalizeWatchCharts()
        {
            if (WatchTelemetryChartGroups == null) return;

            // 移除多余占位
            var dups = WatchTelemetryChartGroups.Where(IsPlaceholder).Skip(1).ToList();
            foreach (var d in dups) WatchTelemetryChartGroups.Remove(d);

            // 没有就补一个
            if (!WatchTelemetryChartGroups.Any(IsPlaceholder))
                WatchTelemetryChartGroups.Insert(0, CreatePlaceholder());
        }
        private void SyncCurrentChartTab()
        {
            if (_watchChartGroupsForTab.Count == 0)
            {
                if (CurrentChartTab != null) CurrentChartTab = null;
                return;
            }
            // 当前未选/占位/或不在视图中：选第一个真实项
            if (CurrentChartTab == null
                || IsPlaceholder(CurrentChartTab)
                || !_watchChartGroupsForTab.Contains(CurrentChartTab))
            {
                CurrentChartTab = (WatchChartModel)_watchChartGroupsForTab.GetItemAt(0);
            }
        }
        private ObservableCollection<TableColumn> InitTableColumns()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] { "序号", "名称", "数据位置(bytes)", "解析内容(bit)", "解析要求", "解析结果", "解析内容(HEX)","单位","添加到监测图" };
            for (int i = 0; i < arr.Length; i++)
            {
                var tab = new TableColumn()
                {
                    Name = arr[i],
                }; 
                target.Add(tab);
            }
            return target;
        }
        public async void ChangeList()
        {
            List<string> isOldCheck = new List<string>();
            foreach(var selectItem in ShowTelemetryList)
            {
                if(selectItem.IsChecked)
                {
                    isOldCheck.Add(selectItem.Name);
                    ChangeChartVisible(WpfPlotControl, false, selectItem.Name);
                }
            }
            
            ShowTelemetryList.Clear();

           
            if (_projectManager.CurrentProject.CommService is PcmuUartService puService)
            {
                foreach (var item in puService._tlmSlices)
                {
                    item.IsChecked = false;
                }
                if (SingleParamTrees.Where(x=>x.Title== Constants.AllCheck).FirstOrDefault().IsCheck)
                {
                    ShowTelemetryList.AddRange(puService._tlmSlices);
                }
                else
                {
                    foreach (var selectCag in SingleParamTrees)
                    {
                        if(selectCag.IsCheck)
                            ShowTelemetryList.AddRange(puService._tlmSlices.Where(x => x.Category == selectCag.Title));
                    }
                }                    
            }
            else
            {
                List<TelemetryMonit> ltm = new List<TelemetryMonit>();
                List<TelemetrySliceField> ltsf = new List<TelemetrySliceField>();

                using (var db = new DbContext())
                {
                    var monitCode = await db.TelemetryMonits.Where(t => t.ChipId == _projectManager.CurrentProject.Chip.ChipId).ToListAsync();
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
                    tsf.Category = monit.Category;
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

                if (SingleParamTrees.Where(x => x.Title == Constants.AllCheck).FirstOrDefault().IsCheck)
                {
                    ShowTelemetryList.AddRange(ltsf);
                }
                else
                {
                    foreach (var selectCag in SingleParamTrees)
                    {
                        if (selectCag.IsCheck)
                            ShowTelemetryList.AddRange(ltsf.Where(x => x.Category == selectCag.Title));
                    }
                }
            }
            foreach (var selectItem in ShowTelemetryList)
            {
                var isHave=isOldCheck.Where(x => x == selectItem.Name).FirstOrDefault();

                if (!string.IsNullOrEmpty(isHave))
                {
                    selectItem.IsChecked = true;
                    ChangeChartVisible(WpfPlotControl, true, selectItem.Name);
                }
            }
        }
        public void EventListener()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Subscribe(() =>
            {
                _timer.Stop();
                _ChartDataTimer.Stop();
                _refTime.Stop();
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    IsStart = false;
                });
            });
            _eventAggregator.GetEvent<OnConnctedEvent>().Subscribe(() =>
            {
                LoadData();
            });
            _eventAggregator.GetEvent<SaveProjectEvent>().Subscribe(e =>
            {
                e.TlmSlices = ShowTelemetryList;
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
        bool isFirst = true;
        public void IsFirstLoad()
        {
            foreach(var item in _projectManager.CurrentProject.TlmSlices)
            {
                var haveItem = ShowTelemetryList.Where(x => x.Name == item.Name).FirstOrDefault();
                if (haveItem!=null&&item.SelectedChartValue != null)
                {
                    haveItem.TableId = item.TableId;
                    haveItem.SelectedChartValue = item.SelectedChartValue;

                    var chart = WatchTelemetryChartGroups.FirstOrDefault(x => x.Id == haveItem.TableId);
                    if(chart!=null)
                    {
                        ChangeChartVisble2(chart.WpfPlotControl2, haveItem.Name);
                        ChangeChartVisble2(chart.WpfPlotControl, haveItem.Name);
                    }
                }
            }
        }
        public override async void LoadData()
        {
            List< TelemetrySliceField > selectItem= new List< TelemetrySliceField >();
            foreach(var oldItem in ShowTelemetryList.ToArray())
            {
                selectItem.Add(oldItem);
            }
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
            SingleParamTrees.Clear();
            var tree = await _projectManager.GetChipCategoryTreeForTeleMonite();
            SingleParamTrees.AddRange(tree);
            foreach(var checkItem in SingleParamTrees)
            {
                checkItem.IsCheck = true;
            }
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var newItem in ShowTelemetryList)
                {
                    var haveItem = selectItem.Where(x => x.Name == newItem.Name).FirstOrDefault();
                    if (haveItem != null)
                    {
                        if (haveItem.TableId != null)
                        {
                            newItem.TableId = haveItem.TableId;
                            newItem.SelectedChartValue = haveItem.SelectedChartValue;
                        }
                    }
                }
            });
            if(isFirst)
            {
                isFirst = false;
                IsFirstLoad();
            }
        }
        private void InitOrderAndSort()
        { 

            // WatchChartGroups
            if (WatchTelemetryChartGroups != null)
            {
                // 占位项（未选中）固定最前
                foreach (var c in WatchTelemetryChartGroups)
                    if (c.Id == "placeholder") c.Order = int.MinValue;

                for (int i = 0; i < WatchTelemetryChartGroups.Count; i++)
                    if (WatchTelemetryChartGroups[i].Order == 0 && WatchTelemetryChartGroups[i].Id != "placeholder")
                        WatchTelemetryChartGroups[i].Order = i; // 初始化

                // 默认视图排序（ItemsControl 会用到默认视图）
                var viewCharts = System.Windows.Data.CollectionViewSource.GetDefaultView(WatchTelemetryChartGroups);
                viewCharts.SortDescriptions.Clear();
                viewCharts.SortDescriptions.Add(new SortDescription(nameof(WatchChartModel.Order), ListSortDirection.Ascending));

                // 你的 Tab 专用视图也加上排序（原来只有 Filter）
                _watchChartGroupsForTab.SortDescriptions.Clear();
                _watchChartGroupsForTab.SortDescriptions.Add(new SortDescription(nameof(WatchChartModel.Order), ListSortDirection.Ascending));
            }
        }
        #endregion

    }
}
