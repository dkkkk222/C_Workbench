using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Enum
{
    public enum AgreementType
    {
        Serial,
        Tcp,
        CAN
    }
    public enum EndianMode
    {
        BigEndian,       // ABCD
        LittleEndian,    // DCBA
        WordSwap,        // CDAB (32位把两个16位词对调)
        ByteSwapInWord   // BADC (16位内的字节互换)
    }
}
