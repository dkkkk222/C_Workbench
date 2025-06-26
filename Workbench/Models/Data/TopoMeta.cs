using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using PPEC.Communication.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.Data
{
    public class TopoMeta
    {
        public AddressName AddressName { get; set; }
        public ushort StartAddress { get; set; }
        public ushort NumOfRegisters { get; set; }
        public float Unit { get; set; }
        public int? MoveUnitRev { get; set; }
        public string Comment { get; set; }
        public dynamic MaxValue { get; set; }
        public dynamic MinValue { get; set; }
        public string DefaultValue { get; set; }
        public RegisterLimit Limit { get; set; }
        public RegisterType RegisterType { get; set; }
        public string IsDown { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string ShowSuffix { get; set; }

        /// <summary>
        /// 精度
        /// </summary>
        public int Precision { get; set; } = 0;
    }
}
