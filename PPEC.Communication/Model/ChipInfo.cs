using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public class ChipInfo
    {
        public string ChipId { get; set; }
        public string ChipName { get; set; }
        public string ChipPath { get; set; }
        public List<RegisterMeta> ChipRegisterInfo { get; set; }
    }

}
