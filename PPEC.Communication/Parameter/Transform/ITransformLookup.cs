using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public interface ITransformLookup
    {
        ITransform<T> GetTransform<T>();
    }
}
