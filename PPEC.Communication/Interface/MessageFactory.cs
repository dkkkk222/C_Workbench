using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    internal class MessageFactory
    {
        public static T CreateMessage<T>(byte[] frame)
            where T : IMessage, new()
        {
            //Create the message
            T message = new T();

            //initialize it
            message.Initialize(frame);

            //return it
            return message;
        }
    }
}
