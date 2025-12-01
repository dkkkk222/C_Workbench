using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Model;
using Prism.Mvvm;

namespace Workbench.Models.PageModel
{
    public class TelemetryMonitGridWidthModel : BindableBase
    {
        private string _ProjectTag = "FF";
        public string ProjectTag
        {
            get => _ProjectTag;
            set => SetProperty(ref _ProjectTag, value);
        }

        private OptionModel _SelectedCycle;
        /// <summary>
        /// 选择的档位
        /// </summary>
        public OptionModel SelectedCycle
        {
            get => _SelectedCycle;
            set
            {
                if (SetProperty(ref _SelectedCycle, value))
                { 
                }
            }
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

        public System.Windows.GridLength splitterPositionRight2 = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight2
        {
            get => splitterPositionRight2;
            set => SetProperty(ref splitterPositionRight2, value);
        }
    }
}
