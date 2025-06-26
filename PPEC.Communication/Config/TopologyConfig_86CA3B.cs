using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Config
{
    public class TopologyConfig_86CA3B : TopologyConfig, ITopologyConfig
    {
        public TopologyConfig_86CA3B(string configFile = @"PPEC.Communication.Resources.Configs.PPEC86CA3B.json") : base(configFile)
        {
        }
    }
}
