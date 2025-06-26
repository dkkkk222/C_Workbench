using PPEC.Communication.Parameter.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public class ParamInfo : IParamInfo
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
        public RegisterTypeParam RegisterType => _registerType;

        public ParamInfo(ushort startAddress, ushort numOfRegisters, float unit = 1.0f, string comment = "")
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

            _endian = numOfRegisters == 1 ? EndianEnum.Unsigned : EndianEnum.CDAB;
        }

        public ParamInfo(ushort startAddress, ushort numOfRegisters, dynamic maxValue, dynamic minValue, EndianEnum? endian = default,
            float unit = 1.0f, string comment = "", int? MoveUnitRev = null) : this(startAddress, numOfRegisters, unit, comment)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _endian = endian ?? EndianEnum.Unsigned;
            _moveUnitRev = MoveUnitRev;
        }

        public dynamic MaxValue => _maxValue;
        public dynamic MinValue => _minValue;

        public ushort StartAddress => _startAddress;
        public ushort NumOfRegisters => _numOfRegisters;
        public float Unit => _unit;
        public float UnitRev => _unitRev;
        public int? MoveUnitRev => _moveUnitRev;
        public string Comment => _comment;

        public EndianEnum Endian => _endian;
    }
}
