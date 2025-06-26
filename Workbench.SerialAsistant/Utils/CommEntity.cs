using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.SerialAsistant.Models;

namespace Workbench.SerialAsistant.Utils
{
    public static class CommEntity
    {

        public static ObservableCollection<SerialCommBoxItems> SerialBaudRateItemList => new ObservableCollection<SerialCommBoxItems>()
        {
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.BaudRate1.ToString(),
                Value = SerialParamsName.BaudRate1,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.BaudRate2.ToString(),
                Value = SerialParamsName.BaudRate2,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.BaudRate3.ToString(),
                Value = SerialParamsName.BaudRate3,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.BaudRate4.ToString(),
                Value = SerialParamsName.BaudRate4,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.BaudRate5.ToString(),
                Value = SerialParamsName.BaudRate5,
            }
        };

        public static ObservableCollection<SerialCommBoxItems> SerialStopBitsItemList => new ObservableCollection<SerialCommBoxItems>()
        {
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.None,
                Value = (int)StopBits.None,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.One,
                Value = (int)StopBits.One,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.OnePointFive,
                Value = (int)StopBits.OnePointFive,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.Two,
                Value = (int)StopBits.Two,
            }
        };

        public static ObservableCollection<SerialCommBoxItems> SerialParityItemList => new ObservableCollection<SerialCommBoxItems>()
        {
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.None,
                Value = (int)Parity.None,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.Odd,
                Value = (int)Parity.Odd,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.Even,
                Value = (int)Parity.Even,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.Mark,
                Value = (int)Parity.Mark,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.Space,
                Value = (int)Parity.Space,
            }
        };

        public static ObservableCollection<SerialCommBoxItems> SerialFlowControlItemList => new ObservableCollection<SerialCommBoxItems>()
        {
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.None,
                Value = (int)Handshake.None,
            },
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.XOnXOff,
                Value = (int)Handshake.XOnXOff,
            }
            ,
            new SerialCommBoxItems()
            {
                Name = SerialParamsName.RequestToSend,
                Value = (int)Handshake.RequestToSend,
            }
        };
    }
}
