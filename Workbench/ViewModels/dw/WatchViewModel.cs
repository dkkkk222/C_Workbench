using Force.DeepCloner;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;
using Workbench.Views;
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

        private CancellationTokenSource _cts = new CancellationTokenSource();
        public WatchViewModel(IEventAggregator eventAggregator, ProjectManager projectManager, IDialogService dialogService)
        {
            _projectManager = projectManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            WatchGroups = _projectManager.CurrentProject.WatchGroups;
            pms=new ParameterMonitorService(3) { CurrentProject= _projectManager.CurrentProject };
            pms.Enable();
        }

        #region Property

        #endregion
        public bool _isActive = false;
        public new bool IsActive
        {
            get
            {
                return _isActive;
            }
            set 
            { 
                if(SetProperty(ref _isActive, value))
                {
                    if(value)
                    {
                        pms.Enable();
                        StartUiLoop(RefreshInterval);
                    }
                    else
                    {
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
            set => SetProperty(ref _currentSettingCategory, value);
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
            //var tab = WatchGroups.FirstOrDefault(t => t.Id == param.TableId);
            //if (tab == null) return;
            //double[] dataX = { 1, 2, 3, 4, 5 };
            //double[] dataY = Enumerable.Range(0, 100).OrderBy(d => new Random().Next()).Select(x => (double)x).Take(5).ToArray<double>();
            //tab.PlotControl.Plot.Add.Scatter(dataX, dataY);
            //tab.PlotControl.Refresh();
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

            WatchGroups.Add(new WatchGroup(_dialogService)
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

        private ObservableCollection<TableColumn> InitTableColumns()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] { "序号", "名称", "源寄存器地址", "解析范围", "解析要求", "解析结果","原始值(Dec)","原始值(Bit)", "单位", "添加到监测图" };
            for (int i = 0; i < arr.Length; i++)
            {
                var tab = new TableColumn()
                {
                    Name = arr[i],
                };
                if(arr[i]== "原始值(Dec)")
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
            }

            //找到Tab
            var tab = WatchGroups.FirstOrDefault(t => t.Id == param.TableId);
            if (tab == null)
                return;
            //遍历寄存器下的BitField
            param.BitFields.ForEach(bf =>
            {
                var clone = JsonHelper.DeepClone(bf);
                clone.AddressHexName = param.AddressHex;
                tab.BitFields.Add(clone);
            });
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
                        field.Result = newField.Result;
                        field.ReadBinary = newField.ReadBinary;
                        field.Value = newField.Value;
                    }
                        
                }

            });
        }
        #endregion


        public void Dispose() => _cts.Cancel();

        #endregion
        public override void LoadData()
        {
            InitData();
            StartUiLoop(RefreshInterval);
        }

        private void InitData()
        {
            var categoryOptions = _projectManager.GetCategories().Select(t => new ValueLabelOption() { Value = t, Label = t });
            SettingCategoryList.Clear();
            SettingCategoryList.AddRange(categoryOptions);
            CurrentSettingCategory = SettingCategoryList.FirstOrDefault();

            LoadRegisters();

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
