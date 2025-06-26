using Prism.Mvvm;

namespace Workbench.Controls.Models
{
    public class PPEC_Pin : BindableBase
    {
        private int _index;
        public int Index
        {
            get { return _index; }
            set { SetProperty(ref _index, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private bool _isChecked = false;
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(ref _isChecked, value); }
        }

        private string _backgroundColor = "#00cc44";
        public string BackgroundColor
        {
            get { return _backgroundColor; }
            set { SetProperty(ref _backgroundColor, value); }
        }

        private string _foregroundColor = "#000000";
        public string ForegroundColor
        {
            get { return _foregroundColor; }
            set { SetProperty(ref _foregroundColor, value); }
        }

        private string _checkedBackgroundColor = "#ffd300";
        public string CheckedBackgroundColor
        {
            get { return _checkedBackgroundColor; }
            set { SetProperty(ref _checkedBackgroundColor, value); }
        }

        private string _checkedForgroundColor = "#000000";
        public string CheckedForgroundColor
        {
            get { return _checkedForgroundColor; }
            set { SetProperty(ref _checkedForgroundColor, value); }
        }

    }
}
