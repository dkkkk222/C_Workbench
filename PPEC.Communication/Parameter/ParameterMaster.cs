using PPEC.Communication.Interfaces;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public class ParameterMaster : IParameterMaster
    {
        public ParameterMaster(IDefaultDataSource registers)
        {
            _registers = registers;
        }
        private readonly IDefaultDataSource _registers;

        public IDefaultDataSource Registers => _registers;

        private readonly IDictionary<string, CANModelParam> _dicCache;

        public IDictionary<string, CANModelParam> DicCache => _dicCache;

        public T GetValue<T>(IParamInfo info, ITransform<T> transform, string AddressName = null)
        {
            if (info == null || transform == null)
            {
                return default;
            }
            var regs = Registers.ReadPoints(info.StartAddress, info.NumOfRegisters);
            return transform.GetValue(regs, info);
        }

        public void SetValue<T>(T value, IParamInfo info, ITransform<T> transform, string AddressName = null)
        {
            if (value == null || info == null || transform == null) { return; }
            var regs = transform.GetRegisters(value, info);
            Registers.WritePoints(info.StartAddress, regs);
        }
    }
}
