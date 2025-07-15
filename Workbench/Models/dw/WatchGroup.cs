using Newtonsoft.Json;
using PPEC.Communication.Model;
using Prism.Mvvm;
using ScottPlot.WPF;
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

        private string _tableName = "状态监测表";
        public string TableName
        {
            get { return _tableName; }
            set { SetProperty(ref _tableName, value); }
        }

        private string _chartName = "状态监测图";
        public string ChartName
        {
            get { return _chartName; }
            set { SetProperty(ref _chartName, value); }
        }

        private ObservableCollection<BitField> _bitFields = new ObservableCollection<BitField>();
        public ObservableCollection<BitField> BitFields
        {
            get { return _bitFields; }
            set { SetProperty(ref _bitFields, value); }
        }

        private ObservableCollection<TableColumn> _tableColumns = new ObservableCollection<TableColumn>();
        public ObservableCollection<TableColumn> TableColumns
        {
            get { return _tableColumns; }
            set { SetProperty(ref _tableColumns, value); }
        }

        [JsonIgnore]
        public WpfPlot PlotControl { get; } = new WpfPlot();

    }
}
