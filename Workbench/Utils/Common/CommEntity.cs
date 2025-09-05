using System;
using System.Collections.ObjectModel;
using Workbench.Models;
using Workbench.Models.Consts;

namespace Workbench.Utils.Common
{
    public static class CommEntity
    {
        /// <summary>
        /// 通信协议
        /// </summary>
        public static ObservableCollection<ComConnectType> ComConnectTypeList => new ObservableCollection<ComConnectType>
        {
            //new ComConnectType()
            //{
            //    Name = Enum.GetName(typeof(ComConnectEnum), ComConnectEnum.Modbus),
            //    Value = ComConnectEnum.Modbus,
            //},
            //new ComConnectType()
            //{
            //    Name = Enum.GetName(typeof(ComConnectEnum), ComConnectEnum.CAN),
            //    Value = ComConnectEnum.CAN,
            //}
        };

        public static ObservableCollection<BBLLCCANBAUDItem> BBLLCCANTYPEList => new ObservableCollection<BBLLCCANBAUDItem>
        {
            new BBLLCCANBAUDItem()
            {
                Name = "USBCAN-2E-U",
                Value = BBLLCCANBaud.A,
            },
            new BBLLCCANBAUDItem()
            {
                Name = "USBCAN2",
                Value = BBLLCCANBaud.B,
            }
        };

        public static ObservableCollection<BBLLCCANBAUDItem> BBLLCCANBAUDList => new ObservableCollection<BBLLCCANBAUDItem>
        {
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud0,
                Value = BBLLCCANBaud.A,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud1,
                Value = BBLLCCANBaud.B,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud2,
                Value = BBLLCCANBaud.C,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud3,
                Value = BBLLCCANBaud.D,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud4,
                Value = BBLLCCANBaud.E,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud5,
                Value = BBLLCCANBaud.F,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud6,
                Value = BBLLCCANBaud.G,
            },
            new BBLLCCANBAUDItem()
            {
                Name = CAN_Baud.Baud7,
                Value = BBLLCCANBaud.H,
            }
        };

        public static ObservableCollection<BBLLCCANItem> BBLLCCANList => new ObservableCollection<BBLLCCANItem>
        {
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can0,
                Value = BBLLCCANPort.A,
            },
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can1,
                Value = BBLLCCANPort.B,
            }//,
            //new BBLLCCANItem()
            //{
            //    Name = CAN_Port.Can2,
            //    Value = BBLLCCANPort.C,
            //}
            //,
            //new BBLLCCANItem()
            //{
            //    Name = CAN_Port.Can3,
            //    Value = BBLLCCANPort.D,
            //}
            //,
            //new BBLLCCANItem()
            //{
            //    Name = CAN_Port.Can4,
            //    Value = BBLLCCANPort.E,
            //}
            //,
            //new BBLLCCANItem()
            //{
            //    Name = CAN_Port.Can5,
            //    Value = BBLLCCANPort.F,
            //}
        };
    }
}
