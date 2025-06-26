using PPEC.Communication.CAN;
using PPEC.Communication.Model;
using PPEC.Communication.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    public interface ITopologyConfigBase<Tenum>
    {
        IParamInfo GetParamInfo(Tenum name);
        IParamInfo GetParamInfo(uint mailBox, Tenum name);
        IDictionary<Tenum, TopoConfigMeta<Tenum>> MetaConfig { get; }
        IDictionary<uint, SmlsCanFrame> CANMetaConfig { get; }
    }
}
