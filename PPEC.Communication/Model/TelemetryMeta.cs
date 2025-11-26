using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Enum;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    public class TelemetryMeta:BindableBase
    {
        public int CommandId { get; set; }

        public string CommandName { get; set; }

        public string CommandCode { get; set; }
        public string Category { get; set; }    // 主分类
        public string SubCategory { get; set; } // 子分类
        public TelemetryCommandType CommandType { get; set; }

        public int CommandLength { get; set; }
    }
    public class TelemetryMonitAnalysisMeta : BindableBase
    {
        public int Id { get;set; }
        public string CodeName { get; set; }
        public string Category { get; set; }    // 主分类
        public string SubCategory { get; set; } // 子分类
        public string DateLocation { get; set; }
        public int StartLocaltion { get; set; }
        public int EndLocaltion { get; set; }
        public int LocaltionLen { get; set; }
        public string AnalyString { get; set;}
        public string BitName { get; set; }
        public int StartBit { get; set; }   
        public int EndBit { get; set; } 
        public int BitLength { get; set; }//位长度 

        public string ShowFormParam { get; set; }
        private ParseResult formParam = new ParseResult();
        public ParseResult FormParam
        {
            get => formParam;
            set
            {
                SetProperty(ref formParam, value);
            }
        }

        public Dictionary<string, string> KeyValuePairs { get; set; }
        public string Unit { get; set; }


    }
}
