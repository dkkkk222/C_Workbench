using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Workbench.Models.PageModel
{
    public class WatchViewGridWidthModel : BindableBase
    {
        public System.Windows.GridLength splitterPositionUp = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionUp
        {
            get => splitterPositionUp;
            set => SetProperty(ref splitterPositionUp, value);
        }

        public System.Windows.GridLength splitterPositionDown = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
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

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, value);
        }
    }
}
