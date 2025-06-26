using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    public interface IMessage
    {
        /// <summary>
        /// The function code tells the server what kind of action to perform.
        /// </summary>
        byte FunctionCode { get; set; }

        /// <summary>
        ///     Address of the slave (server).
        /// </summary>
        byte? SlaveAddress { get; set; }

        /// <summary>
        ///     Composition of the slave address and protocol data unit.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        byte[] MessageFrame { get; }

        /// <summary>
        ///     Composition of the function code and message data.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        byte[] ProtocolDataUnit { get; }
        /// <summary>
        ///     Initializes a modbus message from the specified message frame.
        /// </summary>
        /// <param name="frame">Bytes of Modbus frame.</param>
        void Initialize(byte[] frame);
        IMessageDataCollection AddressCollection { get; set; }
        byte? StartOfInfo { get; set; }
        byte? EndOfInfo { get; set; }
        byte[] PureBytes { get; set; }
        bool IsQuickCommand { get; set; }
        bool IsSendPure { get; set; }
    }
}
