using Force.DeepCloner;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        }

        private ValueLabelOption _currentSettingCategory;
        public ValueLabelOption CurrentSettingCategory
        {
            get => _currentSettingCategory;
            set
            {
                SetProperty(ref _currentSettingCategory, value);
            }
        }

        private bool _isOrderByName = true;
        public bool IsOrderByName
        {
            get => _isOrderByName;
            set => SetProperty(ref _isOrderByName, value);
        }

        private bool _isOrderByAddress = true;
        public bool IsOrderByAddress
        {
            get => _isOrderByAddress;
            set => SetProperty(ref _isOrderByAddress, value);
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

        private DelegateCommand _checkboxChangeCommand;
        public DelegateCommand CheckboxChangeCommand => _checkboxChangeCommand ?? (_checkboxChangeCommand = new DelegateCommand(() =>
        {
            SearchCategoryTree(TreeKeyword, IsOrderByAddress);
        }));

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

        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                SetProperty(ref _treeKeyword, value);
                SearchCategoryTree(value, IsOrderByAddress);
            }
        }

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

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

        private async Task SendSequence(Sequence param)
        {
            var currentProject = _projectManager.CurrentProject;
            param.CompletedNum = 0;
            Thread.Sleep(1000);
            foreach (var register in param.Items)
            {
                var calcResult = UtilsFunc.GetWriteCommandByAddress(register.AddressHex, currentProject.CommunicationType, register.DecValue);
                await currentProject.CommService.SendAsync(calcResult.bytes);
                param.CompletedNum += 1;
                Thread.Sleep(TimeSpan.FromMilliseconds(2));
            }
        }

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
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

            var clone = CurrentRegister.DeepClone();
            clone.Id = Guid.NewGuid().ToString("N");
            CurrentSequence.Items.Add(clone);
        }));

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

        public override void LoadData()
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
        }
    }
}
