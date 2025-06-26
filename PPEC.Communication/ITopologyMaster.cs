using PPEC.Communication.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public interface ITopologyMaster
    {
        TopologyId Id { get; set; }
        ICommunicationMaster ComMaster { get; set; }
        T GetValue<T>(AddressName name, bool isSyncSlave = true);
        void SetValue<T>(T value, AddressName name, bool isSyncSlave = true);
        void Start();
        void Stop();
        Task<T> GetValueAsync<T>(AddressName name) where T : struct;
        Task SetValueAsync<T>(T value, AddressName name) where T : struct;
        void SlaveToBufferBatch(AddressName startAddress, ushort numOfRegisters = 125);

    }
}
