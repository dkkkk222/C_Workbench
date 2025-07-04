using Newtonsoft.Json;
using PPEC.Communication;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.ViewModels.dw
{
    public class SingleParamsViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;

        public SingleParamsViewModel(ProjectManager projectManager, IEventAggregator eventAggregator)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            ReadWriteHistory = _projectManager.CurrentProject.ReadWriteHistory;
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

        private RegisterAddrInfo _currentRegister;
        public RegisterAddrInfo CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
        }

        private void SearchCategoryTree(string keyword)
        {
            SingleParamTrees.Clear();
            var source = _projectManager.GetChipCategoryTree();
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

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

        private ObservableCollection<SingleParamHistory> _readWriteHistory = new ObservableCollection<SingleParamHistory>();

        public ObservableCollection<SingleParamHistory> ReadWriteHistory
        {
            get => _readWriteHistory;
            set => SetProperty(ref _readWriteHistory, value);
        }

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
            }));

        private DelegateCommand _readRegisterCommand;
        public DelegateCommand ReadRegisterCommand => _readRegisterCommand ?? (_readRegisterCommand = new DelegateCommand(() =>
        {
            if (CurrentRegister == null)
                return;
            var history = new SingleParamHistory
            {
                ReadWrite = "R",
                Address = CurrentRegister.AddressHex,
                Hex = CurrentRegister.HexValue,
                Binary = ""
            };
            _projectManager.CurrentProject.ReadWriteHistory.Add(history);
        }));

        private DelegateCommand _sendRegisterCommand;
        public DelegateCommand SendRegisterCommand => _sendRegisterCommand ?? (_sendRegisterCommand = new DelegateCommand(() =>
        {
            if (CurrentRegister == null)
                return;
            var history = new SingleParamHistory
            {
                ReadWrite = "W",
                Address = CurrentRegister.AddressHex,
                Hex = CurrentRegister.HexValue,
                Binary = ""
            };
            _projectManager.CurrentProject.ReadWriteHistory.Add(history);
        }));

        private DelegateCommand _closeCommand;

        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        public override void LoadData()
        {
            var tree = _projectManager.GetChipCategoryTree();
            SingleParamTrees.AddRange(tree);
            CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo)
                .FirstOrDefault(t => t.Name == tree[0].Children[0].Children[0].Title);

        }
    }
}
