using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Workbench.Models.PageModel
{
    public class SingleParamGridWidthModel : BindableBase
    {
        public System.Windows.GridLength splitterPositionOne = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionOne
        {
            get => splitterPositionOne;
            set => SetProperty(ref splitterPositionOne, value);
        }

        public System.Windows.GridLength splitterPositionTwo = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionTwo
        {
            get => splitterPositionTwo;
            set => SetProperty(ref splitterPositionTwo, value);
        }

        public System.Windows.GridLength splitterPositionThree = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionThree
        {
            get => splitterPositionThree;
            set => SetProperty(ref splitterPositionThree, value);
        }
    }
}
