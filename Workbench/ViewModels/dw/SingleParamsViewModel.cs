using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Models.dw;

namespace Workbench.ViewModels.dw
{
    public class SingleParamsViewModel : AvaDocument
    {
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

        private ObservableCollection<SingleParamRegisterInfo> _registerInfoList = new ObservableCollection<SingleParamRegisterInfo>()
        {
            new SingleParamRegisterInfo(){ Bit="631", Name="ADC驱动SPI单次传输使能",DataPull="0",DataPullResolve="禁止" },
            new SingleParamRegisterInfo(){ Bit="b28-b26", Name="ADC驱动SPI时钟频率",DataPull="001",DataPullResolve="5Mhz" }
        };
        public ObservableCollection<SingleParamRegisterInfo> RegisterInfoList
        {
            get => _registerInfoList;
            set => SetProperty(ref _registerInfoList, value);
        }

        public override void LoadData()
        {
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
