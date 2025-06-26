using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Prism.Events;
using Prism.Ioc;
using Workbench.SerialAsistant.Events;

namespace Workbench.SerialAsistant
{
    public class TcpClientMaster : IComMaster
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly int _bufferLength;
        private TcpClient _client;
        private NetworkStream _stream;
        public CancellationTokenSource cts;
        public TcpClientMaster(string ip, int port, int bufferLength)
        {
            _ip = ip;
            _port = port;
            _bufferLength = bufferLength;
            cts = new CancellationTokenSource();
        }
        public bool Connect()
        {
            bool result = true;
            try
            {
                _client = new TcpClient(_ip, _port);
                _stream = _client.GetStream();
                keepReading();
            }
            catch
            {
                result = false;
                Dispose();
            }
            return result;
        }

        private void keepReading()
        {
            Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[_bufferLength];
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (_stream.DataAvailable)
                        {
                            var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                byte[] actualData = new byte[bytesRead];
                                Array.Copy(buffer, actualData, bytesRead);
                                ContainerLocator.Container.Resolve<IEventAggregator>().GetEvent<ReceiveDataEvent>().Publish(actualData);
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

        public void Dispose()
        {
            cts.Cancel();
            _stream?.Close();
            _client?.Close();
        }

        public void Send(byte[] bytes, string clientName)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }
    }
}
