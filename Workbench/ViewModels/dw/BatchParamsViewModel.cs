using Force.DeepCloner;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.ViewModels.dw
{
    public class BatchParamsViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;

        public BatchParamsViewModel(IEventAggregator eventAggregator, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            SequenceList = _projectManager.CurrentProject.Sequences;
            TableColumns = InitTableColumns();
            TableColumnsR = InitTableColumnsR();
            InitData();
            InitListen();
        }
        public void InitData()
        {
            try
            {
                SplitterPositionOne = _projectManager.CurrentProject.BathParamGrid.UpGridWidth;
                SplitterPositionTwo = _projectManager.CurrentProject.BathParamGrid.DownGridWidth;
                AllTime = _projectManager.CurrentProject.BathParamGrid.AllTime;
                if (_projectManager.CurrentProject.BathParamGrid.CurrentSequence != null)
                    CurrentSequence = _projectManager.CurrentProject.BathParamGrid.CurrentSequence;
                IsConfigPaneOpen = _projectManager.CurrentProject.BathParamGrid.IsConfigPaneOpen;
                SplitterPositionRight = _projectManager.CurrentProject.BathParamGrid.SplitterPositionRight;
                SplitterPositionLeft = _projectManager.CurrentProject.BathParamGrid.SplitterPositionLeft;
            }
            catch(Exception ex)
            {

            }
            
        }
        public void InitListen()
        {
            _eventAggregator.GetEvent<SaveProjectEvent>().Subscribe(e => {
                e.BathParamGrid.upGridWidth= SplitterPositionOne;
                e.BathParamGrid.DownGridWidth = SplitterPositionTwo;
                e.BathParamGrid.AllTime= AllTime;
                e.BathParamGrid.CurrentSequence = CurrentSequence;
                e.BathParamGrid.IsConfigPaneOpen = IsConfigPaneOpen;
                e.BathParamGrid.SplitterPositionRight = SplitterPositionRight;
                e.BathParamGrid.SplitterPositionLeft = SplitterPositionLeft;
            });
        }
        private bool _isLeftOpen=true;
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
        private bool _isConfigPaneOpen = false;               // 默认展开
        public bool IsConfigPaneOpen
        {
            get => _isConfigPaneOpen;
            set
            {
                SetProperty(ref _isConfigPaneOpen, value);
            }
        }

        #region 
        public System.Windows.GridLength splitterPositionOne = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionOne
        {
            get => splitterPositionOne;
            set
            {
                SetProperty(ref splitterPositionOne, value);
            }
        }

        public System.Windows.GridLength splitterPositionTwo =new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionTwo
        {
            get => splitterPositionTwo;
            set
            {
                SetProperty(ref splitterPositionTwo, value);
            }
        }

        public System.Windows.GridLength splitterPositionLeft = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft
        {
            get => splitterPositionLeft;
            set
            {
                SetProperty(ref splitterPositionLeft, value);
            }
        }

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set
            {
                SetProperty(ref splitterPositionRight, value);
            }
        }
        #endregion

        private ObservableCollection<TableColumn> _tableColumns = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableColumn> TableColumns
        {
            get { return _tableColumns; }
            set { SetProperty(ref _tableColumns, value); }
        }

        private ObservableCollection<TableColumn> _tableColumnsR = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableColumn> TableColumnsR
        {
            get { return _tableColumnsR; }
            set { SetProperty(ref _tableColumnsR, value); }
        }

        private ObservableCollection<TableColumn> InitTableColumns()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] {"寄存器地址", "分类", "子分类", "寄存器名称", "数据(HEX)", "数据(binary)" };
            for (int i = 0; i < arr.Length; i++)
            {
                var tab = new TableColumn()
                {
                    Name = arr[i],
                };
                //if (arr[i] == "原始值(Dec)" || arr[i] == "原始值(Bit)")
                //{
                //    tab.IsChecked = false;
                //}
                target.Add(tab);
            }
            return target;
        }
        private ObservableCollection<TableColumn> InitTableColumnsR()
        {
            var target = new ObservableCollection<TableColumn>();
            string[] arr = new string[] { "位", "名称", "数据(HEX)", "配置" };
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
        public DelegateCommand ToggleDrawerCommand => new DelegateCommand(() => IsLeftOpen = !IsLeftOpen);
        private ValueLabelOption _currentSettingCategory;
        public ValueLabelOption CurrentSettingCategory
        {
            get => _currentSettingCategory;
            set
            {
                SetProperty(ref _currentSettingCategory, value);
            }
        }

        private bool _isOrderByCategory = true;
        public bool IsOrderByCategory
        {
            get => _isOrderByCategory;
            set
            {
                if(value)
                {
                    SearchCategoryTree(TreeKeyword, IsOrderByAddress);
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
                    var tempList= _projectManager.GetChipCategoryTreeOnlyW().GetMaxDepthLeaves().ToList().OrderBy(x=>x.Title);
                    SingleParamTrees.Clear();
                    SingleParamTrees.AddRange(tempList);                     
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
                    var tempList = _projectManager.GetChipCategoryTreeOnlyW().GetMaxDepthLeaves()
    .OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue)
    .ToList();
                    SingleParamTrees.Clear();
                    SingleParamTrees.AddRange(tempList);
                }
                SetProperty(ref _isOrderByAddress, value);
            } 
        }

        private ObservableCollection<ValueLabelOption> _settingCategoryList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> SettingCategoryList
        {
            get => _settingCategoryList;
            set => SetProperty(ref _settingCategoryList, value);
        }

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
            set
            {
                if (SetProperty(ref _categoryAddress, value))
                {
                    if (value != null)
                    {
                        CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.AddressDec == (uint)value.Value);

                    }
                }
            }
        }

        private ObservableCollection<Sequence> _sequenceList = new ObservableCollection<Sequence>();
        public ObservableCollection<Sequence> SequenceList
        {
            get => _sequenceList;
            set => SetProperty(ref _sequenceList, value);
        }

        private RegisterAddrInfo _writeCurrentRegister;
        public RegisterAddrInfo WriteCurrentRegister
        {
            get => _writeCurrentRegister;
            set => SetProperty(ref _writeCurrentRegister, value);
        }

        private string _AllTime = "50";
        public string AllTime
        {
            get => _AllTime;
            set => SetProperty(ref _AllTime, value);
        }

        public DelegateCommand SettingAllTimeCommand => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(AllTime))
            {
                HandyControl.Controls.MessageBox.Show("请输入正确的数值!");
                return;
            }
            int setTime = 1;
            if (int.TryParse(AllTime, out setTime))
            {
                if (setTime < 1)
                {
                    HandyControl.Controls.MessageBox.Show("请输入大于等于1的数值!");
                    return;
                }
                foreach (var reg in SequenceList)
                {
                    reg.ParamPushInterval = setTime;
                }
            }
            else
            {
                HandyControl.Controls.MessageBox.Show("请输入正确的数值!");
                return;
            }
        });

        public bool _batchAllCheck;
        public bool BatchAllCheck
        {
            get=> _batchAllCheck; 
            set
            {
                SetProperty(ref _batchAllCheck, value);
            }
        }
        public DelegateCommand<object> SelectAllCommand => new DelegateCommand<object>((e) =>
        {
            SingleParamTrees.SetAllLeavesChecked((bool)e);
        });

        private DelegateCommand _checkboxChangeCommand;
        public DelegateCommand CheckboxChangeCommand => _checkboxChangeCommand ?? (_checkboxChangeCommand = new DelegateCommand(() =>
        {
            SearchCategoryTree(TreeKeyword, IsOrderByAddress);
        }));

        private void SearchCategoryTree(string keyword, bool isOrderByAddress = true)
        {
            SingleParamTrees.Clear();
            var source = _projectManager.GetChipCategoryTreeOnlyW(isOrderByAddress: isOrderByAddress);
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

        private Sequence _currentSequence;
        public Sequence CurrentSequence
        {
            get => _currentSequence;
            set => SetProperty(ref _currentSequence, value);
        }

        private RegisterAddrInfo _currentRegister;
        public RegisterAddrInfo CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
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

        private bool _checkAllResister = false;
        public bool CheckAllResister
        {
            get => _checkAllResister;
            set
            {
                SetProperty(ref _checkAllResister, value);
                foreach(var item in CurrentSequence.Items)
                {
                    item.IsChecked= value;
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
            }
        }

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }
        public void ChangeIsConfigPaneOpen(RegisterAddrInfo param)
        {
            var selected = param;
            bool same = IsSameRegister(selected, WriteCurrentRegister);
            if(same && selected != null)
            {
                //IsConfigPaneOpen = true;
            }
            else
            {
                WriteCurrentRegister = param;
                //IsConfigPaneOpen = true;
            }
        }
        public DelegateCommand<RegisterAddrInfo> ToggleConfigPaneCommand => new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            var selected = param;
            // 与右侧当前显示的是否同一寄存器？
            bool same = IsSameRegister(selected, WriteCurrentRegister);
            if (!same && selected != null)
            {
                // 用你已有的逻辑把“左侧选中项”灌到右侧详情
                // 例如：WriteCurrentRegister = MapToRegisterDetail(selected);
                //      或者调用你原先 ConfigRegisterCommand 里做的那段赋值代码
                //ApplySelectionToConfigPane(selected);

                WriteCurrentRegister = param;
                // 强制展开
                IsConfigPaneOpen = true;
                return;
            }
            IsConfigPaneOpen = !IsConfigPaneOpen;
        });
        private static bool IsSameRegister(RegisterAddrInfo a, RegisterAddrInfo b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;

            // 优先按 AddressHex 比较（你两边通常都有这个字段）
            var aAddr = GetStringProp(a, "AddressHex");
            var bAddr = GetStringProp(b, "AddressHex");
            if (!string.IsNullOrEmpty(aAddr) && !string.IsNullOrEmpty(bAddr))
                return string.Equals(aAddr, bAddr, StringComparison.OrdinalIgnoreCase);

            // 备用：按 Id 比较（如果你的模型有 Id）
            var aId = GetStringProp(a, "Id");
            var bId = GetStringProp(b, "Id");
            if (!string.IsNullOrEmpty(aId) && !string.IsNullOrEmpty(bId))
                return string.Equals(aId, bId, StringComparison.OrdinalIgnoreCase);

            return false;
        }
        private static string GetStringProp(object o, string name)
        => o?.GetType().GetProperty(name)?.GetValue(o)?.ToString();
        private DelegateCommand _closeCommand;


        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        private DelegateCommand _addSequenceCommand;
        public DelegateCommand AddSequenceCommand => _addSequenceCommand ?? (_addSequenceCommand = new DelegateCommand(() =>
        {
            var indexS=SequenceList.Count()+1;
            string nameS = "序列" + indexS;

            SequenceList.Add(new Sequence
            {
                Id = Guid.NewGuid().ToString("N"),
                Name= nameS
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
            if(!_projectManager.CurrentProject.IsConnecting)
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
            var result= MessageBox.Show("是否批量删除序列", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if(result==DialogResult.Yes)
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

        public DelegateCommand BatchDelRegisterCommand => new DelegateCommand(async  () =>
        {
            var result = MessageBox.Show("是否批量删除序列详情", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            var delSeq = CurrentSequence.Items.Where(t => t.IsChecked).ToArray();
            foreach (var item in delSeq)
            {
                CurrentSequence.Items.Remove(item);
            }
            CollectionViewSource.GetDefaultView(CurrentSequence.Items).Refresh();
        });

        private async Task SendSequence(Sequence param)
        {
            var currentProject = _projectManager.CurrentProject;
            param.Progress = 0;
            param.CompletedNum = 0;
            Thread.Sleep(1000);
            foreach (var register in param.Items)
            {
                var calcResult = UtilsFunc.GetWriteCommandByAddress(register.AddressHex, currentProject.CommunicationType, register.DecValue);
                switch (currentProject.CommunicationType)
                {
                    case Constants.OldSERIAL_PORT:
                    case Constants.Modbus:
                        await currentProject.CommService.SendAsync(calcResult.bytes);
                        break;
                    case Constants.I2C:
                        if (ushort.TryParse(register.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                        {
                            await currentProject.CommService.WriteRegisterAsync(reg, register.DecValue);
                        }
                        break;
                    case Constants.CAN:
                        byte[] byteArray = BitConverter.GetBytes(register.DecValue);
                        var frame = new List<byte>();
                        if (ushort.TryParse(register.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg1))
                        {
                            UtilsFunc.WriteUInt32(register.DecValue, EndianMode.BigEndian, frame);
                            await currentProject.CommService.WriteRegisterAsync(reg1, frame.ToArray());
                        }
                        break;
                }
                //await currentProject.CommService.SendAsync(calcResult.bytes);
                param.CompletedNum += 1;
                Thread.Sleep(TimeSpan.FromMilliseconds(param.ParamPushInterval));
            }
        }

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
                param.IsCheck = !param.IsCheck;

            }));

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
            var SelectAddress = SingleParamTrees.GetDeepestCheckedWithCheckedAncestors().ToList();
            //var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            foreach (var item in SelectAddress)
            {
                if (item.AddressHex != null)
                {
                    var register = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == item.Title);
                    var clone = JsonHelper.DeepClone(register);
                    clone.Id = Guid.NewGuid().ToString("N");
                    CurrentSequence.Items.Add(clone);
                }
                
                item.IsCheck = false;
            }
            this.BatchAllCheck = false;
        }));

        private DelegateCommand<BitField> _optionChangeCommand;
        public DelegateCommand<BitField> OptionChangeCommand => _optionChangeCommand ?? (_optionChangeCommand = new DelegateCommand<BitField>(async (param) =>
        {
            var bnr = Utility.HexToBinaryStringLarge(param.SelectedValue, param.Length);
            param.WriteBinary = bnr;
            //UpdateBinaryString(param.Name, param.EndBit, param.StartBit, bnr);
            await UpdateWriteRegister(param.Name, param.EndBit, param.StartBit, bnr);
        }));
        public async Task UpdateWriteRegister(string name, int endBit, int startBit, string replaceStr)
        {
            WriteCurrentRegister.BinaryStr=Utility.ParseDecToBinary(WriteCurrentRegister.DecValue).binaryString;
            var bs = WriteCurrentRegister.BinaryStr;
           
            var str = Utility.ReplaceBitsInString(bs, endBit, startBit, replaceStr);
            var dec = Utility.BinaryToDec(str);
            _projectManager.SetWriteRegisterValue(WriteCurrentRegister, name, dec);
            var newBitStr = WriteCurrentRegister.BinaryStr;
            var newList = await Task.Run(() =>
            {
                var charArr = newBitStr.ToCharArray();
                var length = charArr.Length;

                var list = new List<BitOption>(length);
                for (int i = 0; i < length; i++)
                {
                    list.Add(new BitOption
                    {
                        Value = (uint)Char.GetNumericValue(charArr[i]),
                        Display = (length - 1 - i).ToString()
                    });
                }
                return list;
            });
            WriteCurrentRegister.BinaryList.Clear();
            foreach (var item in newList)
            {
                WriteCurrentRegister.BinaryList.Add(item);
            }
        }
        public DelegateCommand<RegisterAddrInfo> ConfigRegisterCommand => new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            WriteCurrentRegister = param;
        });

        public DelegateCommand<Sequence> RemoveSequenceListCommand => new DelegateCommand<Sequence>((e) =>
        {
            SequenceList.Remove(e);
        });
        public DelegateCommand<Sequence> CopySequenceListCommand => new DelegateCommand<Sequence>((e) =>
        {
            var clone = JsonHelper.DeepClone(e);
            clone.Id = Guid.NewGuid().ToString("N");
            SequenceList.Add(clone);
        });
        public DelegateCommand<Sequence> MoveUpSequenceListCommand => new DelegateCommand<Sequence>((e) =>
        {
            var index = SequenceList.IndexOf(e);
            if (index > 0)
                SequenceList.Move(index, index - 1);
            CollectionViewSource.GetDefaultView(SequenceList).Refresh();
        });
        public DelegateCommand<Sequence> MoveDownSequenceListCommand => new DelegateCommand<Sequence>((e) =>
        {
            int idx = SequenceList.IndexOf(e);
            if (idx < SequenceList.Count - 1)
                SequenceList.Move(idx, idx + 1);
            CollectionViewSource.GetDefaultView(SequenceList).Refresh();
        });

        private DelegateCommand<RegisterAddrInfo> _removeSequenceItemCommand;
        public DelegateCommand<RegisterAddrInfo> RemoveSequenceItemCommand => _removeSequenceItemCommand ?? (_removeSequenceItemCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            CurrentSequence.Items.Remove(param);
        }));

        private DelegateCommand<RegisterAddrInfo> _moveUpCommand;
        public DelegateCommand<RegisterAddrInfo> MoveUpCommand => _moveUpCommand ?? (_moveUpCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            var index = CurrentSequence.Items.IndexOf(param);
            if (index > 0)
                CurrentSequence.Items.Move(index, index - 1);
            CollectionViewSource.GetDefaultView(CurrentSequence.Items).Refresh();
        }));

        private DelegateCommand<RegisterAddrInfo> _moveDownCommand;
        public DelegateCommand<RegisterAddrInfo> MoveDownCommand => _moveDownCommand ?? (_moveDownCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            int idx = CurrentSequence.Items.IndexOf(param);
            if (idx < CurrentSequence.Items.Count - 1)
                CurrentSequence.Items.Move(idx, idx + 1);
            CollectionViewSource.GetDefaultView(CurrentSequence.Items).Refresh();
        }));

        private DelegateCommand<RegisterAddrInfo> _copyCommand;
        public DelegateCommand<RegisterAddrInfo> CopyCommand => _copyCommand ?? (_copyCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            var clone = param.DeepClone();
            clone.Id = Guid.NewGuid().ToString("N");
            CurrentSequence.Items.Add(clone);
        }));
        public  DelegateCommand ClearSettingCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否清空所选寄存器配置", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                foreach (var item in WriteCurrentRegister.BitFields)
                {
                    item.SelectedValue = "";
                    item.WriteHex = "";

                    var binValue = Utility.HexToBinaryStringLarge(item.WriteHex, item.Length);
                    item.WriteBinary = binValue;
                    await UpdateWriteRegister(item.Name, item.EndBit, item.StartBit, binValue);
                }
            }
                
        });

        private void OrderByType(string value, OrderByTypeEnum NameOrAddress)
        {
            var tempList = _projectManager.GetChipCategoryTreeOnlyW().GetMaxDepthLeaves().ToList().OrderBy(x => x.Title);
            if (NameOrAddress == OrderByTypeEnum.Address)
            {
                tempList = _projectManager.GetChipCategoryTreeOnlyW().GetMaxDepthLeaves().ToList().OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue);

            }
            if (!string.IsNullOrEmpty(value))
            {
                tempList = tempList.Where(x => x.AddressHex.Contains(value) || x.Title.Contains(value)).OrderBy(x => x.Title);
            }
            SingleParamTrees.Clear();
            SingleParamTrees.AddRange(tempList);
        }
        public override void LoadData()
        {
            var tree = _projectManager.GetChipCategoryTreeOnlyW();
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
        }
    }
}
