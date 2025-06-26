using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication
{
    internal static class ConstParams
    {
        public const int MaximumDiscreteRequestResponseSize = 2040;
        public const int MaximumRegisterRequestResponseSize = 127;

        // modbus slave exception offset that is added to the function code, to flag an exception
        public const byte ExceptionOffset = 128;

        // default setting for number of retries for IO operations
        public const int DefaultRetries = 3;

        // default number of milliseconds to wait after encountering an ACKNOWLEGE or SLAVE DEVIC BUSY slave exception response.
        public const int DefaultWaitToRetryMilliseconds = 250;

        // default setting for IO timeouts in milliseconds
        //public const int DefaultTimeout = 1000;


        public const ushort CoilOn = 0xFF00;
        public const ushort CoilOff = 0x0000;

        // IP slaves should be addressed by IP
        public const byte DefaultIpSlaveUnitId = 0;

        //// An existing connection was forcibly closed by the remote host
        //public const int ConnectionResetByPeer = 10054;

        // Existing socket connection is being closed
        //public const int WSACancelBlockingCall = 10004;

        // used by the ASCII tranport to indicate end of message
        public const string NewLine = "\r\n";
    }

    internal static class Resources
    {
        public const string Acknowlege = "Specialized use in conjunction with programming commands.The server (or slave) has accepted the request and is processing it, but a long duration of time will be required to do so.This response is returned to prevent a timeout error from occurring in the client(or master). The client(or master) can next issue a Poll Program Complete message to determine if processing is completed.";

        public const string EmptyEndPoint = "Argument endPoint cannot be empty.";

        public const string GatewayPathUnavailable = "Specialized use in conjunction with gateways, indicates that the gateway was unable to allocate an internal communication path from the input port to the output port for processing the request.Usually means that the gateway is misconfigured or overloaded.";

        public const string GatewayTargetDeviceFailedToRespond = "Specialized use in conjunction with gateways, indicates that no response was obtained from the target device.Usually means that the device is not present on the network.";

        public const string HexCharacterCountNotEven = "Hex string must have even number of characters.";

        public const string IllegalDataAddress = "The data address received in the query is not an allowable address for the server (or slave). More specifically, the combination of reference number and transfer length is invalid.For a controller with 100 registers, the PDU addresses the first register as 0, and the last one as 99. If a request is submitted with a starting register address of 96 and a quantity of registers of 4, then this request will successfully operate(address-wise at least) on registers 96, 97, 98, 99. If a request is submitted with a starting register address of 96 and a quantity of registers of 5, then this request will fail with Exception Code 0x02 “Illegal Data Address” since it attempts to operate on registers 96, 97, 98, 99 and 100, and there is no register with address 100.";

        public const string IllegalDataValue = "A value contained in the query data field is not an allowable value for server(or slave). This indicates a fault in the structure of the remainder of a complex request, such as that the implied length is incorrect.It specifically does NOT mean that a data item submitted for storage in a register has a value outside the expectation of the application program, since the MODBUS protocol is unaware of the significance of any particular value of any particular register.";

        public const string IllegalFunction = "The function code received in the query is not an allowable action for the server (or slave). This may be because the function code is only applicable to newer devices, and was not implemented in the unit selected.It could also indicate that the server(or slave) is in the wrong state to process a request of this type, for example because it is unconfigured and is being asked to return register values.";

        public const string MemoryParityError = "Specialized use in conjunction with function codes 20 and 21 and reference type 6, to indicate that the extended file area failed to pass a consistency check.";

        public const string NetworkBytesNotEven = "Array networkBytes must contain an even number of bytes.";

        public const string SlaveDeviceBusy = "Specialized use in conjunction with programming commands. The server (or slave) is engaged in processing a long–duration program command.The client(or master) should retransmit the message later when the server(or slave) is free.";

        public const string SlaveDeviceFailure = "An unrecoverable error occurred while the server(or slave) was attempting to perform the requested action.";

        public const string SlaveExceptionResponseFormat = "Function Code: {1}{0}Exception Code: {2} - {3}";

        public const string SlaveExceptionResponseInvalidFunctionCode = "Invalid function code value for SlaveExceptionResponse.";

        public const string TimeoutNotSupported = "The compact framework UDP client does not support timeouts.";

        public const string UdpClientNotConnected = "UdpClient must be bound to a default remote host. Call the Connect method.";

        public const string Unknown = "Unknown slave exception code.";

        public const string WaitRetryGreaterThanZero = "WaitToRetryMilliseconds must be greater than 0.";
    }

    public static class ExceptionCodes
    {
        /// <summary>
        /// The function code received in the query is not an allowable action for the slave.  This may be because the function code is only applicable to newer devices, and was not implemented in the unit selected.  It could also indicate that the slave is in the wrong state to process a request of this type, for example because it is unconfigured and is being asked to return register values. If a Poll Program Complete command was issued, this code indicates that no program function preceded it.
        /// </summary>
        public const byte IllegalFunction = 1;

        /// <summary>
        /// The data address received in the query is not an allowable address for the slave. More specifically, the combination of reference number and transfer length is invalid. For a controller with 100 registers, a request with offset 96 and length 4 would succeed, a request with offset 96 and length 5 will generate exception 02.
        /// </summary>
        public const byte IllegalDataAddress = 2;

        /// <summary>
        /// A value contained in the query data field is not an allowable value for the slave.  This indicates a fault in the structure of remainder of a complex request, such as that the implied length is incorrect. It specifically does NOT mean that a data item submitted for storage in a register has a value outside the expectation of the application program, since the MODBUS protocol is unaware of the significance of any particular value of any particular register.
        /// </summary>
        public const byte IllegalDataValue = 3;

        /// <summary>
        /// An unrecoverable error occurred while the slave was attempting to perform the requested action.
        /// </summary>
        public const byte SlaveDeviceFailure = 4;

        /// <summary>
        /// Specialized use in conjunction with programming commands.
        /// 
        /// The slave has accepted the request and is processing it, but a long duration of time will be required to do so.T
        /// his response is returned to prevent a timeout error from occurring in the master.The master can next issue a 
        /// Poll Program Complete message to determine if processing is completed.
        /// </summary>
        public const byte Acknowledge = 5;

        /// <summary>
        /// Specialized use in conjunction with programming commands.
        /// The slave is engaged in processing a long-duration program command.The master should retransmit 
        /// the message later when the slave is free.
        /// </summary>
        public const byte SlaveDeviceBusy = 6;
    }
}
