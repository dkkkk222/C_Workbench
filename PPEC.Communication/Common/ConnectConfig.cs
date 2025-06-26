using PPEC.Communication.Config;
using PPEC.Communication.Enum;

namespace PPEC.Communication.Common
{
    public class ConnectConfig
    {
        public AgreementType ConnectionType { get; set; }
        public SerialPortConfig SerialConfig { get; set; }
        public NetworkConfig NetworkConfig { get; set; }
        public CANCommConfig CANCommConfig { get; set; }

        public ConnectConfig(AgreementType connectionType)
        {
            ConnectionType = connectionType;
        }
    }
}
