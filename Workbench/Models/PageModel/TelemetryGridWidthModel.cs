using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Workbench.Models.dw;

namespace Workbench.Models.PageModel
{
    public class TelemetryGridWidthModel : BindableBase
    {
        private string _AllTime = "50";
        public string AllTime
        {
            get => _AllTime;
            set => SetProperty(ref _AllTime, value);
        }

        public System.Windows.GridLength upGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength UpGridWidth
        {
            get => upGridWidth;
            set => SetProperty(ref upGridWidth, value);
        }

        public System.Windows.GridLength downGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength DownGridWidth
        {
            get => downGridWidth;
            set => SetProperty(ref downGridWidth, value);
        }

        public System.Windows.GridLength threeGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ThreeGridWidth
        {
            get => threeGridWidth;
            set => SetProperty(ref threeGridWidth, value);
        }

        public System.Windows.GridLength splitterPositionLeft = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft
        {
            get => splitterPositionLeft;
            set => SetProperty(ref splitterPositionLeft, value);
        }

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, value);
        }
    }
}
