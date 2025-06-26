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
    public static class ParameterFactory
    {
        /// <summary>
        /// 创建float类型参数转换器
        /// </summary>
        /// <returns> ITransform<float> </returns>
        public static ITransform<float> CreateTransformFloat()
        {
            return new TransformFloat();
        }
        /// <summary>
        /// 创建Ushort类型参数转换器
        /// </summary>
        /// <returns> ITransform<ushort> </returns>
        public static ITransform<ushort> CreateTransformUshort()
        {
            return new TransformUshort();
        }

        public static ITransform<int> CreateTransformInt()
        {
            return new TransformInt();
        }

        public static ITransform<short> CreateTransformShort()
        {
            return new TransformShort();
        }

        public static ITransform<decimal> CreateTransformDecimal()
        {
            return new TransformDecimal();
        }

        public static IDefaultDataSource CreateDefaultDataSource()
        {
            return new DefaultDataSource();
        }

        public static IDataSource<T> CreateDataSource<T>()
        {
            return new DataSource<T>();
        }

        public static IParameterMaster CreateMaster(IDefaultDataSource registers)
        {
            return new ParameterMaster(registers);
        }
        public static IParameterMaster CreateDefaultMater()
        {
            var dataSource = CreateDefaultDataSource();
            return CreateMaster(dataSource);
        }
    }
}
