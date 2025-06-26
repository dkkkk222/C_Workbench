using PPEC.Communication.Parameter;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interfaces
{
    public interface IParameterMaster
    {
        /// <summary>
        /// 寄存器存储
        /// </summary>
        IDefaultDataSource Registers { get; }
        IDictionary<string, CANModelParam> DicCache { get; }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <typeparam name="T"> 返回参数类型 </typeparam>
        /// <param name="info"> 参数信息 </param>
        /// <param name="transform"> 参数转换器 </param>
        /// <returns></returns>
        T GetValue<T>(IParamInfo info, ITransform<T> transform, string AddressName = null);

        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <typeparam name="T"> 返回参数类型 </typeparam>
        /// <param name="value"> 设置值 </param>
        /// <param name="info"> 参数信息 </param>
        /// <param name="transform"> 参数转换器 </param>
        void SetValue<T>(T value, IParamInfo info, ITransform<T> transform, string AddressName = null);
    }
}
