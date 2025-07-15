using PPEC.Communication.Model;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace Workbench.Models.dw
{
    public class WatchGroup : BindableBase
    {
        private string _id;
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _header;
        public string Header
        {
            get { return _header; }
            set { SetProperty(ref _header, value); }
        }

        private ObservableCollection<BitField> _bitFields = new ObservableCollection<BitField>();
        public ObservableCollection<BitField> BitFields
        {
            get { return _bitFields; }
            set { SetProperty(ref _bitFields, value); }
        }
    }
}
