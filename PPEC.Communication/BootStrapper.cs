using PPEC.Communication.CAN;
using PPEC.Communication.Config;
using PPEC.Communication.Enum;
using PPEC.Communication.Interface;
using PPEC.Communication.Parameter;
using PPEC.Communication.Parameter.Data;
using PPEC.Communication.Parameter.Transform;
using Prism.Ioc;
using System.Collections.Generic;
using Unity;

namespace PPEC.Communication
{
    public class BootStrapper
    {
        private readonly IContainerProvider _containerProvider;
        private readonly IContainerRegistry _containerRegistry;
        public BootStrapper(IUnityContainer unityContainer, IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider;
            _containerRegistry = containerProvider as IContainerRegistry;
        }

        public void OnStart()
        {
            RegisterTypes();
        }

        private void RegisterTypes()
        {
            _containerRegistry.Register<IDefaultDataSource, DefaultDataSource>();
            _containerRegistry.Register(typeof(ITransform<float>), c => ParameterFactory.CreateTransformFloat());
            _containerRegistry.Register(typeof(ITransform<ushort>), c => ParameterFactory.CreateTransformUshort());
            _containerRegistry.Register(typeof(ITransform<int>), c => ParameterFactory.CreateTransformInt());
            _containerRegistry.Register(typeof(ITransform<short>), c => ParameterFactory.CreateTransformShort());
            _containerRegistry.Register(typeof(ITransform<decimal>), c => ParameterFactory.CreateTransformDecimal());

            _containerRegistry.RegisterSingleton(typeof(ITopologyConfig), typeof(TopologyConfig_86CA3A), TopologyId.PPEC86CA3A.ToString());
            _containerRegistry.RegisterSingleton(typeof(ITopologyConfig), typeof(TopologyConfig_86CA3B), TopologyId.PPEC86CA3B.ToString());


            _containerRegistry.RegisterSingleton(typeof(ITransformLookup), c => { return new TransformLookup(_containerProvider); });
            _containerRegistry.RegisterSingleton(typeof(ITopologyConfigLookup), c => { return new TopologyConfigLookup(_containerProvider); });

            _containerRegistry.Register<ITopologyMaster, TopologyMaster>();
            //_containerRegistry.Register(typeof(ITopologyMaster), t =>
            //{
            //    return new CANMaster(t.Resolve<ITopologyConfigLookup>(), new Dictionary<string, CANModelParam>(), t.Resolve<ITransformLookup>());
            //});
            //_containerRegistry.Register<IFactory, Factory>();

            _containerRegistry.Register(typeof(ICommunicationMaster), c =>
            {
                var factory = c.Resolve<IFactory>();
                return new ConcurrentComMaster(factory);
            });
            _containerRegistry.Register<ICommunicationMaster, ConcurrentComMaster>();

        }
    }
}
