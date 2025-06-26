using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Models.BootLoader;
using Workbench.Models.Consts;
using Workbench.Models.Enums;

namespace Workbench.Utils.Common
{
    public static class CommEntity
    {
        /// <summary>
        /// 通信协议
        /// </summary>
        public static ObservableCollection<ComConnectType> ComConnectTypeList => new ObservableCollection<ComConnectType>
        {
            new ComConnectType()
            {
                Name = Enum.GetName(typeof(ComConnectEnum), ComConnectEnum.Modbus),
                Value = ComConnectEnum.Modbus,
            },
            new ComConnectType()
            {
                Name = Enum.GetName(typeof(ComConnectEnum), ComConnectEnum.CAN),
                Value = ComConnectEnum.CAN,
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
            },
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can2,
                Value = BBLLCCANPort.C,
            }
            ,
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can3,
                Value = BBLLCCANPort.D,
            }
            ,
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can4,
                Value = BBLLCCANPort.E,
            }
            ,
            new BBLLCCANItem()
            {
                Name = CAN_Port.Can5,
                Value = BBLLCCANPort.F,
            }
        };
    }
}
