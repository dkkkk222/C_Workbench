using Force.DeepCloner;
using NPOI.SS.Formula.Functions;
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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    public class WatchViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        public int RefreshInterval = 500;//UI更新间隔
        public System.Timers.Timer _timer = new System.Timers.Timer();
        public System.Timers.Timer _recordTime=new System.Timers.Timer();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public IngestPipeline pipeLineIng { get; set; }
        public string session_id { get; set; }
        public WatchViewModel(IEventAggregator eventAggregator, ProjectManager projectManager, IDialogService dialogService)
        {
            _projectManager = projectManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            WatchGroups = _projectManager.CurrentProject.WatchGroups;
            CategoryRegisters = _projectManager.CurrentProject.CategoryRegisters;
            pms =new ParameterMonitorService(10) { CurrentProject= _projectManager.CurrentProject };
            pms.Enable();

            _timer.Interval = 1; // 设置触发间隔
            _timer.Elapsed += Timer_Tick; // 设置触发事件

            _recordTime.Interval = 500; // 设置触发间隔
            _recordTime.Elapsed += RecordTime_Tick; // 设置触发事件
            EventListener();


            
            foreach (var group in WatchGroups)
            {
                group.Inject(dialogService);
            }
            session_id = Guid.NewGuid().ToString();
            pipeLineIng = new IngestPipeline(session_id);
        }

        #region Property
        public bool _isActive = false;
        public new bool IsActive
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
                        _recordTime.Start();
                        pms.Enable();
                        StartUiLoop(RefreshInterval);
                    }
                    else
                    {
                        _timer.Stop();
                        _recordTime.Stop();
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

        private WatchGroup _currentTab;
        public WatchGroup CurrentTab
        {
            get => _currentTab;
            set => SetProperty(ref _currentTab, value);
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
                if(SetProperty(ref _currentSettingCategory, value))
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
                else if(IsOrderByName)
                {
                    OrderByType(value,OrderByTypeEnum.Name);
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
                    var currentTreeNode=SingleParamTrees.GetMaxDepthLeaves().ToList();
                     
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
                    OrderByType(null,OrderByTypeEnum.Name);
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
                    OrderByType(null,OrderByTypeEnum.Address);
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
        /// 从监控表移除
        /// </summary>
        public DelegateCommand RemoveTreeListToTableCommand => new DelegateCommand(() =>
        {
            var resultSelect = MessageBox.Show("是否从监控表执行移除，该操作会停止监控该数据并从图和表中移除！", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
                if(SelectAddress.Where(x => x.Title == isWatch.Name).FirstOrDefault()==null)
                {
                    pms.StopRecord(isWatch.Id);//停止记录
                    isWatch.IsStartRecord = false;
                    if(!string.IsNullOrEmpty(isWatch.TableId))
                    {
                        var thisTab = WatchGroups.Where(x => x.Id == isWatch.TableId).FirstOrDefault();
                        if (thisTab != null)
                        {
                            //从监测表中移除
                            var allFields=thisTab.BitFields.Where(x => x.AddressId == isWatch.Id).ToList();
                            if(allFields!=null&& allFields.Count>0)
                            {
                                var labels = thisTab.WpfPlotControl.Plot.GetPlottables();
                                var labels2 = thisTab.WpfPlotControl2.Plot.GetPlottables();
                                foreach (var rem in allFields)
                                {
                                    var legLabel = labels.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                    if(legLabel!=null)
                                        legLabel.IsVisible = false;//从波形图中移除
                                    var legLabel2 = labels2.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
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
            foreach(var removeObj in RemoveList)
            {
                CategoryRegisters.Remove(removeObj);
            }
        });
        /// <summary>
        /// 添加到监控表
        /// </summary>
        public DelegateCommand AddTreeListToTableCommand => new DelegateCommand(() =>
        {
            var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            if (SelectAddress == null)
                return;
            foreach (var item in SelectAddress)
            {
                var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == item.Title);
                var isHaveAdd=CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
                if(isHaveAdd==null)
                    CategoryRegisters.Add(register);
            }
        });

        #region TreeViewAddOrRemove
        public void AddRegisterForCheck(CategoryTree current)
        {
            var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == current.Title);
            var isHaveAdd = CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
            if (isHaveAdd == null)
                CategoryRegisters.Add(register);
        }
        public void RemoveRegisterForCheck(CategoryTree current)
        {            
            //当选中状态取消的时候触发          
            var RemoveList = new ObservableCollection<RegisterAddrInfo>();
            var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == current.Title);
            var isWatch = CategoryRegisters.Where(x => x.AddressDec == register.AddressDec).FirstOrDefault();
            if(isWatch != null)
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
                                var legLabel = labels.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
                                if (legLabel != null)
                                    legLabel.IsVisible = false;//从波形图中移除
                                var legLabel2 = labels2.Where(x => (x as Scatter).LegendText.Equals(rem.Desc)).FirstOrDefault();
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
            if(e.IsCheck)
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
            if(NameOrAddress== OrderByTypeEnum.Address)
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

        private DelegateCommand<RegisterAddrInfo> _beginRecordCommand;
        /// <summary>
        /// 开始记录
        /// </summary>
        public DelegateCommand<RegisterAddrInfo> BeginRecordCommand => _beginRecordCommand ?? (_beginRecordCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            pms.StartRecord(param,TimeSpan.FromSeconds(param.RecordTime));
            param.IsStartRecord = true;
            //StartUiLoop(RefreshInterval);
        }));

        private DelegateCommand<RegisterAddrInfo> _stopRecordCommand;
        public DelegateCommand<RegisterAddrInfo> StopRecordCommand => _stopRecordCommand ?? (_stopRecordCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            pms.StopRecord(param.Id);
            param.IsStartRecord = false;
        }));

        private DelegateCommand _addWatchGroupCommand;
        public DelegateCommand AddWatchGroupCommand => _addWatchGroupCommand ?? (_addWatchGroupCommand = new DelegateCommand(() =>
        {

            WatchGroups.Add(new WatchGroup(_dialogService,session_id)
            {
                Id = Guid.NewGuid().ToString("N"),
                Header = $"表{WatchGroups.Count + 1}",
                TableColumns = InitTableColumns()
            });
            if (CurrentTab == null)
            {
                CurrentTab = WatchGroups.Last();
            }
        }));

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

        
        private ObservableCollection<TableColumn> InitTableColumns()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] { "序号", "名称", "寄存器地址(HEX)", "解析范围", "解析要求", "解析结果","原始值(Dec)","原始值(Bit)", "单位", "添加到监测图" };
            for (int i = 0; i < arr.Length; i++)
            {
                var tab = new TableColumn()
                {
                    Name = arr[i],
                };
                if(arr[i]== "原始值(Dec)"|| arr[i] == "原始值(Bit)")
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
                    if(lengLabel is Scatter sc)
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
            var tab = WatchGroups.FirstOrDefault(t => t.Id == param.TableId);
            if (tab == null)
                return;
            //遍历寄存器下的BitField
            foreach(var bf in param.BitFields)
            {
                var clone = JsonHelper.DeepClone(bf);
                clone.AddressHexName = param.AddressHex;
                clone.AddressId = param.Id;
                tab.BitFields.Add(clone);
            }
            //param.BitFields.ForEach(bf =>
            //{
            //    var clone = JsonHelper.DeepClone(bf);
            //    clone.AddressHexName = param.AddressHex;
            //    clone.AddressId = param.Id;
            //    tab.BitFields.Add(clone);
            //});
        }));

        #region Method

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
            catch (OperationCanceledException) { }
            finally { _uiLoopTask = null; }
        }
        private async Task UiLoopAsync(int periodMs, CancellationToken token)
        {
            var sw = new System.Diagnostics.Stopwatch();

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

        public async Task UpdateGroupAsync(WatchGroup group, CancellationToken token)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var field in group.BitFields)
                {
                    var newValue = _projectManager.GetRegisterValue(field.AddressHexName);
                    if (newValue == null)
                        continue;
                    var newField = newValue.BitFields
                                         .FirstOrDefault(x => x.StartBit == field.StartBit);
                    if (newField != null)
                    {
                        field.Result = newField.Result;
                        field.ReadBinary = newField.ReadBinary;
                        field.Value = newField.Value;

                        group.WpfPlotControl.RefreshData(true);
                        group.WpfPlotControl2.RefreshData(true);
                    }                        
                }
            });
        }

        private void RecordTime_Tick(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                foreach(var param in CategoryRegisters)
                {
                    if(param.IsStartRecord)
                    {
                        foreach(var newField in param.BitFields)
                        {
                            var tempdic = new Dictionary<string, double>();
                            tempdic.Add(newField.Id, newField.Result);
                            //开始存储，记录历史记录
                            pipeLineIng.Enqueue(new Sample
                            {
                                TimestampUtcMs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                                Values = tempdic,
                            });
                        }                        
                    }                    
                }
                
            });
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var group in WatchGroups.ToArray())
                    {
                        foreach (var field in group.BitFields)
                        {
                            var unitValue = _projectManager.CurrentProject.CommService?.Read(field.AddressHexName);
                            if (unitValue == null)
                                continue;
                            _projectManager.SetRegisterValue(field.Name, unitValue.Value);
                            var newValue = _projectManager.GetRegisterValue(field.AddressHexName);
                            if (newValue == null)
                                continue;
                            var newField = newValue.BitFields
                                                 .FirstOrDefault(x => x.StartBit == field.StartBit);
                            if (newField != null)
                            {
                                if (field.IsSelected)
                                {
                                    if (pms._watching.ContainsKey(field.AddressId))
                                    {
                                        //更新波形数据
                                        group.WpfPlotControl.UpdateData(field.Desc, field.Result);
                                        group.WpfPlotControl2.UpdateData(field.Desc, field.Result);
                                    }
                                }
                            }
                        }
                    }
                       
                }
                catch(Exception ex)
                {

                }
            });
        }
        #endregion
        public void Dispose() => _cts.Cancel();

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
                _recordTime.Stop();
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
    }
}
