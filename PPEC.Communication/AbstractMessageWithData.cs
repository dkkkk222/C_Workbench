using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public abstract class AbstractMessageWithData<TData> : AbstractMessage
        where TData : IMessageDataCollection
    {
        public AbstractMessageWithData()
        {
        }

        public TData Data
        {
            get => (TData)MessageImpl.Data;
            set => MessageImpl.Data = value;
        }
    }
}
