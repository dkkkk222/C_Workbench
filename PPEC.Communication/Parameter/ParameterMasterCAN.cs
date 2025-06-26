using PPEC.Communication.Interfaces;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Parameter
{
    public class ParameterMasterCAN : IParameterMaster
    {
        public IDictionary<string, CANModelParam> DicCache
        {
            get => _dicCache;
        }
        private readonly IDictionary<string, CANModelParam> _dicCache;
        private readonly IDefaultDataSource _registers;
        public IDefaultDataSource Registers => _registers;
        public ParameterMasterCAN(IDictionary<string, CANModelParam> dicCache)
        {
            _dicCache = dicCache;
        }
        /// <summary>
        /// 收到数据后的参数转换
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="info">参数配置和值</param>
        /// <param name="transform">参数转换类接口</param>
        /// <returns></returns>
        public T GetValue<T>(IParamInfo info, ITransform<T> transform, string AddressName = null)
        {
            if (info == null || transform == null)
            {
                return default;
            }
            // ushort[] ushortArray = new ushort[info.NumOfRegisters / sizeof(ushort)];
            ushort[] ushortArray = new ushort[info.NumOfRegisters];
            byte[] subArray = new byte[info.NumOfRegisters];
            if (DicCache[AddressName].Recevie == null)
            {
                var regs = transform.GetRegisters((T)DicCache[AddressName].DisplayValue, info);
                ushortArray = regs;
            }
            else
            {
                //从收到的帧中得到需要转换的byte[]
                Array.Copy(DicCache[AddressName].Recevie, info.StartAddress, subArray, 0, info.NumOfRegisters);
                //将byte[]转为ushort[]
                Buffer.BlockCopy(subArray, 0, ushortArray, 0, subArray.Length);
            }

            var value = transform.GetValue(ushortArray, info);
            DicCache[AddressName].DisplayValue = value;
            return (T)value;
        }
        /// <summary>
        /// 发送数据时的参数转换
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="value">值</param>
        /// <param name="info">参数配置和值</param>
        /// <param name="transform">参数转换接口</param>
        public void SetValue<T>(T value, IParamInfo info, ITransform<T> transform, string AddressName = null)
        {
            if (value == null || info == null || transform == null) { return; }
            //数值转为ushort[]
            //var regs = transform.GetRegisters(value, info);

            var regs1 = transform.GetRegistersBytes(value, info);
            //需要发送的Byte[]长度
            //byte[] byteArray = new byte[info.NumOfRegisters];
            //设置当前值
            DicCache[AddressName].ValueData = regs1;
        }
    }
}
