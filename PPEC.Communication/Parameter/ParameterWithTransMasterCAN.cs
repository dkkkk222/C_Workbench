using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public class ParameterWithTransMasterCAN : ParameterMasterCAN, IParameterWithTransMaster
    {
        private readonly ITransformLookup _transformLookup;

        public ParameterWithTransMasterCAN(IDictionary<string, CANModelParam> registers, ITransformLookup transLookup)
            : base(registers)
        {
            _transformLookup = transLookup;
        }

        public T GetValue<T>(IParamInfo info, string AddressName = null)
        {
            var trans = _transformLookup.GetTransform<T>();
            return GetValue(info, trans, AddressName);
        }
        public void SetValue<T>(T value, IParamInfo info, string AddressName = null)
        {
            var trans = _transformLookup.GetTransform<T>();
            SetValue(value, info, trans, AddressName);
        }
    }
}
