using System.Collections.Generic;
using System.Collections.ObjectModel;
using Workbench.Models.Pages;
using Workbench.ViewModels.Content.Develop;
using Prism.Ioc;
using Workbench.Utils;
using Workbench.SerialAsistant.Utils;
using System;
using Prism.Events;
using Workbench.Events;
using System.Reflection;
using PPEC.Communication.Model;
using PPEC.Communication.Enum;
using System.Linq;
using Workbench.Models.Data;

namespace Workbench.ViewModels.Pages
{
    public class ParamSettingViewModel : DevelopBaseViewModel
    {
        private readonly IContainerProvider _container;
        public ParamSettingViewModel()
        {
            _container = ContainerLocator.Container;
            LoadMenus();
            LoadElements();
            HandleData();
            EventWatchDog();
        }

        private void EventWatchDog()
        {
            var eventAggregator = _container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<SerialPortAddRemoveEvent>().Subscribe(() =>
            {
                var portName = SelectedPort;
                PortList.Clear();
                PortList.AddRange(SerialPortHelper.GetPortNames());
                if (!string.IsNullOrEmpty(portName) && PortList.Contains(portName))
                    SelectedPort = portName;
                else
                    SelectedPort = PortList[0];

            }, ThreadOption.UIThread);
        }

        private void HandleData()
        {
            PortList.AddRange(SerialPortHelper.GetPortNames());
            SelectedPort = PortList[0];
        }

        private void LoadElements()
        {
            var projectManager = _container.Resolve<ProjectManager>();
            var currentPpec = projectManager.GetCachePPEC();
            var ppecId = currentPpec.Label.Replace("-", "");
            var fileHandler = _container.Resolve<FileHandler>();
            var list = fileHandler.ReadResourceConfig<ParamSettingGroup>($"Workbench.Data.ParamSetting.{ppecId}.json");
            var assembly = Assembly.Load("PPEC.Communication");
            var config = fileHandler.ReadResourceConfig<TopoMeta>($"PPEC.Communication.Resources.Configs.{ppecId}.json", assembly);
            var dic = config.ToDictionary(t => t.AddressName, t => t);
            list.ForEach(t =>
            {
                foreach (var element in t.Elements)
                {
                    if (element.AddressName.HasValue)
                    {
                        if (dic.ContainsKey(element.AddressName.Value))
                        {
                            element.TopoConfigMeta = dic[element.AddressName.Value];
                            //设置默认值
                            if (element.TopoConfigMeta.DefaultValue != null)
                                element.Value = element.TopoConfigMeta.DefaultValue;
                        }
                    }
                }
            });
            ParamSettingElements.AddRange(list);
        }

        private void LoadMenus()
        {
            Menus.Add(new TreeVeiwModel
            {
                Name = "菜单",
                Children = new List<TreeVeiwModel>
                {
                    new TreeVeiwModel{ Name = "控制设置", IsSelected = true},
                    new TreeVeiwModel{ Name = "采样设置"},
                    new TreeVeiwModel{ Name = "保护设置"},
                    new TreeVeiwModel{ Name = "启动设置"},
                    new TreeVeiwModel{ Name = "通讯设置"},
                }
            });
        }

        #region Properties

        private string _selectedPort;

        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        private ObservableCollection<string> _portList = new ObservableCollection<string>();

        public ObservableCollection<string> PortList
        {
            get => _portList;
            set => SetProperty(ref _portList, value);
        }

        private ObservableCollection<ParamSettingGroup> _paramSettingElements = new ObservableCollection<ParamSettingGroup>();

        public ObservableCollection<ParamSettingGroup> ParamSettingElements
        {
            get => _paramSettingElements;
            set => SetProperty(ref _paramSettingElements, value);
        }

        private ObservableCollection<TreeVeiwModel> _menus = new ObservableCollection<TreeVeiwModel>();

        public ObservableCollection<TreeVeiwModel> Menus
        {
            get => _menus;
            set => SetProperty(ref _menus, value);
        }

        #endregion
    }
}
