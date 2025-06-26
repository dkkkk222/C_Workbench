using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    public interface IRequest : IMessage
    {
        /// <summary>
        ///     Validate the specified response against the current request.
        /// </summary>
        void ValidateResponse(IMessage response);
    }
}
