using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using PPEC.Communication.Parameter.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.CAN
{
    public class SmlsCanData
    {
        public string CanDataName { get; set; }
        public ushort StartIndex { get; set; }
        public ushort NumOfData { get; set; }
        public string Comment { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RegisterTypeParam RegisterType { get; set; } = RegisterTypeParam.BYTE;

        [DefaultValue(1.0f)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public float Unit { get; set; }

        public dynamic MaxValue { get; set; }
        public dynamic MinValue { get; set; }
        public dynamic DefaultValue { get; set; }
        public int? MoveUnitRev { get; set; }
        public EndianEnum Endian { get; set; }
        public string IsDown { get; set; }

        public string ShowSuffix { get; set; }//单位
        public int? Precision { get; set; }//精度
    }
}
