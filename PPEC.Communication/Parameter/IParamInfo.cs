using PPEC.Communication.Parameter.Enum;

namespace PPEC.Communication.Parameter
{
    public interface IParamInfo
    {
        /// <summary>
        /// 参数的开始地址
        /// </summary>
        ushort StartAddress { get; }
        /// <summary>
        /// 占寄存器数量
        /// </summary>
        ushort NumOfRegisters { get; }
        /// <summary>
        /// 参数单位(精度)
        /// </summary>
        float Unit { get; }
        /// <summary>
        /// 参数单位倒数，Unit * UnitRev = 1
        /// </summary>
        float UnitRev { get; }
        /// <summary>
        /// 参数描述
        /// </summary>
        string Comment { get; }
        /// <summary>
        /// 参数最大值限制
        /// </summary>
        dynamic MaxValue { get; }
        /// <summary>
        /// 参数最小值限制
        /// </summary>
        dynamic MinValue { get; }
        /// <summary>
        /// 大小端
        /// </summary>
        EndianEnum Endian { get; }
        /// <summary>
        /// 移位位数
        /// </summary>
        int? MoveUnitRev { get; }

        RegisterTypeParam RegisterType { get; }
    }
}
