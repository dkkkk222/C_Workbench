using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Enum
{
    public enum UartDataType : ushort
    {
        ResetCommand = 0x0000,   // 复位
        ReadRegisterCommand = 0x000A,   // 寄存器读
        WriteRegisterCommand = 0x000F,   // 寄存器写
        ReadRegisterResponse = 0x0014,    // 从机寄存器应答
        InvRealtime= 0x0001,
    }
    public enum ConnectPortType
    {
        UART,
        CAN,
        I2C,
        TCP,
        INV
    }
}
