using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;

namespace Workbench.ViewModels.dw
{
    public class BatchParamsViewModel : AvaDocument
    {
        private readonly IEventAggregator _eventAggregator;

        public BatchParamsViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

        }

        private ObservableCollection<SettingCategory> _settingCategoryList = new ObservableCollection<SettingCategory>()
        {
            new SettingCategory{Label="ADC工作状态"}
        };
        public ObservableCollection<SettingCategory> SettingCategoryList
        {
            get => _settingCategoryList;
            set => SetProperty(ref _settingCategoryList, value);
        }

        private SettingCategory _currentSettingCategory;
        public SettingCategory CurrentSettingCategory
        {
            get => _currentSettingCategory;
            set => SetProperty(ref _currentSettingCategory, value);
        }

        private ObservableCollection<SingleParamTree> _singleParamTrees = new ObservableCollection<SingleParamTree>();
        public ObservableCollection<SingleParamTree> SingleParamTrees
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

        private DelegateCommand _closeCommand;

        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        public override void LoadData()
        {
            CurrentSettingCategory = SettingCategoryList.FirstOrDefault();

            SingleParamTrees.Add(new SingleParamTree()
            {
                Title = "基本保护设置",
                Children = new List<SingleParamTree>()
                {
                    new SingleParamTree()
                    {
                        Title="功率控制1",
                        Children=new List<SingleParamTree>()
                        {
                            new SingleParamTree(){Title="pwr1protect set1"},
                            new SingleParamTree(){Title="pwr1protect set2"},
                            new SingleParamTree(){Title="pwr1protect set3"}
                        }
                    },
                    new SingleParamTree()
                    {
                        Title="功率控制2",
                        Children=new List<SingleParamTree>()
                        {
                            new SingleParamTree(){Title="pwr1protect set1"},
                            new SingleParamTree(){Title="pwr1protect set2"},
                            new SingleParamTree(){Title="pwr1protect set3"}
                        }
                    }
                }
            });
            SingleParamTrees.Add(new SingleParamTree()
            {
                Title = "ADC设置",
                Children = new List<SingleParamTree>()
                {
                    new SingleParamTree()
                    {
                        Title="功率控制1",
                        Children=new List<SingleParamTree>()
                        {
                            new SingleParamTree(){Title="pwr1protect set1"},
                            new SingleParamTree(){Title="pwr1protect set2"},
                            new SingleParamTree(){Title="pwr1protect set3"}
                        }
                    },
                    new SingleParamTree()
                    {
                        Title="功率控制2",
                        Children=new List<SingleParamTree>()
                        {
                            new SingleParamTree(){Title="pwr1protect set1"},
                            new SingleParamTree(){Title="pwr1protect set2"},
                            new SingleParamTree(){Title="pwr1protect set3"}
                        }
                    }
                }
            });
        }
    }
}
