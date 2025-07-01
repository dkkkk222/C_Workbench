using System;
using System.Collections.ObjectModel;
using Workbench.Models;

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
    }
}
