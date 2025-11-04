using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class OptionModel : BindableBase
    {
        private ushort _value;
        public ushort Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }
        public float _ShowData;
        public float ShowData
        {
            get => _ShowData;
            set => SetProperty(ref _ShowData, value);
        }
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }
        private int _IsAnalog;
        public int IsAnalog
        {
            get => _IsAnalog;
            set => SetProperty(ref _IsAnalog, value);
        }
        private string _ButtonImageContent;
        public string ButtonImageContent
        {
            get => _ButtonImageContent;
            set => SetProperty(ref _ButtonImageContent, value);
        }

        private ObservableCollection<string> _relatedComponents = new ObservableCollection<string>();
        public ObservableCollection<string> RelatedComponents
        {
            get => _relatedComponents;
            set => SetProperty(ref _relatedComponents, value);
        }
    }
}
