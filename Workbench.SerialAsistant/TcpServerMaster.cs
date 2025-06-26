using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Prism.Events;
using Prism.Ioc;
using Workbench.SerialAsistant.Events;

namespace Workbench.SerialAsistant
{
    public class TcpServerMaster : IComMaster
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly int _bufferLength;
        private TcpListener _tcpListener;
        private ConcurrentDictionary<string, TcpClient> _connectedClients = new ConcurrentDictionary<string, TcpClient>();
        public CancellationTokenSource cts;

        public TcpServerMaster(string ip, int port, int bufferLength)
        {
            _ip = ip;
            _port = port;
            _bufferLength = bufferLength;
            cts = new CancellationTokenSource();
        }
        public bool Connect()
        {
            var result = true;
            try
            {
                var ip = IPAddress.Parse(_ip);
                _tcpListener = new TcpListener(ip, _port);
                Task.Run(async () => { await StartListening(); });
                Task.Run(() =>
                {
                    var list = new List<string>();
                    while (!cts.Token.IsCancellationRequested)
                    {
                        list.Clear();
                        foreach (var kv in _connectedClients)
                        {
                            var clientName = kv.Key;
                            var client = kv.Value;
                            bool isConnected = IsSocketConnect(client);
                            if (!isConnected)
                            {
                                list.Add(clientName);
                            }
                        }

                        foreach (var item in list)
                        {
                            _connectedClients.TryRemove(item, out var client);
                            ContainerLocator.Container.Resolve<IEventAggregator>().GetEvent<TcpClientDisConnectedEvent>().Publish(item);
                        }
                    }
                });
                Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        foreach (var client in _connectedClients.Values)
                        {
                            byte[] buffer = new byte[_bufferLength];
                            var stream = client.GetStream();
                            if (stream.DataAvailable)
                            {
                                var bytesRead = stream.Read(buffer, 0, buffer.Length);
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
                });
            }
            catch (Exception ex)
            {
                result = false;
                Dispose();
            }
            return result;
        }

        static bool IsSocketConnect(TcpClient client)
        {
            if (client == null || client.Client == null)
            {
                return false;
            }
            //先看看状态
            if (client.Client.Connected == false || client.Client.RemoteEndPoint == null)
            {
                return false;
            }

            if (client.Client.Poll(200, SelectMode.SelectRead))
            {
                var buffer = new byte[1024];
                var read = client.Client.Receive(buffer, 0, buffer.Length, SocketFlags.Peek);
                if (read == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task StartListening()
        {
            _tcpListener.Start();
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                    var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    if (!_connectedClients.ContainsKey(remoteEndPoint.ToString()) && remoteEndPoint != null)
                    {
                        _connectedClients.TryAdd(remoteEndPoint.ToString(), client);
                        ContainerLocator.Container.Resolve<IEventAggregator>().GetEvent<TcpClientConnectedEvent>().Publish(remoteEndPoint.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Dispose();
                throw ex;
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            _tcpListener?.Stop();
        }

        public void Send(byte[] bytes, string clientName)
        {
            if (!string.IsNullOrEmpty(clientName))
            {
                _connectedClients.TryGetValue(clientName, out var client);
                if (client != null)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            else
            {
                foreach (var client in _connectedClients.Values)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
