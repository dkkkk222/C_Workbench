using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class TelemetryTag : BindableBase
    {
        public string Id { get; set; }
        public string chipId { get; set; }
        public string Name { get; set; }
    }
}
