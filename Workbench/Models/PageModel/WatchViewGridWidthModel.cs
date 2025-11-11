using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Workbench.Models.dw;

namespace Workbench.Models.PageModel
{
    public class WatchViewGridWidthModel : BindableBase
    {
        private string _AllTime = "1";
        public string AllTime
        {
            get => _AllTime;
            set => SetProperty(ref _AllTime, value);
        }

        private string _SelectRecordTimeType = "0";
        public string SelectRecordTimeType
        {
            get => _SelectRecordTimeType;
            set => SetProperty(ref _SelectRecordTimeType, value);
        }

        private WatchGroup _SelectTab;
        public WatchGroup SelectTab
        {
            get => _SelectTab;
            set => SetProperty(ref _SelectTab, value);
        }

        public System.Windows.GridLength splitterPositionUp = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionUp
        {
            get => splitterPositionUp;
            set => SetProperty(ref splitterPositionUp, value);
        }

        public System.Windows.GridLength splitterPositionDown = new System.Windows.GridLength(2, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionDown
        {
            get => splitterPositionDown;
            set => SetProperty(ref splitterPositionDown, value);
        }

        public System.Windows.GridLength splitterPositionLeft = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft
        {
            get => splitterPositionLeft;
            set => SetProperty(ref splitterPositionLeft, value);
        }

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1.3, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, value);
        }
    }
}
