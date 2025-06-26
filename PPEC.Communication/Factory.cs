using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class Factory : IFactory
    {
        #region Base
        /// <summary>
        /// Create a factory which uses the built in standard slave function handlers.
        /// </summary>
        public Factory(ILogger logger = null)
        {
            Logger = logger ?? NullLogger.Instance;
        }

        public ILogger Logger { get; }
        public int Retries { get; set; } = 3;
        #endregion

        #region CreateTransport
        public ITransport CreateTransport(IFactory factory, IStreamResource serialPort)
        {
            ITransport transport = new TransportModbus(serialPort, factory, Logger);
            return transport;
        }
        public ITransport CreateTransportSerialWithEvent(IFactory factory, IStreamResource serialPort)
        {
            ITransport transport = new TransportSerialWithEvent(serialPort, factory, Logger);
            return transport;
        }
        public ITransport CreateTransportNetworkWithEvent(IFactory factory, IStreamResource serialPort)
        {
            ITransport transport = new TransportNetWorkWithEvent(serialPort, factory, Logger);
            return transport;
        }
        #endregion

        #region CreateMaster
        /// <summary>
        /// 继续拆分
        /// </summary>
        /// <param name="transport"></param>
        /// <returns></returns>
        public IMaster CreateMaster(ITransport transport)
        {
            return new MasterModbus(transport);
        }
        public IMaster CreateMasterWithConfig(CommunicationConfig config)
        {
            ITransport transport = null;
            switch (config.PortType)
            {
                case PortType.SerialPort:
                    transport = CreateTransport(config.SerialConfig);
                    break;
                case PortType.TCPClient:
                    transport = CreateTransport(config.NetWorkConfig);
                    break;
            }
            return new MasterModbus(transport);
        }
        public IMaster CreateMasterEventWithConfig(CommunicationConfig config)
        {
            ITransport transport = null;
            switch (config.PortType)
            {
                case PortType.SerialPort:
                    transport = CreateTransportSerialWithEvent(config.SerialConfig);
                    break;
                case PortType.TCPClient:
                    transport = CreateTransportNetworkWithEvent(config.NetWorkConfig);
                    break;
            }
            transport.Retries = Retries;
            return new MasterModbus(transport);
        }
        #endregion

        #region CreateConfigTransport

        public ITransport CreateTransport(SerialPortConfig config)
        {
            SerialPort serialPort = CommonUtil.CreateSerialPort(config);
            IStreamResource streamResource = new SerialPortAdapter(serialPort);
            ITransport transport = new TransportModbus(streamResource, this, Logger);
            return transport;
        }
        public ITransport CreateTransport(NetworkConfig config)
        {
            IStreamResource streamResource = new TcpClientAdapter(config.HostIP, config.HostPort);
            ITransport transport = new TransportModbus(streamResource, this, Logger);
            return transport;
        }

        public ITransport CreateTransportSerialWithEvent(SerialPortConfig config)
        {
            SerialPort serialPort = CommonUtil.CreateSerialPort(config);
            IStreamResource streamResource = new SerialPortAdapter(serialPort);
            ITransport transport = new TransportSerialWithEvent(streamResource, this, Logger);
            return transport;
        }
        public ITransport CreateTransportSerialWithEvent(NetworkConfig config)
        {
            IStreamResource streamResource = new TcpClientAdapter(config.HostIP, config.HostPort);
            ITransport transport = new TransportSerialWithEvent(streamResource, this, Logger);
            return transport;
        }

        public ITransport CreateTransportNetworkWithEvent(SerialPortConfig config)
        {
            SerialPort serialPort = CommonUtil.CreateSerialPort(config);
            IStreamResource streamResource = new SerialPortAdapter(serialPort);
            ITransport transport = new TransportNetWorkWithEvent(streamResource, this, Logger);
            return transport;
        }
        public ITransport CreateTransportNetworkWithEvent(NetworkConfig config)
        {
            IStreamResource streamResource = new TcpClientAdapter(config.HostIP, config.HostPort);
            ITransport transport = new TransportNetWorkWithEvent(streamResource, this, Logger);
            return transport;
        }
        #endregion
    }
}
