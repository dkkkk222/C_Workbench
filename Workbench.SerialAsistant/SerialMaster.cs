using System.IO.Ports;
using System.Windows;
using Workbench.SerialAsistant.Models;
using Prism.Ioc;
using Prism.Events;
using Workbench.SerialAsistant.Events;
using System;

namespace Workbench.SerialAsistant
{
    public class SerialMaster : IComMaster
    {
        public SerialPort _port { get; set; }
        public SerialMaster(SerialPortConfig config)
        {
            _port = new SerialPort(config.SerialProtName);
            _port.BaudRate = config.BaudRate;
            _port.DataBits = config.DataBits;
            _port.Parity = config.Parity;
            _port.StopBits = config.StopBits;
            _port.ReadBufferSize = config.BufferSize;
            _port.WriteBufferSize = config.BufferSize;
            _port.DataReceived += _port_DataReceived;
        }

        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            // 获取接收缓冲区中的字节数
            int bytesToRead = sp.BytesToRead;
            // 创建一个byte数组，用于存储接收到的数据
            byte[] buffer = new byte[bytesToRead];
            // 读取数据到buffer中
            sp.Read(buffer, 0, bytesToRead);
            var eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<ReceiveDataEvent>().Publish(buffer);
        }

        public bool Connect()
        {
            var result = true;
            try
            {
                if (!_port.IsOpen)
                    _port.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                result = false;
            }
            return result;
        }

        public void Dispose()
        {
            if (_port.IsOpen)
                _port.Close();
        }

        public void Send(byte[] bytes, string clientName)
        {
            _port.Write(bytes, 0, bytes.Length);
        }
    }
}
