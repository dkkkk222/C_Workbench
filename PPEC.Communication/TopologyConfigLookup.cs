using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using Prism.Ioc;

namespace PPEC.Communication
{
    public class TopologyConfigLookup : ITopologyConfigLookup
    {
        private readonly IContainerProvider _context;

        public TopologyConfigLookup(IContainerProvider context)
        {
            _context = context;
        }

        public ITopologyConfig GetConfig(TopologyId id)
        {
            return _context.Resolve<ITopologyConfig>(id.ToString());
        }
    }
}
