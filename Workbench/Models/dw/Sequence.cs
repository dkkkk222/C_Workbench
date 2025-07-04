using PPEC.Communication.Model;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace Workbench.Models.dw
{
    public class Sequence : BindableBase
    {
        private string _id;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _paramPushNum = 0;
        /// <summary>
        /// 参数下发数量
        /// </summary>
        public int ParamPushNum
        {
            get => _paramPushNum;
            set => SetProperty(ref _paramPushNum, value);
        }

        private int _paramPushInterval = 1;
        /// <summary>
        /// 参数下发间隔
        /// </summary>
        public int ParamPushInterval
        {
            get => _paramPushInterval;
            set => SetProperty(ref _paramPushInterval, value);
        }

        private string _paramPushState;
        /// <summary>
        /// 下发状态
        /// </summary>
        public string ParamPushState
        {
            get => _paramPushState;
            set => SetProperty(ref _paramPushState, value);
        }

        private bool _isChecked = false;
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        private ObservableCollection<RegisterAddrInfo> _items = new ObservableCollection<RegisterAddrInfo>();
        /// <summary>
        /// 详情
        /// </summary>
        public ObservableCollection<RegisterAddrInfo> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

    }
}
