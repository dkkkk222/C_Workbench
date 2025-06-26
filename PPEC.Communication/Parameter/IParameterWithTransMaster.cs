using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public interface IParameterWithTransMaster
    {
        T GetValue<T>(IParamInfo info, string AddressName = null);
        void SetValue<T>(T value, IParamInfo info, string AddressName = null);
    }
}
