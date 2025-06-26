using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;

namespace PPEC.Communication.Model
{
    public class TopoConfigMeta<Tenum>
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Tenum AddressName { get; set; }
        public ushort StartAddress { get; set; }
        public ushort NumOfRegisters { get; set; }
        [DefaultValue(1.0f)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public float Unit { get; set; }
        public int? MoveUnitRev { get; set; }
        public string Comment { get; set; }
        public dynamic MaxValue { get; set; }
        public dynamic MinValue { get; set; }
        public dynamic DefaultValue { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(RegisterLimit.ALL)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public RegisterLimit Limit { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public RegisterType RegisterType { get; set; }
        public string IsDown { get; set; }
        public string ShowSuffix { get; set; }//单位
        public int? Precision { get; set; }//精度
    }
}
