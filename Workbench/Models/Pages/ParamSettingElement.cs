using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models.Data;

namespace Workbench.Models.Pages
{
    public class ParamSettingElement : BindableBase
    {

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        public string Type { get; set; }
        public string Unit { get; set; }
        public string Name { get; set; }
        public bool IsControl { get; set; }
        public AddressName? AddressName { get; set; }
        public TopoMeta TopoConfigMeta { get; set; }
        private string _value;
        public string Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }
        public List<ValueName> Options { get; set; } = new List<ValueName>();
        public List<ControlConfig> IsControled { get; set; } = new List<ControlConfig>();


    }
}
