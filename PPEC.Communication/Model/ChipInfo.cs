using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{
    public class ChipInfo
    {
        public int ChipId { get; set; }
        public string ChipName { get; set; }
        public string ChipPath { get; set; }
        public int AddressDec { get; set; }
        public RegisterAddrInfo ChipRegisterInfo { get; set; }
        public List<BitField> BitFields { get; set; } = new List<BitField>();
    }
}
