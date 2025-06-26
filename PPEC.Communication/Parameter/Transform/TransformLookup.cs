using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter.Transform
{
    public class TransformLookup : ITransformLookup
    {
        private readonly IContainerProvider _context;

        public TransformLookup(IContainerProvider context)
        {
            _context = context;
        }

        public ITransform<T> GetTransform<T>()
        {
            return _context.Resolve<ITransform<T>>();
        }
    }
}
