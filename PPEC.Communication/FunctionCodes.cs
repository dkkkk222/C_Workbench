using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public static class FunctionCodes
    {
        public const byte ReadCoils = 1;

        public const byte ReadInputs = 2;

        public const byte ReadHoldingRegisters = 3;

        public const byte ReadInputRegisters = 4;

        public const byte WriteSingleCoil = 5;

        public const byte WriteSingleRegister = 6;

        public const byte PPECWriteMultipleRegister = 10;

        public const byte Diagnostics = 8;

        public const ushort DiagnosticsReturnQueryData = 0;

        public const byte WriteMultipleCoils = 15;

        public const byte WriteMultipleRegisters = 87;
        public const byte WriteMultipleRegistersPPEC = 16;
        public const byte WriteMultipleRegistersModbus = 16;

        public const byte PPECCommodReadOrWrite = 0;

        public const byte WriteFileRecord = 21;

        public const byte ReadWriteMultipleRegisters = 23;

        public const byte PPECShakeHandRequest = 0;
        public const byte PPECFlashEraseRequest = 1;
        public const byte PPECSendDataRequest = 2;
        public const byte PPECFlashEfficacyRequest = 3;
        public const byte PPEUpgradeResultRequest = 4;
    }
}
