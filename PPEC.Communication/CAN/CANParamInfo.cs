using PPEC.Communication.Parameter.Enum;
using PPEC.Communication.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.CAN
{
    public class CANParamInfo : IParamInfo
    {
        private readonly ushort _startAddress;
        private readonly ushort _numOfRegisters;
        private readonly float _unit;
        private readonly float _unitRev;
        private readonly int? _moveUnitRev;
        private readonly string _comment;
        private readonly dynamic _minValue = null;
        private readonly dynamic _maxValue = null;
        private readonly dynamic _endian = null;
        private readonly RegisterTypeParam _registerType;
        public CANParamInfo(ushort startAddress, ushort numOfRegisters, float unit = 1.0f, string comment = "")
        {
            _startAddress = startAddress;
            _numOfRegisters = numOfRegisters;
            _unit = unit;
            if (unit == 0.0f)
            {
                throw new ArgumentException("unit should not be 0.0f");
            }
            _unitRev = 1.0f / unit;
            _comment = comment;
            _endian = numOfRegisters == 2 ? EndianEnum.Unsigned : EndianEnum.CDAB;
        }

        public CANParamInfo(ushort startAddress, ushort numOfRegisters, dynamic maxValue, dynamic minValue, EndianEnum? endian = default,
            float unit = 1.0f, string comment = "", int? MoveUnitRev = null, RegisterTypeParam RegisterType = RegisterTypeParam.UINT16) : this(startAddress, numOfRegisters, unit, comment)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _endian = endian;
            _moveUnitRev = MoveUnitRev;
            _registerType = RegisterType;
        }

        public dynamic MaxValue => _maxValue;
        public dynamic MinValue => _minValue;

        public ushort StartAddress => _startAddress;
        public ushort NumOfRegisters => _numOfRegisters;
        public float Unit => _unit;
        public float UnitRev => _unitRev;
        public string Comment => _comment;
        public int? MoveUnitRev => _moveUnitRev;
        public EndianEnum Endian => _endian;


        public RegisterTypeParam RegisterType => _registerType;
    }
}
