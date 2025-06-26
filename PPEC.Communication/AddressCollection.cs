using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PPEC.Communication
{
    public class AddressCollection : Collection<byte>, IMessageDataCollection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterCollection" /> class.
        /// </summary>
        public AddressCollection()
        {
        }

        public AddressCollection(ushort address)
            : this((IList<byte>)BitConverter.GetBytes(address))
        {
        }

        public AddressCollection(int address)
        {
        }
        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterCollection" /> class.
        /// </summary>
        /// <param name="bytes">Array for register collection.</param>
        public AddressCollection(params byte[] bytes)
            : this((IList<byte>)bytes)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterCollection" /> class.
        /// </summary>
        /// <param name="addressList">List for register collection.</param>
        public AddressCollection(IList<byte> addressList)
            : base(addressList.IsReadOnly ? new List<byte>(addressList) : addressList)
        {
        }
        /// <summary>
        /// 获取网络字节序--小端
        /// </summary>
        public byte[] NetworkBytes
        {
            get
            {
                return this.ToArray();
            }
        }
        /// <summary>
        /// 获取主机字节序--大端
        /// </summary>
        public byte[] HostNetworkBytes
        {
            get
            {
                return this.Reverse().ToArray();
            }
        }
        public byte ByteCount => (byte)(Count);
        /// <summary>
        ///     Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        /// </returns>
        public override string ToString()
        {
            return string.Concat("{", string.Join(", ", this.Select(v => v.ToString()).ToArray()), "}");
        }
    }
}
