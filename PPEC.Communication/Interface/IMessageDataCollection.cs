using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Interface
{
    public interface IMessageDataCollection
    {
        /// <summary>
        ///     Gets the network bytes.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        byte[] NetworkBytes { get; }

        byte[] HostNetworkBytes { get; }

        /// <summary>
        ///     Gets the byte count.
        /// </summary>
        byte ByteCount { get; }
    }
}
