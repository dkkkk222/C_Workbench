using Force.DeepCloner;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                SetProperty(ref _currentSettingCategory, value);
                GetAddressList(value.Value.ToString());
            }
        }

        private void GetAddressList(string category)
        {
            AddressList.Clear();
            var list = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).Where(t => t.Category == category).ToList();
            if (list.Any())
            {
                var options = list.Select(t => new ValueLabelOption { Value = t.AddressHex, Label = $"{t.AddressHex} : {t.Name}" });
                AddressList.AddRange(options);
            }
        }

        private ObservableCollection<ValueLabelOption> _addressList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> AddressList
        {
            get => _addressList;
            set => SetProperty(ref _addressList, value);
        }

        private ValueLabelOption _currentAddress;
        public ValueLabelOption CurrentAddress
        {
            get => _currentAddress;
            set => SetProperty(ref _currentAddress, value);
        }
        private ObservableCollection<CategoryTree> _batchParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> BatchParamTrees
        {
            get => _batchParamTrees;
            set => SetProperty(ref _batchParamTrees, value);
        }

        private ObservableCollection<Sequence> _sequenceList = new ObservableCollection<Sequence>();
        public ObservableCollection<Sequence> SequenceList
        {
            get => _sequenceList;
            set => SetProperty(ref _sequenceList, value);
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
                SearchCategoryTree(value);
            }
        }

        private void SearchCategoryTree(string keyword)
        {
            BatchParamTrees.Clear();
            var source = _projectManager.GetChipCategoryTree(CurrentSettingCategory.Value.ToString(), CurrentAddress?.Value.ToString());
            if (string.IsNullOrEmpty(keyword))
            {
                BatchParamTrees.AddRange(source);
            }
            else
            {
                var searcher = new TreeSearcher();
                var filteredResult = searcher.SearchInForest(source, keyword);
                BatchParamTrees.AddRange(filteredResult);
            }
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
            SequenceList.Add(new Sequence
            {
                Id = Guid.NewGuid().ToString("N"),
            });
        }));

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
            {
                CurrentSequence.Items.Remove(param);
                CurrentSequence.Items.Insert(index - 1, param);
            }
        }));

        private DelegateCommand<RegisterAddrInfo> _moveDownCommand;
        public DelegateCommand<RegisterAddrInfo> MoveDownCommand => _moveDownCommand ?? (_moveDownCommand = new DelegateCommand<RegisterAddrInfo>((param) =>
        {
            var index = CurrentSequence.Items.IndexOf(param);
            if (index < CurrentSequence.Items.Count - 1)
            {
                CurrentSequence.Items.Remove(param);
                CurrentSequence.Items.Insert(index + 1, param);
            }
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
            InitData();
        }

        private void InitData()
        {
            var categoryOptions = _projectManager.GetCategories().Select(t => new ValueLabelOption() { Value = t, Label = t });
            SettingCategoryList.Clear();
            SettingCategoryList.AddRange(categoryOptions);
            CurrentSettingCategory = SettingCategoryList.FirstOrDefault();

            //InitCategoryTree();
        }

        public void InitCategoryTree()
        {
            BatchParamTrees.Clear();
            var trees = _projectManager.GetChipCategoryTree(CurrentSettingCategory.Value.ToString(), CurrentAddress?.Value.ToString());
            BatchParamTrees.AddRange(trees);
        }
    }
}
