using Force.DeepCloner;
using HandyControl.Controls;
using log4net;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using NPOI.XSSF.Streaming.Values;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Db;
using Workbench.Db.Tables;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;
using Workbench.Views;
using Workbench.Views.dw;
using Workbench.Views.Windows;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace Workbench.ViewModels.dw
{
    public sealed class RecordingSession : IDisposable
    {
        public string SessionId { get; }
        public IngestPipeline Pipeline { get; }

        public RecordingSession(string sessionId = null)
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString("N");
            Pipeline = new IngestPipeline(SessionId);
        }

        public void Dispose() => Pipeline?.Dispose();
    }
    public class WatchViewModel : AvaDocument
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WatchViewModel));
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        public int RefreshInterval = 50;//UI更新间隔
        public System.Timers.Timer _timer = new System.Timers.Timer();
        public System.Timers.Timer _ReceiveTimer = new System.Timers.Timer();
        public System.Timers.Timer _recordTime = new System.Timers.Timer();
        public System.Timers.Timer _refTime = new System.Timers.Timer();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public IngestPipeline pipeLineIng { get; set; }
        private string session_id { get; set; }
        public WatchViewModel(IEventAggregator eventAggregator, ProjectManager projectManager, IDialogService dialogService)
        {
            _projectManager = projectManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            WatchGroups = _projectManager.CurrentProject.WatchGroups;
            WatchChartGroups = _projectManager.CurrentProject.WatchChartGroups;
            CategoryRegisters = _projectManager.CurrentProject.CategoryRegisters;
            _projectManager.CurrentProject.EnsureSession();
            int registerDelay = 10;
            registerDelay=int.Parse(projectManager.CurrentProject.RegisterDelay);
            pms = new ParameterMonitorService(registerDelay) { CurrentProject = _projectManager.CurrentProject };
            pms.Enable();
            NormalizeWatchCharts();
            NormalizeWatchTable();
            _timer.Interval = 50; // 设置触发间隔
            _timer.Elapsed += Timer_Tick; // 设置触发事件

            _ReceiveTimer.Interval = 100;
            _ReceiveTimer.Elapsed += ReceiveTimer_Tick;

            _recordTime.Interval = 50; // 设置触发间隔
            _recordTime.Elapsed += RecordTime_Tick; // 设置触发事件

            _refTime.Interval = 500; // 设置触发间隔
            _refTime.Elapsed += RefTime_Tick; // 设置触发事件

            EventListener();

            foreach (var group in WatchGroups)
            {
                group.Inject(dialogService);
            }
            //session_id = Guid.NewGuid().ToString();
            //pipeLineIng = new IngestPipeline(session_id);
            session_id = _projectManager.CurrentProject.ActiveSessionId;
            pipeLineIng = _projectManager.CurrentProject.ActiveSession.Pipeline;

            _watchChartGroupsForTab = new ListCollectionView(WatchChartGroups);
            _watchChartGroupsForTab.Filter = o => o is WatchChartModel m && !IsPlaceholder(m);
            if (WatchChartGroups != null && _chartGroupsChangedHandler != null)
                WatchChartGroups.CollectionChanged -= _chartGroupsChangedHandler;
            _chartGroupsChangedHandler = (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    _watchChartGroupsForTab?.Refresh();
                    SyncCurrentChartTab();
                    HasRealCharts = _watchChartGroupsForTab?.Count > 0;
                });
            };

            WatchChartGroups.CollectionChanged += _chartGroupsChangedHandler;

            _watchChartGroupsForTab.Refresh();
            SyncCurrentChartTab();
            HasRealCharts = _watchChartGroupsForTab.Count > 0;

            #region Table的未选中
            _watchGroupsForTab = new ListCollectionView(WatchGroups);
            _watchGroupsForTab.Filter = o => o is WatchGroup m && !IsPlaceholderTable(m);
            if (WatchGroups != null && _tableGroupsChangedHandler != null)
                WatchGroups.CollectionChanged -= _tableGroupsChangedHandler;
            _tableGroupsChangedHandler = (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    _watchGroupsForTab?.Refresh();
                    SyncCurrentTableTab();
                    HasRealTables = _watchGroupsForTab?.Count > 0;
                });
            };

            WatchGroups.CollectionChanged += _tableGroupsChangedHandler;

            _watchGroupsForTab.Refresh();
            SyncCurrentTableTab();
            HasRealTables = _watchGroupsForTab.Count > 0;
            #endregion
            ReLoadChartTable();
            InitOrderAndSort();
        }
        private static WatchChartModel CreatePlaceholder() => new WatchChartModel("监测图")
        {
            Id = "placeholder",
            Header = "未选中"
        };
        private WatchGroup CreatePlaceholderTable()
        {
             var temp= new WatchGroup(_dialogService,session_id,_projectManager)
                {
                    Id = "placeholder",
                    Header = "未选中"
                };
            return temp;
        }
        private void NormalizeWatchCharts()
        {
            if (WatchChartGroups == null) return;

            // 移除多余占位
            var dups = WatchChartGroups.Where(IsPlaceholder).Skip(1).ToList();
            foreach (var d in dups) WatchChartGroups.Remove(d);

            // 没有就补一个
            if (!WatchChartGroups.Any(IsPlaceholder))
                WatchChartGroups.Insert(0, CreatePlaceholder());
        }
        private void NormalizeWatchTable()
        {
            if (WatchGroups == null) return;

            // 移除多余占位
            var dups = WatchGroups.Where(IsPlaceholderTable).Skip(1).ToList();
            foreach (var d in dups) WatchGroups.Remove(d);

            // 没有就补一个
            if (!WatchGroups.Any(IsPlaceholderTable))
                WatchGroups.Insert(0, CreatePlaceholderTable());
        }

        private NotifyCollectionChangedEventHandler _chartGroupsChangedHandler;
        private NotifyCollectionChangedEventHandler _tableGroupsChangedHandler;
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
        private void SyncCurrentTableTab()
        {
            if (_watchGroupsForTab.Count == 0)
            {
                if (CurrentTab != null) CurrentTab = null;
                return;
            }

            // 当前未选/占位/或不在视图中：选第一个真实项
            if (CurrentTab == null
                || IsPlaceholderTable(CurrentTab)
                || !_watchGroupsForTab.Contains(CurrentTab))
            {
                CurrentTab = (WatchGroup)_watchGroupsForTab.GetItemAt(0);
            }
        }
        private static bool IsPlaceholder(WatchChartModel m)
    => m != null && string.Equals(m.Header, "未选中", StringComparison.Ordinal);
        // TabControl 专用视图（独立于默认视图，不会影响 ComboBox）
        private readonly ListCollectionView _watchChartGroupsForTab;
        public ICollectionView WatchChartGroupsForTab => _watchChartGroupsForTab;


        private static bool IsPlaceholderTable(WatchGroup m)
   => m != null && string.Equals(m.Header, "未选中", StringComparison.Ordinal);
        // TabControl 专用视图（独立于默认视图，不会影响 ComboBox）
        private readonly ListCollectionView _watchGroupsForTab;
        public ICollectionView WatchGroupsForTab => _watchGroupsForTab;


        private bool _hasRealCharts;
        public bool HasRealCharts
        {
            get => _hasRealCharts;
            set => SetProperty(ref _hasRealCharts, value);
        }

        private bool _hasRealTables;
        public bool HasRealTables
        {
            get => _hasRealTables;
            set => SetProperty(ref _hasRealTables, value);
        }
        #region Property
        public bool _isActive = false;
        public override bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    if (value)
                    {
                        _timer.Start();
                        _ReceiveTimer.Start();
                        _recordTime.Start();
                        _refTime.Start();
                        pms.Enable();
                        StartUiLoop(RefreshInterval);
                    }
                    else
                    {
                        _timer.Stop();
                        _ReceiveTimer.Stop();
                        _recordTime.Stop();
                        _refTime.Stop();
                        pms.Disable();
                        StopUiLoopAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private string _addressKeyword;
        public string AddressKeyword
        {
            get => _addressKeyword;
            set => SetProperty(ref _addressKeyword, value);
        }

        private ObservableCollection<WatchGroup> _watchGroups = new ObservableCollection<WatchGroup>();
        public ObservableCollection<WatchGroup> WatchGroups
        {
            get => _watchGroups;
            set => SetProperty(ref _watchGroups, value);
        }

        //public ObservableCollection<WatchChartModel> _watchChartGroups = new ObservableCollection<WatchChartModel>() {
        //       new WatchChartModel("监测图") {
        //        Id = Guid.NewGuid().ToString("N"),
        //        Header = $"未选中",
        //    }};
        public ObservableCollection<WatchChartModel> _watchChartGroups = new ObservableCollection<WatchChartModel>();
        /// <summary>
        /// 数据监测图列表
        /// </summary>
        public ObservableCollection<WatchChartModel> WatchChartGroups
        {
            get => _watchChartGroups;
            set => SetProperty(ref _watchChartGroups, value);
        }

        private WatchGroup _currentTab;
        public WatchGroup CurrentTab
        {
            get => _currentTab;
            set => SetProperty(ref _currentTab, value);
        }
        private WatchGroup _SelectTab;
        public WatchGroup SelectTab
        {
            get => _SelectTab;
            set => SetProperty(ref _SelectTab, value);
        }
        private WatchChartModel _currentChartTab = null;
        public WatchChartModel CurrentChartTab
        {
            get => _currentChartTab;
            set => SetProperty(ref _currentChartTab, value);
        }

        private ObservableCollection<ValueLabelOption> _settingCategoryList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> SettingCategoryList
        {
            get => _settingCategoryList;
            set => SetProperty(ref _settingCategoryList, value);
        }

        private ValueLabelOption _currentSettingCategory;
        public ValueLabelOption CurrentSettingCategory
        {
            get => _currentSettingCategory;
            set
            {
                if (SetProperty(ref _currentSettingCategory, value))
                {
                    CategoryAddressList.Clear();
                    var CategoryAddressListOptions = _projectManager.GetRegisterForCategories(value.Value.ToString()).Select(t => new ValueLabelOption() { Value = t.AddressDec, Label = t.ShowAddressStr });
                    CategoryAddressList.AddRange(CategoryAddressListOptions);
                    CategoryAddress = CategoryAddressList.FirstOrDefault();
                }
            }
        }

        private ObservableCollection<ValueLabelOption> _categoryAddressList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> CategoryAddressList
        {
            get => _categoryAddressList;
            set => SetProperty(ref _categoryAddressList, value);
        }

        private ValueLabelOption _categoryAddress;
        public ValueLabelOption CategoryAddress
        {
            get => _categoryAddress;
            set => SetProperty(ref _categoryAddress, value);
        }


        private ObservableCollection<RegisterAddrInfo> _categoryRegisters = new ObservableCollection<RegisterAddrInfo>();
        public ObservableCollection<RegisterAddrInfo> CategoryRegisters
        {
            get => _categoryRegisters;
            set => SetProperty(ref _categoryRegisters, value);
        }

        private RegisterAddrInfo _currentRegister;
        public RegisterAddrInfo CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
        }

        private ParameterMonitorService _pms;
        public ParameterMonitorService pms
        {
            get => _pms;
            set => SetProperty(ref _pms, value);
        }
        private string _AllTime="1";
        public string AllTime
        {
            get => _AllTime;
            set => SetProperty(ref _AllTime, value);
        }
        #endregion

        #region Tree
        private ValueLabelOption _currentCategory;
        public ValueLabelOption CurrentCategory
        {
            get => _currentCategory;
            set
            {
                if (SetProperty(ref _currentCategory, value))
                {
                    CategoryAddressList.Clear();
                    var CategoryAddressListOptions = _projectManager.GetRegisterForCategories(value.Value.ToString()).Select(t => new ValueLabelOption() { Value = t.AddressDec, Label = t.ShowAddressStr });
                    CategoryAddressList.AddRange(CategoryAddressListOptions);
                    CategoryAddress = CategoryAddressList.FirstOrDefault();

                    UtilsFunc.SerachCategoryNode(SingleParamTrees, value);
                }
            }
        }
        private bool _isLeftOpen = true;
        public bool IsLeftOpen
        {
            get => _isLeftOpen;
            set
            {
                if (_isLeftOpen != value)
                {
                    SetProperty(ref _isLeftOpen, value);
                }
            }
        }
        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                SetProperty(ref _treeKeyword, value);
                if (IsOrderByCategory)
                {
                    SearchCategoryTree(value, IsOrderByAddress);
                }
                else if (IsOrderByName)
                {
                    OrderByType(value, OrderByTypeEnum.Name);
                }
                else if (IsOrderByAddress)
                {
                    OrderByType(value, OrderByTypeEnum.Address);
                }

                var currentTreeNode = SingleParamTrees.GetMaxDepthLeaves().ToList();
                UtilsFunc.SyncTreeCheckNode(currentTreeNode, CategoryRegisters);
            }
        }
        private bool _isOrderByCategory = true;
        public bool IsOrderByCategory
        {
            get => _isOrderByCategory;
            set
            {
                if (value)
                {
                    SearchCategoryTree(TreeKeyword, IsOrderByAddress);
                    var currentTreeNode = SingleParamTrees.GetMaxDepthLeaves().ToList();

                    UtilsFunc.SyncTreeCheckNode(currentTreeNode, CategoryRegisters);
                }
                SetProperty(ref _isOrderByCategory, value);
            }
        }

        private bool _isOrderByName = false;
        public bool IsOrderByName
        {
            get => _isOrderByName;
            set
            {
                if (value)
                {
                    OrderByType(null, OrderByTypeEnum.Name);
                }
                SetProperty(ref _isOrderByName, value);
            }
        }

        private bool _isOrderByAddress = false;
        public bool IsOrderByAddress
        {
            get => _isOrderByAddress;
            set
            {
                if (value)
                {
                    OrderByType(null, OrderByTypeEnum.Address);
                }
                SetProperty(ref _isOrderByAddress, value);
            }
        }

        public DelegateCommand ToggleDrawerCommand => new DelegateCommand(() => IsLeftOpen = !IsLeftOpen);

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }


        /// <summary>
        /// 从监测表移除
        /// </summary>
        public DelegateCommand RemoveTreeListToTableCommand => new DelegateCommand(() =>
        {
            var resultSelect = System.Windows.Forms.MessageBox.Show("是否从监测表执行移除，该操作会停止监测该数据并从图和表中移除！", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resultSelect != System.Windows.Forms.DialogResult.Yes)
            {
                return;
            }
            //当选中状态取消的时候触发
            var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            if (SelectAddress == null)
                return;
            var RemoveList = new ObservableCollection<RegisterAddrInfo>();
            foreach (var isWatch in CategoryRegisters)
            {
                if (SelectAddress.Where(x => x.Title == isWatch.Name).FirstOrDefault() == null)
                {
                    pms.StopRecord(isWatch.Id);//停止记录
                    isWatch.IsStartRecord = false;
                    if (!string.IsNullOrEmpty(isWatch.TableId))
                    {
                        var thisTab = WatchGroups.Where(x => x.Id == isWatch.TableId).FirstOrDefault();
                        if (thisTab != null)
                        {
                            //从监测表中移除
                            var allFields = thisTab.BitFields.Where(x => x.AddressId == isWatch.Id).ToList();
                            if (allFields != null && allFields.Count > 0)
                            {
                                var labels = thisTab.WpfPlotControl.Plot.GetPlottables();
                                var labels2 = thisTab.WpfPlotControl2.Plot.GetPlottables();
                                foreach (var rem in allFields)
                                {
                                    //var legLabel = labels.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                    var legLabel = labels.OfType<Scatter>().FirstOrDefault(s => s.LegendText == rem.Desc);
                                    if (legLabel != null)
                                        legLabel.IsVisible = false;//从波形图中移除
                                    //var legLabel2 = labels2.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                    var legLabel2 = labels2.OfType<Scatter>().FirstOrDefault(s => s.LegendText == rem.Desc);
                                    if (legLabel2 != null)
                                        legLabel2.IsVisible = false;//从波形图中移除
                                    thisTab.BitFields.Remove(rem);//从状态监测表中移除
                                }
                            }
                            thisTab.WpfPlotControl.Refresh();
                            thisTab.WpfPlotControl2.Refresh();
                        }
                        isWatch.TableId = null;
                    }
                    RemoveList.Add(isWatch);
                }
            }
            foreach (var removeObj in RemoveList)
            {
                CategoryRegisters.Remove(removeObj);
            }
        });
        /// <summary>
        /// 添加到监测表
        /// </summary>
        public DelegateCommand AddTreeListToTableCommand => new DelegateCommand(() =>
        {
            var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            if (SelectAddress == null)
                return;
            foreach (var item in SelectAddress)
            {
                var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == item.Title);
                var isHaveAdd = CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
                if (isHaveAdd == null)
                    CategoryRegisters.Add(register);
            }
        });

        #region TreeViewAddOrRemove
        public void AddRegisterForCheck(CategoryTree current)
        {
            var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == current.Title);
            var isHaveAdd = CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
            register.TableId = "placeholder";
            if (isHaveAdd == null)
                CategoryRegisters.Add(register);
        }
        public void RemoveRegisterForCheck(CategoryTree current)
        {
            //当选中状态取消的时候触发          
            var RemoveList = new ObservableCollection<RegisterAddrInfo>();
            var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == current.Title);
            var isWatch = CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
            if (isWatch != null)
            {
                pms.StopRecord(isWatch.Id);//停止记录
                isWatch.IsStartRecord = false;
                if (!string.IsNullOrEmpty(isWatch.TableId))
                {
                    var thisTab = WatchGroups.Where(x => x.Id == isWatch.TableId).FirstOrDefault();
                    if (thisTab != null)
                    {
                        //从监测表中移除
                        var allFields = thisTab.BitFields.Where(x => x.AddressId == isWatch.Id).ToList();
                        if (allFields != null && allFields.Count > 0)
                        {
                            var labels = thisTab.WpfPlotControl.Plot.GetPlottables();
                            var labels2 = thisTab.WpfPlotControl2.Plot.GetPlottables();
                            foreach (var rem in allFields)
                            {
                                var legLabel = labels.OfType<Scatter>().FirstOrDefault(s => s.LegendText == rem.Desc); // labels.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                if (legLabel != null)
                                    legLabel.IsVisible = false;//从波形图中移除
                                var legLabel2 = labels2.OfType<Scatter>().FirstOrDefault(s => s.LegendText == rem.Desc); // labels2.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                if (legLabel2 != null)
                                    legLabel2.IsVisible = false;//从波形图中移除
                                thisTab.BitFields.Remove(rem);//从状态监测表中移除
                            }
                        }
                        thisTab.WpfPlotControl.Refresh();
                        thisTab.WpfPlotControl2.Refresh();
                    }
                    isWatch.TableId = null;
                }
                RemoveList.Add(isWatch);
            }
            CategoryRegisters.Remove(isWatch);
        }
        #endregion
        public DelegateCommand<CategoryTree> CheckChangeCommand => new DelegateCommand<CategoryTree>((e) =>
        {
            if (e.IsCheck)
            {
                AddRegisterForCheck(e);
            }
            else
            {
                RemoveRegisterForCheck(e);
            }
        });

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
                param.IsCheck = !param.IsCheck;
                if (param.IsCheck)
                {
                    AddRegisterForCheck(param);
                }
                else
                {
                    RemoveRegisterForCheck(param);
                }
            }));

        private void OrderByType(string value, OrderByTypeEnum NameOrAddress)
        {
            var tempList = _projectManager.GetChipCategoryTree().GetMaxDepthLeaves().ToList().OrderBy(x => x.Title);
            if (NameOrAddress == OrderByTypeEnum.Address)
            {
                tempList = _projectManager.GetChipCategoryTree().GetMaxDepthLeaves().ToList().OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue);

            }
            if (!string.IsNullOrEmpty(value))
            {
                tempList = tempList.Where(x => x.AddressHex.Contains(value) || x.Title.Contains(value)).OrderBy(x => x.Title);
            }
            SingleParamTrees.Clear();
            SingleParamTrees.AddRange(tempList);
            UtilsFunc.SyncTreeCheckNode(SingleParamTrees, CategoryRegisters);
        }
        private void SearchCategoryTree(string keyword, bool isOrderByAddress = true)
        {
            SingleParamTrees.Clear();
            var source = _projectManager.GetChipCategoryTree(isOrderByAddress: isOrderByAddress);
            if (string.IsNullOrEmpty(keyword))
            {
                SingleParamTrees.AddRange(source);
            }
            else
            {
                var searcher = new TreeSearcher();
                var filteredResult = searcher.SearchInForest(source, keyword);
                SingleParamTrees.AddRange(filteredResult);
            }
        }

        public void LoadTreeData()
        {
            var tree = _projectManager.GetChipCategoryTree();
            SingleParamTrees.AddRange(tree);
            CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo)
                .FirstOrDefault(t => t.Name == tree[0].Children[0].Children[0].Title);

            var categoryOptions = _projectManager.GetCategories().Select(t => new ValueLabelOption() { Value = t, Label = t });
            SettingCategoryList.Clear();
            SettingCategoryList.AddRange(categoryOptions);
            CurrentCategory = SettingCategoryList.FirstOrDefault();

            CategoryAddressList.Clear();

            var CategoryAddressListOptions = _projectManager.GetRegisterForCategories(CurrentCategory.Value.ToString()).Select(t => new ValueLabelOption() { Value = t.AddressDec, Label = t.ShowAddressStr });
            CategoryAddressList.AddRange(CategoryAddressListOptions);
            CategoryAddress = CategoryAddressList.FirstOrDefault();

            var currentTreeNode = SingleParamTrees.GetMaxDepthLeaves().ToList();
            UtilsFunc.SyncTreeCheckNode(currentTreeNode, CategoryRegisters);
        }
        #endregion
        public DelegateCommand SettingAllTimeCommand => new DelegateCommand(() =>
        {
            if(string.IsNullOrEmpty(AllTime))
            {
                HandyControl.Controls.MessageBox.Show("请输入正确的数值!");
                return;
            }
            int setTime = 1;
            if (int.TryParse(AllTime,out setTime))
            { 
                if(setTime<1)
                {
                    HandyControl.Controls.MessageBox.Show("请输入大于等于1的数值!");
                    return;
                }
                foreach(var reg in CategoryRegisters)
                {
                    reg.RecordTime = setTime;
                }
            }
            else
            {
                HandyControl.Controls.MessageBox.Show("请输入正确的数值!");
                return;
            }
        });
        public DelegateCommand SettingAllTableCommand => new DelegateCommand(() =>
        {
            foreach (var reg in CategoryRegisters)
            {
                var tab = WatchGroups.FirstOrDefault(t => t.Id == reg.TableId);
                //清除原tab中的数据
                var groups = WatchGroups.Where(t => t.BitFields.Any(t => t.Name == reg.Name));
                foreach (var group in groups)
                {
                    var remain = group.BitFields.Where(t => t.Name != reg.Name).ToList();
                    group.BitFields.Clear();
                    group.BitFields.AddRange(remain);

                    var labels = group.WpfPlotControl.Plot.GetPlottables();
                    var labels2 = group.WpfPlotControl2.Plot.GetPlottables();
                    foreach (var lengLabel in labels)
                    {
                        if (lengLabel is Scatter sc)
                        {
                            sc.IsVisible = false;
                        }
                    }
                    foreach (var lengLabel in labels2)
                    {
                        if (lengLabel is Scatter sc)
                        {
                            sc.IsVisible = false;
                        }
                    }
                    group.WpfPlotControl.Refresh();
                    group.WpfPlotControl2.Refresh();
                }

                reg.TableId = SelectTab.Id;
                //找到Tab 
                if (tab == null)
                    return;
                //遍历寄存器下的BitField
                foreach (var bf in reg.BitFields)
                {
                    var clone = JsonHelper.DeepClone(bf);
                    clone.AddressHexName = reg.AddressHex;
                    clone.AddressId = reg.Id;
                    SelectTab.BitFields.Add(clone);
                }
            }
        });
        private DelegateCommand _closeCommand;

        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                if (pms != null)
                    pms.Disable();
                StopUiLoopAsync().ConfigureAwait(false);
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        private DelegateCommand _searchCommand;
        public DelegateCommand SearchCommand => _searchCommand ?? (_searchCommand = new DelegateCommand(() =>
        {
            LoadRegisters();
        }));
        public DelegateCommand<object> CloseChartCommand => new DelegateCommand<object>((e) =>
        {
            if (e is WatchChartModel chartToClose)
            {
                // 从 WatchGroups 集合中移除选中的选项卡
                WatchChartGroups.Remove(chartToClose);
                // （可选）如果需要，更新 CurrentTab 指向另一个有效选项卡
                if (CurrentChartTab == chartToClose)
                {
                    CurrentChartTab = WatchChartGroups.FirstOrDefault();
                }
            }
        });
        public DelegateCommand<object> CloseTabCommand => new DelegateCommand<object>((e) =>
        {
            if (e is WatchGroup tabToClose)
            {
                // 从 WatchGroups 集合中移除选中的选项卡
                WatchGroups.Remove(tabToClose);
                // （可选）如果需要，更新 CurrentTab 指向另一个有效选项卡
                if (CurrentTab == tabToClose)
                {
                    CurrentTab = WatchGroups.FirstOrDefault();
                }
            }
        });
        public DelegateCommand StopAllCommand => new DelegateCommand(() =>
        {
            foreach (var param in CategoryRegisters)
            {
                if (param.IsStartRecord)
                {
                    pms.StopRecord(param.Id);
                    param.IsStartRecord = false;
                }
            }
        });
        public DelegateCommand StartAllCommand => new DelegateCommand(() =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                System.Windows.Forms.MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach(var param in CategoryRegisters)
            {
                if(!param.IsStartRecord)
                {
                    param.IsStartRecord = true;
                    double recordTime = param.RecordTime;
                    if (param.RecordTimeTypeItem == ((int)RecordTimeType.Hour).ToString())
                    {
                        recordTime = param.RecordTime * 60 * 60;
                    }
                    if (param.RecordTimeTypeItem == ((int)RecordTimeType.Min).ToString())
                    {
                        recordTime = param.RecordTime * 60;
                    }
                    pms.StartRecord(param, TimeSpan.FromSeconds(recordTime));                    
                }                
            }
        });
        private DelegateCommand<RegisterAddrInfo> _beginRecordCommand;
        /// <summary>
        /// 开始记录
        /// </summary>
        public DelegateCommand<RegisterAddrInfo> BeginRecordCommand => _beginRecordCommand ?? (_beginRecordCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                System.Windows.Forms.MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            double recordTime = param.RecordTime;
            if (param.RecordTimeTypeItem == ((int)RecordTimeType.Hour).ToString())
            {
                recordTime = param.RecordTime * 60 * 60;
            }
            if (param.RecordTimeTypeItem == ((int)RecordTimeType.Min).ToString())
            {
                recordTime = param.RecordTime * 60;
            }
            pms.StartRecord(param, TimeSpan.FromSeconds(recordTime));
            param.IsStartRecord = true;
            //StartUiLoop(RefreshInterval);
        }));

        private DelegateCommand<RegisterAddrInfo> _stopRecordCommand;
        public DelegateCommand<RegisterAddrInfo> StopRecordCommand => _stopRecordCommand ?? (_stopRecordCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            pms.StopRecord(param.Id);
            param.IsStartRecord = false;
        }));

        public DelegateCommand<PPEC.Communication.Model.BitField> CaptureOldNameCommand => new DelegateCommand<PPEC.Communication.Model.BitField>((e) =>
        {
            var selectChart = WatchChartGroups.FirstOrDefault(x => x.Id == e.TableId);
            if (selectChart == null)
                return;
            if (selectChart.Header != "未选中")
            {
                if (selectChart != null)
                {
                    ChangeChartVisible(selectChart.WpfPlotControl2, false, e.Desc);
                    ChangeChartVisible(selectChart.WpfPlotControl, false, e.Desc);
                }
            }
        });

        public DelegateCommand<PPEC.Communication.Model.BitField> NameEditedCommand => new DelegateCommand<PPEC.Communication.Model.BitField>((e) =>
        {
            var selectChart = WatchChartGroups.FirstOrDefault(x => x.Id == e.TableId);
            if (selectChart == null)
                return;
            if (selectChart.Header != "未选中")
            {                
                ChangeChartVisble2(selectChart.WpfPlotControl2, e.Desc);
                ChangeChartVisble2(selectChart.WpfPlotControl, e.Desc);                
            }
        }); 
        private DelegateCommand _addWatchGroupCommand;
        public DelegateCommand AddWatchGroupCommand => _addWatchGroupCommand ?? (_addWatchGroupCommand = new DelegateCommand(() =>
        {
            var baseName = $"表{WatchGroups.Where(x=>x.Id!= "placeholder").Count() + 1}";
            var header = MakeUniqueHeader(baseName);
            var maxOrder = WatchGroups.Any() ? WatchGroups.Max(g => g.Order) : 0;
            WatchGroups.Add(new WatchGroup(_dialogService, session_id,_projectManager)
            {
                Id = Guid.NewGuid().ToString("N"),
                Header = header,
                TableColumns = InitTableColumns(),
                Order = maxOrder + 1,
            });

            if (CurrentTab == null)
                CurrentTab = WatchGroups.Last();
            //WatchGroups.Add(new WatchGroup(_dialogService,session_id)
            //{
            //    Id = Guid.NewGuid().ToString("N"),
            //    Header = $"表{WatchGroups.Count + 1}",
            //    TableColumns = InitTableColumns()
            //});
            //if (CurrentTab == null)
            //{
            //    CurrentTab = WatchGroups.Last();
            //}
        }));
        private string MakeUniqueHeader(string baseName)
        {
            var exists = new HashSet<string>(WatchGroups.Select(x => x.Header));
            if (!exists.Contains(baseName)) return baseName;

            int i = 1;
            string candidate;
            do
            {
                candidate = $"{baseName}-{i}";
                i++;
            } while (exists.Contains(candidate));

            return candidate;
        }
        public DelegateCommand AddWatchGroupChartCommand => new DelegateCommand(() =>
        {
            int nameCount = WatchChartGroups.Count == 1 ? 1 : WatchChartGroups.Count;
            var maxOrder = WatchChartGroups.Any(c => c.Id != "placeholder")
       ? WatchChartGroups.Where(c => c.Id != "placeholder").Max(c => c.Order)
       : 0;
            WatchChartModel wpfPlotControl = new WatchChartModel("监测图", _dialogService, session_id)
            {
                Id = Guid.NewGuid().ToString("N"),
                Header = $"图{nameCount}",
                Order = maxOrder + 1,
            };
            WatchChartGroups.Add(wpfPlotControl);
            if (CurrentChartTab == null || IsPlaceholder(CurrentChartTab))
            {
                CurrentChartTab = wpfPlotControl;
            }
            _watchChartGroupsForTab.Refresh();
            HasRealCharts = _watchChartGroupsForTab.Count > 0;
        });

        public DelegateCommand ShowWatchGroupCommand => new DelegateCommand(() =>
        {
            IDialogParameters dialogParameters = new DialogParameters();
            dialogParameters.Add("viewModel", this);
            _dialogService.Show(nameof(WatchTableListView), dialogParameters, r =>
            {
                if (r.Result == ButtonResult.OK)
                {

                }
            }, nameof(ShowTableListWindows));
        });

        public DelegateCommand ShowChartGroupCommand => new DelegateCommand(() =>
        {
            IDialogParameters dialogParameters = new DialogParameters();
            dialogParameters.Add("viewModel", this);
            _dialogService.Show(nameof(WatchChartListView), dialogParameters, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                }
            }, nameof(ShowChartListWindows));
        });

        public DelegateCommand HistoryDownloadCommand => new DelegateCommand(() =>
        {
            try
            {
                var fbd = new FolderBrowserDialog();
                fbd.Description = "请选择保存路径";
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = fbd.SelectedPath;
                    string currentSessionID = null;
                    if (_projectManager != null)
                    {
                        currentSessionID = _projectManager.CurrentProject.ActiveSessionId;
                    }

                    ExporterExcel exporterExcel = new ExporterExcel();
                    exporterExcel.ExportSessionToExcel_MergedByTimeAndId(currentSessionID, path);
                    //HistoryToExcel(path);
                }
            }
            catch (Exception ex) 
            { 
            }
        });

        private ObservableCollection<TableColumn> InitTableColumns()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] { "序号", "名称", "寄存器地址(HEX)", "解析范围", "解析要求", "解析结果", "原始值(Dec)", "原始值(Bit)", "单位", "添加到监测图" };
            for (int i = 0; i < arr.Length; i++)
            {
                var tab = new TableColumn()
                {
                    Name = arr[i],
                };
                if (arr[i] == "原始值(Dec)" || arr[i] == "原始值(Bit)")
                {
                    tab.IsChecked = false;
                }
                target.Add(tab);
            }
            return target;
        }

        private DelegateCommand<RegisterAddrInfo> _tableChangeCommand;
        public DelegateCommand<RegisterAddrInfo> TableChangeCommand => _tableChangeCommand ?? (_tableChangeCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            var tab = WatchGroups.FirstOrDefault(t => t.Id == param.TableId);
            //清除原tab中的数据
            var groups = WatchGroups.Where(t => t.BitFields.Any(t => t.Name == param.Name));
            foreach (var group in groups)
            {
                var remain = group.BitFields.Where(t => t.Name != param.Name).ToList();
                group.BitFields.Clear();
                group.BitFields.AddRange(remain);

                var labels = group.WpfPlotControl.Plot.GetPlottables();
                var labels2 = group.WpfPlotControl2.Plot.GetPlottables();
                foreach (var lengLabel in labels)
                {
                    if (lengLabel is Scatter sc)
                    {
                        sc.IsVisible = false;
                    }
                }
                foreach (var lengLabel in labels2)
                {
                    if (lengLabel is Scatter sc)
                    {
                        sc.IsVisible = false;
                    }
                }
                group.WpfPlotControl.Refresh();
                group.WpfPlotControl2.Refresh();
            }

            
            //找到Tab 
            if (tab == null)
                return;
            if (tab.Header == "未选中")
            { }
            //遍历寄存器下的BitField
            foreach (var bf in param.BitFields)
            {
                var clone = JsonHelper.DeepClone(bf);
                clone.AddressHexName = param.AddressHex;
                clone.AddressId = param.Id;
                tab.BitFields.Add(clone);
            }
        }));

        public void ReLoadChartTable()//重新加载后清除选中
        {
            foreach(var table in WatchGroups)
            {
                foreach(var filed in table.BitFields)
                {
                    var selectChart = WatchChartGroups.FirstOrDefault(x => x.Id == filed.TableId);
                    
                    //filed.SelectedChartValue = null;
                    filed.TableId = null;
                }
            }
        }
        public DelegateCommand<SelectionChangedEventArgs> ChartTableChangeCommand => new DelegateCommand<SelectionChangedEventArgs>((e) =>
        {
            try
            {
                if (e == null) return;

                var selectedItem = e.AddedItems?.OfType<WatchChartModel>().FirstOrDefault();
                if (selectedItem == null) return;
                var cb = e.Source as System.Windows.Controls.ComboBox ?? e.OriginalSource as System.Windows.Controls.ComboBox;
                if (cb == null) return;

                var row = cb.DataContext as PPEC.Communication.Model.BitField;
                if (row == null) return;

                var selectChart = WatchChartGroups.FirstOrDefault(x => x.Id == row.TableId);
                var selectTable = WatchChartGroups.FirstOrDefault(x => x.Id == selectedItem.Id);

                if (selectedItem.Header == "未选中" && selectedItem.Id != row.TableId)
                {
                    if (selectChart != null)
                    {
                        ChangeChartVisible(selectChart.WpfPlotControl2, false, row.Desc);
                        ChangeChartVisible(selectChart.WpfPlotControl, false, row.Desc);
                    }
                    //row.SelectedChartValue = null;
                    row.TableId = null;
                    return;
                }

                if (selectedItem.Id != row.TableId)
                {
                    if (selectChart != null)
                    {
                        ChangeChartVisible(selectChart.WpfPlotControl2, false, row.Desc);
                        ChangeChartVisible(selectChart.WpfPlotControl, false, row.Desc);
                    }
                    if (selectTable != null)
                    {
                        ChangeChartVisble2(selectTable.WpfPlotControl2, row.Desc);
                        ChangeChartVisble2(selectTable.WpfPlotControl, row.Desc);
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

        public DelegateCommand<object> SelectAllCommand => new DelegateCommand<object>((e) =>
        {
            SingleParamTrees.SetAllLeavesChecked((bool)e);
        });

        #region Method
        /// <summary>
        /// 波形图参数添加
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="paramName"></param>
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
        /// <summary>
        /// 波形图参数显示设置
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="isShow"></param>
        /// <param name="paramName"></param>
        public void ChangeChartVisible(WpfPlotSteamBase chart, bool isShow, string paramName)
        {
            var legLabel = chart.Plot.GetPlottables().OfType<Scatter>().FirstOrDefault(s => s.LegendText == paramName);
            if (legLabel != null)
            {
                if(isShow)
                {
                    legLabel.IsVisible = isShow;
                }
                else
                {
                    legLabel.IsVisible = isShow;
                    //chart.Plot.Remove(legLabel);
                }
            }
            else
            {
                chart.AddSignalData(paramName);
            }
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() => chart.RefreshData());
            //chart.RefreshData();
        }

        #region UpdateUi  更新界面内容
        private Task _uiLoopTask;
        private void StartUiLoop(int periodMs)
        {
            if (_uiLoopTask != null && !_uiLoopTask.IsCompleted) return;    // 已在运行
            if (_cts == null || _cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();
            _uiLoopTask = Task.Run(() => UiLoopAsync(periodMs, _cts.Token));
        }
        private async Task StopUiLoopAsync()
        {
            if (_uiLoopTask == null) return;

            _cts.Cancel();
            try { await _uiLoopTask; }
            catch (OperationCanceledException ex) { _log.Error(ex); }
            finally { _uiLoopTask = null; }
        }
        private async Task UiLoopAsync(int periodMs, CancellationToken token)
        {
            var sw = new System.Diagnostics.Stopwatch();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    sw.Restart();

                    // 拍快照，防止枚举时集合被修改
                    var snapshot = WatchGroups.ToArray();

                    // ★ 这里完全在后台线程跑 — 不阻塞 UI
                    foreach (var group in snapshot)
                    {
                        await UpdateGroupAsync(group, token);
                    }
                    //if (pms._watching.Count == 0)
                    //{
                    //    await StopUiLoopAsync();
                    //}
                    // 补偿延时

                    var remain = periodMs - (int)sw.ElapsedMilliseconds;
                    if (remain > 0)
                        await Task.Delay(remain, token);

                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 正常取消，不记为错误
            }
            catch (Exception ex)
            {
                _log.Error("UiLoop crashed", ex);
                throw;
            }            
        }

        public async Task UpdateGroupAsync(WatchGroup group, CancellationToken token)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                bool anyChanged = false;
                foreach (var field in group.BitFields.ToArray())
                {
                    var newValue = _projectManager.GetRegisterValue(field.AddressHexName);
                    if (newValue == null)
                        continue;
                    var newField = newValue.BitFields
                                         .FirstOrDefault(x => x.StartBit == field.StartBit);
                    if (newField != null)
                    {
                        if (!Equals(field.Result, newField.Result)) { field.Result = newField.Result; anyChanged = true; }
                        if (!Equals(field.ReadBinary, newField.ReadBinary)) { field.ReadBinary = newField.ReadBinary; anyChanged = true; }
                        if (!Equals(field.Value, newField.Value)) { field.Value = newField.Value; anyChanged = true; }
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
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
                        foreach (var chartTab in WatchChartGroups.ToArray())
                        {
                            chartTab.WpfPlotControl2.RefreshData(false);
                            chartTab.WpfPlotControl.RefreshData(false);
                        }
                    });
                }
            }
            catch(Exception ex)
            { _log.Error(ex); }
            finally
            {
                Interlocked.Exchange(ref _busy4, 0);
            }
            
        }
        private int _busy3;
        private void RecordTime_Tick(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _busy3, 1) == 1) return;

            try
            {
                foreach (var param in CategoryRegisters.ToArray())
                {
                    if (param.IsStartRecord)
                    {
                        foreach (var newField in param.BitFields.ToArray())
                        {
                            var tempdic = new Dictionary<string, double>();
                            if (newField.Id == null)
                                continue;
                            tempdic.Add(newField.Id, newField.Result);
                            //开始存储，记录历史记录
                            pipeLineIng.Enqueue(new Sample
                            {
                                TimestampUtcMs = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds,
                                Values = tempdic,
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { _log.Error(ex); }
            finally
            {
                Interlocked.Exchange(ref _busy3, 0);
            }
        }
        private int _busy2;
        private void ReceiveTimer_Tick(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _busy2, 1) == 1) return;
            try
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    foreach (var group in WatchGroups.ToArray())
                    {
                        var fields = group.BitFields?.ToArray() ?? Array.Empty<PPEC.Communication.Model.BitField>();
                        foreach (var field in fields)
                        {
                            if (field.TableId == null) continue;
                            // 注意：这里用的是寄存器Id做键
                            if (field.AddressId != null && pms._watching.ContainsKey(field.AddressId))
                            {
                                var chart = WatchChartGroups.FirstOrDefault(x => x.Id == field.TableId);
                                if (chart != null)
                                {
                                    chart.WpfPlotControl2.UpdateData(field.Desc, field.Result);
                                    chart.WpfPlotControl.UpdateData(field.Desc, field.Result);
                                }
                            }
                        }
                    }
                });
                
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _busy2, 0);
            }
            
        }
        private int _busy;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _busy, 1) == 1) return;
            try
            {
                foreach (var group in WatchGroups.ToArray())
                {
                    var fields = group.BitFields?.ToArray() ?? Array.Empty<PPEC.Communication.Model.BitField>();
                    foreach (var field in fields)
                    {
                        var unitValue = _projectManager.CurrentProject.CommService?.Read(field.AddressHexName);
                        if (unitValue == null) continue;
                        _projectManager.SetRegisterValue(field.Name, unitValue.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }
        #endregion
        public void Dispose()
        {
            try
            {
                if (WatchChartGroups != null && _chartGroupsChangedHandler != null)
                    WatchChartGroups.CollectionChanged -= _chartGroupsChangedHandler;
                _cts.Cancel();
                _timer.Stop();
                _timer.Elapsed -= Timer_Tick; // 设置触发事件
                _ReceiveTimer.Elapsed -= ReceiveTimer_Tick;
                _recordTime.Elapsed -= RecordTime_Tick; // 设置触发事件
                _refTime.Elapsed -= RefTime_Tick; // 设置触发事件
                _ReceiveTimer.Stop();
                _recordTime.Stop();
                _refTime.Stop();
                pms.Disable();
                StopUiLoopAsync().ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
        } 

        public void EventListener()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Subscribe(() =>
            {
                foreach (var isWatch in CategoryRegisters)
                {
                    pms.StopRecord(isWatch.Id);//停止记录
                    isWatch.IsStartRecord = false;
                }
                _timer.Stop();
                _ReceiveTimer.Stop();
                _recordTime.Stop();
                _refTime.Stop();
                pms.Disable();
                StopUiLoopAsync().ConfigureAwait(false);
            });
        }
        #endregion
        public override void LoadData()
        {
            InitData();
            StartUiLoop(RefreshInterval);
            LoadTreeData();
        }

        private void InitData()
        {
            var categoryOptions = _projectManager.GetCategories().Select(t => new ValueLabelOption() { Value = t, Label = t });
            SettingCategoryList.Clear();
            SettingCategoryList.AddRange(categoryOptions);
            CurrentSettingCategory = SettingCategoryList.FirstOrDefault();

            //LoadRegisters();

            if (CurrentTab == null)
                CurrentTab = WatchGroups.FirstOrDefault();
        }

        private void LoadRegisters()
        {
            var categoryFliter = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo)
                .Where(t => t.Category == CurrentSettingCategory.Value.ToString())
                .ToList();
            if (!string.IsNullOrEmpty(AddressKeyword))
            {
                categoryFliter = categoryFliter.Where(t => t.AddressDec.ToString().StartsWith(AddressKeyword) || t.AddressHex.StartsWith(AddressKeyword)).ToList();
            }
            CategoryRegisters.Clear();
            CategoryRegisters.AddRange(categoryFliter);
            if (categoryFliter.Any())
            {
                CurrentRegister = categoryFliter[0];
            }
            else
            {
                CurrentRegister = null;
            }
        }

        private void InitOrderAndSort()
        {
            // WatchGroups
            if (WatchGroups != null)
            {
                foreach (var c in WatchGroups)
                    if (c.Id == "placeholder") c.Order = int.MinValue;
                // 初始化缺省 Order（按当前顺序补齐）
                for (int i = 0; i < WatchGroups.Count; i++)
                    if (WatchGroups[i].Order == 0 && WatchGroups[i].Id != "placeholder") 
                        WatchGroups[i].Order = i; // 按需适配你的默认值策略

                var viewGroups = System.Windows.Data.CollectionViewSource.GetDefaultView(WatchGroups);
                viewGroups.SortDescriptions.Clear();
                viewGroups.SortDescriptions.Add(new SortDescription(nameof(WatchGroup.Order), ListSortDirection.Ascending));

                _watchGroupsForTab.SortDescriptions.Clear();
                _watchGroupsForTab.SortDescriptions.Add(new SortDescription(nameof(WatchGroup.Order), ListSortDirection.Ascending));
            }

            // WatchChartGroups
            if (WatchChartGroups != null)
            {
                // 占位项（未选中）固定最前
                foreach (var c in WatchChartGroups)
                    if (c.Id == "placeholder") c.Order = int.MinValue;

                for (int i = 0; i < WatchChartGroups.Count; i++)
                    if (WatchChartGroups[i].Order == 0 && WatchChartGroups[i].Id != "placeholder")
                        WatchChartGroups[i].Order = i; // 初始化

                // 默认视图排序（ItemsControl 会用到默认视图）
                var viewCharts = System.Windows.Data.CollectionViewSource.GetDefaultView(WatchChartGroups);
                viewCharts.SortDescriptions.Clear();
                viewCharts.SortDescriptions.Add(new SortDescription(nameof(WatchChartModel.Order), ListSortDirection.Ascending));

                // 你的 Tab 专用视图也加上排序（原来只有 Filter）
                _watchChartGroupsForTab.SortDescriptions.Clear();
                _watchChartGroupsForTab.SortDescriptions.Add(new SortDescription(nameof(WatchChartModel.Order), ListSortDirection.Ascending));
            }
        }
    }
}
