using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        public TelemetryViewModel(IEventAggregator eventAggregator, ProjectManager projectManager) 
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            SequenceList = _projectManager.CurrentProject.TeleMetrySequences;
            GetTeleInit();
        }

        #region Property
        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

        private ObservableCollection<Sequence> _sequenceList = new ObservableCollection<Sequence>();
        public ObservableCollection<Sequence> SequenceList
        {
            get => _sequenceList;
            set => SetProperty(ref _sequenceList, value);
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

        public bool _batchAllCheck;
        public bool BatchAllCheck
        {
            get => _batchAllCheck;
            set
            {
                SetProperty(ref _batchAllCheck, value);
            }
        }

        private bool _checkAll = false;
        public bool CheckAll
        {
            get => _checkAll;
            set
            {
                SetProperty(ref _checkAll, value);
                foreach (var item in SequenceList)
                {
                    item.IsChecked = value;
                }
            }
        }

        private Sequence _currentSequence;
        public Sequence CurrentSequence
        {
            get => _currentSequence;
            set => SetProperty(ref _currentSequence, value);
        }

        private TelemetryCode _currentRegister;
        public TelemetryCode CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
        }

        private TelemetryCode _writeCurrentRegister;
        public TelemetryCode WriteCurrentRegister
        {
            get => _writeCurrentRegister;
            set => SetProperty(ref _writeCurrentRegister, value);
        }
        #endregion


        private DelegateCommand _closeCommand;
        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));
        public DelegateCommand ToggleDrawerCommand => new DelegateCommand(() => IsLeftOpen = !IsLeftOpen);

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = ListTele.FirstOrDefault(t => t.Name == param.Title);
                param.IsCheck = !param.IsCheck;

            }));

        public DelegateCommand<object> SelectAllCommand => new DelegateCommand<object>((e) =>
        {
            SingleParamTrees.SetAllLeavesChecked((bool)e);
        });

        #region SqeCommand
        private DelegateCommand _addSequenceCommand;
        public DelegateCommand AddSequenceCommand => _addSequenceCommand ?? (_addSequenceCommand = new DelegateCommand(() =>
        {
            var indexS = SequenceList.Count() + 1;
            string nameS = "序列" + indexS;

            SequenceList.Add(new Sequence
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = nameS
            });
        }));

        private DelegateCommand<Sequence> _sendCommand;
        public DelegateCommand<Sequence> SendCommand => _sendCommand ?? (_sendCommand = new DelegateCommand<Sequence>(async (param) =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await Task.Run(async () =>
            {
                await SendSequence(param);
            });
        }));

        private DelegateCommand _batchSendCommand;
        public DelegateCommand BatchSendCommand => _batchSendCommand ?? (_batchSendCommand = new DelegateCommand(async () =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var seq in SequenceList.Where(t => t.IsChecked))
            {
                await Task.Run(async () =>
                {
                    await SendSequence(seq);
                });
            }
        }));

        public DelegateCommand BatchDelCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否批量删除序列", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var tempRemove = SequenceList.Where(t => t.IsChecked).ToArray();
                        foreach (var seq in tempRemove)
                        {
                            SequenceList.Remove(seq);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }
        });

        private async Task SendSequence(Sequence param)
        {
            var currentProject = _projectManager.CurrentProject;
            param.Progress = 0;
            param.CompletedNumTelemetry = 0;
            Thread.Sleep(1000);
            foreach (var register in param.TelemetryItems)
            {
                
                switch (currentProject.CommunicationType)
                {
                    case Constants.OldSERIAL_PORT:
                    case Constants.Modbus: 
                    case Constants.I2C:
                    case Constants.CAN:                         
                        break;
                    case Constants.Telemetry:
                        if(register.Type == ((int)TelemetryCommandType.IndirectCommand).ToString())
                        {
                            var cmd = UtilsFunc.HexStringToBytes(register.Code);
                            await currentProject.CommService.SendRemoteControlAsync(cmd,50);
                        }
                        if (register.Type == ((int)TelemetryCommandType.NoteInstruction).ToString())
                        {
                            var injection = UtilsFunc.HexStringToBytes(register.Code);
                            await currentProject.CommService.SendInjectionAsync(injection, 50);
                        }
                        
                        break;
                }
                //await currentProject.CommService.SendAsync(calcResult.bytes);
                param.CompletedNumTelemetry += 1;
                Thread.Sleep(TimeSpan.FromMilliseconds(2));
            }
        }

        private DelegateCommand _addRegisterToSequenceCommand;
        public DelegateCommand AddRegisterToSequenceCommand => _addRegisterToSequenceCommand ?? (_addRegisterToSequenceCommand = new DelegateCommand(() =>
        {
            if (CurrentRegister == null)
            {
                MessageBox.Show("请选择寄存器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (CurrentSequence == null)
            {
                MessageBox.Show("请选择序列", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            foreach (var item in SelectAddress)
            {
                var register = ListTele.FirstOrDefault(t => t.Name == item.Title);
                var clone = JsonHelper.DeepClone(register);
                clone.Id = Guid.NewGuid().ToString("N");
                CurrentSequence.TelemetryItems.Add(clone);
                item.IsCheck = false;
            }
            this.BatchAllCheck = false;
        }));

        public DelegateCommand BatchDelRegisterCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否批量删除序列详情", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            var delSeq = CurrentSequence.TelemetryItems.Where(t => t.IsChecked).ToArray();
            foreach (var item in delSeq)
            {
                CurrentSequence.TelemetryItems.Remove(item);
            }
            CollectionViewSource.GetDefaultView(CurrentSequence.TelemetryItems).Refresh();
        });
        #endregion
        public override async void LoadData()
        {
            var tree =await _projectManager.GetChipCategoryTreeForTele();
            SingleParamTrees.AddRange(tree);
        }

        public List<TelemetryCode> ListTele { get; set; }
        #region Method
        public async void GetTeleInit()
        {
            ListTele =await _projectManager.GetTeleLisst(_projectManager.CurrentProject.Chip.ChipId);
            CurrentRegister = ListTele.FirstOrDefault();
        }

        public void ChangeIsConfigPaneOpen(TelemetryCode param)
        {
            var selected = param;
            bool same = IsSameRegister(selected, WriteCurrentRegister);
            if (same && selected != null)
            {
                //IsConfigPaneOpen = true;
            }
            else
            {
                WriteCurrentRegister = param;
                //IsConfigPaneOpen = true;
            }
        }

        private static bool IsSameRegister(TelemetryCode a, TelemetryCode b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;

            // 优先按 AddressHex 比较（你两边通常都有这个字段）
            var aAddr = GetStringProp(a, "Name");
            var bAddr = GetStringProp(b, "Name");
            if (!string.IsNullOrEmpty(aAddr) && !string.IsNullOrEmpty(bAddr))
                return string.Equals(aAddr, bAddr, StringComparison.OrdinalIgnoreCase);

            // 备用：按 Id 比较（如果你的模型有 Id）
            var aId = GetStringProp(a, "Code");
            var bId = GetStringProp(b, "Code");
            if (!string.IsNullOrEmpty(aId) && !string.IsNullOrEmpty(bId))
                return string.Equals(aId, bId, StringComparison.OrdinalIgnoreCase);

            return false;
        }

        private static string GetStringProp(object o, string name)
       => o?.GetType().GetProperty(name)?.GetValue(o)?.ToString();
        #endregion

    }
}
