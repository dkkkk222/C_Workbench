using Newtonsoft.Json;
using PPEC.Communication.CAN;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Model;
using PPEC.Communication.Parameter;
using PPEC.Communication.Parameter.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class TopologyConfig : ITopologyConfig
    {
        public IDictionary<uint, SmlsCanFrame> CANMetaConfig { get; }
        private readonly IDictionary<AddressName, TopoConfigMeta<AddressName>> _metaConfig;
        public IDictionary<AddressName, TopoConfigMeta<AddressName>> MetaConfig => _metaConfig;
        private readonly IDictionary<AddressName, IParamInfo> _paramInfoConfig;

        public TopologyConfig(string configFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(configFile))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string text = reader.ReadToEnd();
                    if (text != null)
                    {
                        _metaConfig = JsonConvert.DeserializeObject<List<TopoConfigMeta<AddressName>>>(text)
                        .ToDictionary(s => s.AddressName, s => s);
                    }
                }
            }
            if (_metaConfig == null)
                throw new Exception($"metaConfig is null in TopologyConfig");

            _paramInfoConfig = _metaConfig.ToDictionary(s => s.Key, s => ToParamInfo(s.Value));
            if (_metaConfig == null)
                throw new Exception($"paramInfoConfig is null in TopologyConfig");
        }

        public IParamInfo GetParamInfo(uint mailBox, AddressName name)
        { return default; }
        private IParamInfo ToParamInfo(TopoConfigMeta<AddressName> meta)
        {
            return new ParamInfo(meta.StartAddress, meta.NumOfRegisters,
                meta.MaxValue, meta.MinValue, null, meta.Unit, meta.Comment, meta.MoveUnitRev);
        }

        public IParamInfo GetParamInfo(AddressName name)
        {
            IParamInfo info;
            if (_paramInfoConfig.TryGetValue(name, out info))
            {
                return info;
            }
            return default;
        }
    }
}
