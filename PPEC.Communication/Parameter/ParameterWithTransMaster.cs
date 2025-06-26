using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public class ParameterWithTransMaster : ParameterMaster, IParameterWithTransMaster
    {
        private readonly ITransformLookup _transformLookup;

        public ParameterWithTransMaster(IDefaultDataSource registers, ITransformLookup transLookup) : base(registers)
        {
            _transformLookup = transLookup;
        }
        public T GetValue<T>(IParamInfo info, string AddressName = null)
        {
            var trans = _transformLookup.GetTransform<T>();
            return GetValue(info, trans);
        }

        public void SetValue<T>(T value, IParamInfo info, string AddressName = null)
        {
            var trans = _transformLookup.GetTransform<T>();
            SetValue(value, info, trans);
        }
    }
}
