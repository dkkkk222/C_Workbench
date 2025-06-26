using PPEC.Communication.Config;

namespace PPEC.Communication.Interface
{
    public interface IFactory
    {
        ILogger Logger { get; }
        int Retries { get; set; }
        IMaster CreateMaster(ITransport transport);
        ITransport CreateTransport(IFactory factory, IStreamResource streamResource);
        ITransport CreateTransportSerialWithEvent(IFactory factory, IStreamResource streamResource);
        ITransport CreateTransportNetworkWithEvent(IFactory factory, IStreamResource streamResource);
        IMaster CreateMasterWithConfig(CommunicationConfig config);
        IMaster CreateMasterEventWithConfig(CommunicationConfig config);

        ITransport CreateTransport(SerialPortConfig config);
        ITransport CreateTransport(NetworkConfig config);

        ITransport CreateTransportSerialWithEvent(SerialPortConfig config);
        ITransport CreateTransportSerialWithEvent(NetworkConfig config);

        ITransport CreateTransportNetworkWithEvent(SerialPortConfig config);
        ITransport CreateTransportNetworkWithEvent(NetworkConfig config);
    }
}
