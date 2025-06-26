using PPEC.Communication.Config;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public static class CommonUtil
    {
        public static SerialPort CreateSerialPort(SerialPortConfig config)
        {
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = config.SerialProtName;
            serialPort.BaudRate = config.BaudRate;
            serialPort.DataBits = config.DataBits;
            serialPort.Parity = config.Parity;
            serialPort.StopBits = config.StopBits;
            serialPort.WriteBufferSize = config.BufferSize;
            serialPort.ReadBufferSize = config.BufferSize;
            serialPort.Handshake = config.FlowControl;
            serialPort.ReadTimeout = config.ReadTimeOut <= 0 ? 500 : config.WriteTimeOut;
            serialPort.WriteTimeout = config.WriteTimeOut <= 0 ? 500 : config.WriteTimeOut;
            //serialPort.Open();
            return serialPort;
        }
    }
}
