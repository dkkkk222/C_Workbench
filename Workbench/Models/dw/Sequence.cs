using Newtonsoft.Json;
using PPEC.Communication.Model;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using Workbench.Models.Consts;

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

        private int _paramPushInterval = 5;
        /// <summary>
        /// 参数下发间隔
        /// </summary>
        public int ParamPushInterval
        {
            get => _paramPushInterval;
            set
            {
                if (value < 5)
                    value = 5;
                SetProperty(ref _paramPushInterval, value);
            }
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

        private int _completedNum = 0;
        public int CompletedNum
        {
            get => _completedNum;
            set
            {
                SetProperty(ref _completedNum, value);
                Progress = (int)((double)_completedNum / _items.Count * 100);
            }
        }

        private int _progress = 0;
        public int Progress
        {
            get => _progress;
            set
            {
                SetProperty(ref _progress, value);
                Statement = value == 100 ? SequenceStatement.Completed : value == 0 ? SequenceStatement.None : SequenceStatement.Doing;
            }
        }


        private string _statement = SequenceStatement.None;
        public string Statement
        {
            get => _statement;
            set => SetProperty(ref _statement, value);
        }
    }
}
