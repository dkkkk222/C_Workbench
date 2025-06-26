using PPEC.Communication.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    public class MasterModbus : MasterAssistant, IMaster
    {
        private ITransport _transport;
        internal MasterModbus(ITransport transport) : base(transport)
        {
            _transport = transport;
        }

        /// <summary>
        ///     Gets the Modbus Transport.
        /// </summary>
        public ITransport Transport => _transport;

        #region Read
        public ushort[] ReadMultiple(byte slaveAddress, AddressCollection startAddress, ushort numberOfPoints)
        {
            ValidateNumberOfPoints("numberOfPoints", numberOfPoints, 125);

            var request = new ReadRequestModbus(
                slaveAddress,
                startAddress,
                numberOfPoints);

            ReadResponseModbus response =
                Transport.UnicastMessage<ReadResponseModbus>(request);
            return response.Data.Take(request.NumberOfPoints).ToArray();
        }
        public Task<ushort[]> ReadMultipleAsync(byte slaveAddress, AddressCollection startAddress, ushort numberOfPoints, CancellationToken cancellationToken)
        {
            ValidateNumberOfPoints("numberOfPoints", numberOfPoints, 125);

            var request = new ReadRequestModbus(
                slaveAddress,
                startAddress,
                numberOfPoints);

            return Task.Factory.StartNew(() =>
            {
                return Transport.UnicastMessage<ReadResponseModbus>(request).Data.Take(request.NumberOfPoints).ToArray();
            }, cancellationToken);
        }
        #endregion

        #region Write
        public void WriteSingle(byte slaveAddress, AddressCollection startAddress, ushort data)
        {
            var request = new WriteSingleRequestModbus(
                slaveAddress,
                startAddress,
                new RegisterCollection(data));
            Transport.UnicastMessage<WriteSingleRequestModbus>(request);
        }
        public void WriteMultiple(byte slaveAddress, AddressCollection startAddress, ushort[] data)
        {
            ValidateData("data", data, 123);

            var request = new WriteMultipleRequestModbus(
                slaveAddress,
                startAddress,
                new RegisterCollection(data));
            Transport.UnicastMessage<WriteMultipleResponseModbus>(request);
        }
        public Task WriteMultipleAsync(byte slaveAddress, AddressCollection startAddress, ushort[] data, CancellationToken cancellationToken)
        {
            ValidateData("data", data, 123);

            var request = new WriteMultipleRequestModbus(
                slaveAddress,
                startAddress,
                new RegisterCollection(data));
            return PerformWriteRequestAsync<WriteMultipleResponseModbus>(request, cancellationToken);
        }

        #endregion

        #region Async

        private Task<WriteMultipleResponseModbus> PerformWriteRequestAsync<T>(IMessage request, CancellationToken cancellationToken)
            where T : WriteMultipleResponseModbus, new()
        {
            return Task<WriteMultipleResponseModbus>.Factory.StartNew(() =>
            {
                return Transport.UnicastMessage<T>(request);
            }, cancellationToken);
        }
        #endregion
    }
}
