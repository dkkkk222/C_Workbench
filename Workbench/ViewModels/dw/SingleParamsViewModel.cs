using PPEC.Communication.Model;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.ViewModels.dw
{
    public class SingleParamsViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        public SingleParamsViewModel(ProjectManager projectManager)
        {
            _projectManager = projectManager;
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

        private ObservableCollection<SingleParamTree> _singleParamTrees = new ObservableCollection<SingleParamTree>();
        public ObservableCollection<SingleParamTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

        private ObservableCollection<SingleParamHistory> _historyData = new ObservableCollection<SingleParamHistory>()
        {
            new SingleParamHistory(){ ReadWrite="R", Address="0000" },
            new SingleParamHistory(){ ReadWrite="W", Address="000C" }
        };
        public ObservableCollection<SingleParamHistory> HistoryData
        {
            get => _historyData;
            set => SetProperty(ref _historyData, value);
        }

        private DelegateCommand<SingleParamTree> _selectedItemChangedCommand;
        public DelegateCommand<SingleParamTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<SingleParamTree>((param) =>
            {
                if (param == null || param.Type != SingleParamTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
            }));

        public override void LoadData()
        {
            var tree = _projectManager.GetChipCategoryTree();
            SingleParamTrees.AddRange(tree);
        }
    }
}
