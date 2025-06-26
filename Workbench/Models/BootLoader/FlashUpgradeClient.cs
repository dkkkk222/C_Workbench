using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models.BootLoader
{
    public partial class FlashUpgradeClient
    {
        public enum RegisterOrder { LowHigh = 0, HighLow = 1 };
        private bool debug = true;
        private byte[] transactionIdentifier = new byte[2];
        private byte[] crc = new byte[2];
        private byte unitIdentifier = 0x00;
        private int baudRate = 9600;
        private int connectTimeout = 1000;

        public byte[] receiveData;
        public byte[] sendData;

        private bool dataReceived = false;
        private bool receiveActive = false;
        private byte[] readBuffer = new byte[2048];
        private int bytesToRead = 0;

        private static byte startByte = 0x5a;
        private static byte stopByte = 0xa5;
        private static byte startByteBack = 0xc3;
        private static byte stopByteBack = 0x3c;

        private int LEN;
        private byte[] FLASH_ADDRESS = new byte[4];
        private byte CMD;

        private SerialPort serialport;
        private Parity parity = Parity.None;
        private StopBits stopBits = StopBits.One;
        private bool connected = false;
        public int NumberOfRetries { get; set; } = 3;
        private int countRetries = 0;

        private uint transactionIdentifierInternal = 0;


        public delegate void ReceiveDataChangedHandler(object sender);
        public event ReceiveDataChangedHandler ReceiveDataChanged;

        public delegate void SendDataChangedHandler(object sender);
        public event SendDataChangedHandler SendDataChanged;

        public delegate void ConnectedChangedHandler(object sender);
        public event ConnectedChangedHandler ConnectedChanged;

        public FlashUpgradeClient() { }

        /// <summary>
        /// Constructor which determines the Serial-Port
        /// </summary>
        /// <param name="serialPort">Serial-Port Name e.G. "COM1"</param>
        public FlashUpgradeClient(string serialPort)
        {
            this.serialport = new SerialPort();
            serialport.PortName = serialPort;
            serialport.BaudRate = baudRate;
            serialport.Parity = parity;
            serialport.StopBits = stopBits;
            serialport.WriteTimeout = 10000;
            serialport.ReadTimeout = connectTimeout;
            serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        #region Property

        /// <summary>
        /// Returns "TRUE" if Client is connected to Server and "FALSE" if not. In case of Modbus RTU returns if COM-Port is opened
        /// </summary>
		public bool Connected
        {
            get
            {
                if (serialport != null)
                {
                    return (serialport.IsOpen);
                }
                return connected;
            }
        }

        //
        // 摘要:
        //     Close connection to Master Device.
        public void Disconnect()
        {
            if (serialport != null)
            {
                receiveActive = false;
                if (serialport.IsOpen & !receiveActive)
                {
                    serialport.Close();
                }

                if (this.ConnectedChanged != null)
                {
                    this.ConnectedChanged(this);
                }
                return;
            }

            connected = false;
            if (this.ConnectedChanged != null)
            {
                this.ConnectedChanged(this);
            }
        }


        /// <summary>
        /// Establish connection to Master device in case of Modbus TCP. Opens COM-Port in case of Modbus RTU
        /// </summary>
        public void Connect()
        {
            if (serialport != null)
            {
                if (!serialport.IsOpen)
                {
                    serialport.BaudRate = baudRate;
                    serialport.Parity = parity;
                    serialport.StopBits = stopBits;
                    serialport.WriteTimeout = 10000;
                    serialport.ReadTimeout = connectTimeout;
                    serialport.Open();
                    connected = true;
                }
                if (ConnectedChanged != null)
                    try
                    {
                        ConnectedChanged(this);
                    }
                    catch { }
                return;
            }
        }

        /// <summary>
        /// Gets or Sets the Baudrate for serial connection (Default = 9600)
        /// </summary>
        public int Baudrate
        {
            get
            {
                return baudRate;
            }
            set
            {
                baudRate = value;
            }
        }

        /// <summary>
        /// Gets or Sets the of Parity in case of serial connection
        /// </summary>
        public Parity Parity
        {
            get
            {
                if (serialport != null)
                    return parity;
                else
                    return Parity.Even;
            }
            set
            {
                if (serialport != null)
                    parity = value;
            }
        }


        /// <summary>
        /// Gets or Sets the number of stopbits in case of serial connection
        /// </summary>
        public StopBits StopBits
        {
            get
            {
                if (serialport != null)
                    return stopBits;
                else
                    return StopBits.One;
            }
            set
            {
                if (serialport != null)
                    stopBits = value;
            }
        }

        /// <summary>
        /// Gets or Sets the connection Timeout in case of ModbusTCP connection
        /// </summary>
        public int ConnectionTimeout
        {
            get
            {
                return connectTimeout;
            }
            set
            {
                connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or Sets the serial Port
        /// </summary>
        public string SerialPort
        {
            get
            {
                return serialport.PortName;
            }
            set
            {
                if (value == null)
                {
                    serialport = null;
                    return;
                }
                if (serialport != null)
                    serialport.Close();
                this.serialport = new SerialPort();
                this.serialport.PortName = value;
                serialport.BaudRate = baudRate;
                serialport.Parity = parity;
                serialport.StopBits = stopBits;
                serialport.WriteTimeout = 10000;
                serialport.ReadTimeout = connectTimeout;
                serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            }
        }
        #endregion


        #region CRC
        /// <summary>
        /// Calculates the CRC16 for Modbus-RTU
        /// </summary>
        /// <param name="data">Byte buffer to send</param>
        /// <param name="numberOfBytes">Number of bytes to calculate CRC</param>
        /// <param name="startByte">First byte in buffer to start calculating CRC</param>
        public static UInt16 calculateCRC(byte[] data, UInt16 numberOfBytes, int startByte)
        {
            byte[] auchCRCHi = {
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
                0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                0x40
                };

            byte[] auchCRCLo = {
                0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
                0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
                0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
                0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
                0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
                0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
                0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
                0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
                0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
                0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
                0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
                0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
                0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
                0x40
                };
            UInt16 usDataLen = numberOfBytes;
            byte uchCRCHi = 0xFF;
            byte uchCRCLo = 0xFF;
            int i = 0;
            int uIndex;
            while (usDataLen > 0)
            {
                usDataLen--;
                if ((i + startByte) < data.Length)
                {
                    uIndex = uchCRCLo ^ data[i + startByte];
                    uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                i++;
            }
            return (UInt16)((UInt16)uchCRCHi << 8 | uchCRCLo);
        }
        #endregion

        #region serial port event handler

        private void DataReceivedHandler(object sender,
                        SerialDataReceivedEventArgs e)
        {
            serialport.DataReceived -= DataReceivedHandler;

            receiveActive = true;

            const long ticksWait = TimeSpan.TicksPerMillisecond * 2000;//((40*10000000) / this.baudRate);

            SerialPort sp = (SerialPort)sender;
            if (bytesToRead == 0)
            {
                sp.DiscardInBuffer();
                receiveActive = false;
                serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                return;
            }
            readBuffer = new byte[256];
            int numbytes = 0;
            int actualPositionToRead = 0;
            DateTime dateTimeLastRead = DateTime.Now;
            do
            {
                try
                {
                    dateTimeLastRead = DateTime.Now;
                    while ((sp.BytesToRead) == 0)
                    {
                        System.Threading.Thread.Sleep(5);
                        if ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) > ticksWait)
                            break;
                    }
                    numbytes = sp.BytesToRead;

                    byte[] rxbytearray = new byte[numbytes];
                    sp.Read(rxbytearray, 0, numbytes);
                    Array.Copy(rxbytearray, 0, readBuffer, actualPositionToRead, (actualPositionToRead + rxbytearray.Length) <= bytesToRead ? rxbytearray.Length : bytesToRead - actualPositionToRead);

                    actualPositionToRead = actualPositionToRead + rxbytearray.Length;

                }
                catch (Exception)
                {
                    if (!sp.IsOpen)
                    {
                        receiveActive = false;
                        return;
                    }
                }

                if (bytesToRead <= actualPositionToRead)
                    break;

                if (DetectValidFrame(readBuffer, (actualPositionToRead < readBuffer.Length) ? actualPositionToRead : readBuffer.Length) | bytesToRead <= actualPositionToRead)
                    break;
            }
            while ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) < ticksWait);

            //10.000 Ticks in 1 ms

            receiveData = new byte[actualPositionToRead];
            Array.Copy(readBuffer, 0, receiveData, 0, (actualPositionToRead < readBuffer.Length) ? actualPositionToRead : readBuffer.Length);

            bytesToRead = 0;
            dataReceived = true;
            receiveActive = false;
            serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            if (ReceiveDataChanged != null)
            {
                ReceiveDataChanged(this);
            }
        }

        public static bool DetectValidFrame(byte[] readBuffer, int length)
        {
            // minimum length 12 bytes
            if (length < 10)
                return false;
            // header of frame
            if (readBuffer[0] != startByteBack)
                return false;
            // end of frame
            if (readBuffer[length - 1] != stopByteBack)
                return false;
            // command correct
            if (readBuffer[1] < 0x00 | readBuffer[1] > 0x05)
                return false;

            //Len correct
            if (length - 10 != readBuffer[6])
                return false;

            //CRC correct?
            byte[] crc = new byte[2];
            crc = BitConverter.GetBytes(calculateCRC(readBuffer, (ushort)(length - 3), 0));
            if (crc[1] != readBuffer[length - 3] | crc[0] != readBuffer[length - 2])
                return false;
            return true;
        }

        #endregion

        #region Method
        /// <summary>
        /// Read Holding Registers from Master device (FC3).
        /// </summary>
        /// <param name="startingAddress">First holding register to be read</param>
        /// <param name="quantity">Number of holding registers to be read</param>
        /// <returns>Int Array which contains the holding registers</returns>


        public byte[] InteractSlave(byte cmd, Int32 flashAddress, byte[] dataArray)
        {

            transactionIdentifierInternal++;
            if (serialport == null)
            {
                return Array.Empty<byte>();
            }

            if (!serialport.IsOpen)
            {
                throw new Exception("serial port not opened");
            }

            if (cmd > 0x05 | cmd < 0x00)
            {
                throw new ArgumentException("cmd must be 0x00 - 0x05;");
            }
            var dataLen = dataArray.Length;
            if (dataLen > 255)
                throw new ArgumentException("data array length larger than 255");

            byte[] response;
            this.transactionIdentifier = BitConverter.GetBytes((uint)transactionIdentifierInternal);
            this.LEN = dataLen + 10;
            UInt16 crcLen = Convert.ToUInt16(dataLen + 7);
            this.FLASH_ADDRESS = BitConverter.GetBytes(flashAddress);
            this.CMD = cmd;

            byte[] dataToWrite = new byte[12 + dataArray.Length];
            // package data
            dataToWrite[0] = this.transactionIdentifier[1];
            dataToWrite[1] = this.transactionIdentifier[0];

            dataToWrite[2] = startByte;
            dataToWrite[3] = this.CMD;

            dataToWrite[4] = this.FLASH_ADDRESS[3];
            dataToWrite[5] = this.FLASH_ADDRESS[2];
            dataToWrite[6] = this.FLASH_ADDRESS[1];
            dataToWrite[7] = this.FLASH_ADDRESS[0];

            dataToWrite[8] = BitConverter.GetBytes(dataLen)[0];
            Array.Copy(dataArray, 0, dataToWrite, 9, dataArray.Length);
            dataToWrite[dataToWrite.Length - 1] = stopByte;

            crc = BitConverter.GetBytes(calculateCRC(dataToWrite, crcLen, 2));

            dataToWrite[dataToWrite.Length - 3] = crc[1];
            dataToWrite[dataToWrite.Length - 2] = crc[0];

            // send data to slave
            dataReceived = false;
            bytesToRead = 256;
            byte[] dataToRead = new byte[500];
            readBuffer = new byte[256];
            serialport.Write(dataToWrite, 2, this.LEN);

            //--------- for debug -------
            sendData = new byte[this.LEN];
            Array.Copy(dataToWrite, 2, sendData, 0, this.LEN);
            //---------------------------

            if (SendDataChanged != null)
            {
                sendData = new byte[this.LEN];
                Array.Copy(dataToWrite, 2, sendData, 0, this.LEN);
                SendDataChanged(this);
            }

            DateTime dateTimeSend = DateTime.Now;
            byte receivedCmd = 0xFF;
            while (receivedCmd != cmd
                & !(DateTime.Now.Ticks - dateTimeSend.Ticks > TimeSpan.TicksPerMillisecond * this.connectTimeout))
            {
                while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this.connectTimeout))
                    System.Threading.Thread.Sleep(1);
                dataToRead = new byte[500];
                Array.Copy(readBuffer, 0, dataToRead, 0, readBuffer.Length);
                receivedCmd = dataToRead[1];
            }
            if (receivedCmd != cmd)
            {
                dataToRead = new byte[500];
                throw new Exception($"Response CMD false, receivedCmd={receivedCmd}");
            }
            //else
            //    countRetries = 0;

            if (serialport != null)
            {
                crcLen = (ushort)(dataToRead[6] + 7);
                crc = BitConverter.GetBytes(calculateCRC(dataToRead, crcLen, 0));
                if ((crc[1] != dataToRead[crcLen] | crc[0] != dataToRead[crcLen + 1]) & dataReceived)
                {
                    if (NumberOfRetries <= countRetries)
                    {
                        countRetries = 0;
                        if (debug)
                        {
                            response = new byte[dataToRead[6] + 10];
                            Array.Copy(dataToRead, 0, response, 0, response.Length);
                            throw new Exception("Response CRC check failed"
                                + $"{BitConverter.ToString(response)}");
                        }
                        throw new Exception("Response CRC check failed");
                    }
                    else
                    {
                        countRetries++;
                        return InteractSlave(cmd, flashAddress, dataArray);
                    }
                }
                else if (!dataReceived)
                {
                    if (NumberOfRetries <= countRetries)
                    {
                        countRetries = 0;
                        throw new TimeoutException("No Response from Modbus Slave");
                    }
                    else
                    {
                        countRetries++;
                        return InteractSlave(cmd, flashAddress, dataArray);
                    }
                }
            }
            countRetries = 0;
            response = new byte[dataToRead[6] + 10];
            Array.Copy(dataToRead, 0, response, 0, response.Length);
            return response;
        }

        public bool HandShake(out byte[] ret)
        {
            ret = InteractSlave(0x00, 0, Array.Empty<byte>());
            return true;
        }

        public bool SendFinish()
        {
            InteractSlave(0x04, 0, Array.Empty<byte>());
            return true;
        }

        public bool FlashWipe(int address, out byte[] ret)
        {
            ret = InteractSlave(0x01, address, Array.Empty<byte>());
            if (ret.Length != 11)
            {
                return false;
            }

            if (address != BitConverter.ToInt32(new byte[] { ret[5], ret[4], ret[3], ret[2] }, 0))
            {
                return false;
            }

            if (ret[7] != 1)
            {
                return false;
            }
            return true;
        }

        public bool SendData(int address, byte[] dataArray, out byte[] ret)
        {
            ret = InteractSlave(0x02, address, dataArray);
            if (ret.Length != 11)
            {
                return false;
            }

            if (address != BitConverter.ToInt32(new byte[] { ret[5], ret[4], ret[3], ret[2] }, 0))
            {
                return false;
            }

            if (ret[7] != dataArray.Length)
            {
                return false;
            }
            return true;
        }

        public bool FlashZoneCRC(int address, UInt16 aLCrc, UInt16 dataLen, out byte[] ret)
        {
            var lenBytes = BitConverter.GetBytes(dataLen);
            ret = InteractSlave(0x03, address, new byte[] { lenBytes[1], lenBytes[0] });
            if (ret.Length != 14)
                return false;

            if (address != BitConverter.ToInt32(new byte[] { ret[5], ret[4], ret[3], ret[2] }, 0))
                return false;

            if (dataLen != BitConverter.ToUInt16(new byte[] { ret[8], ret[7] }, 0))
                return false;

            if (aLCrc != BitConverter.ToUInt16(new byte[] { ret[10], ret[9] }, 0))
                return false;

            return true;
        }

        public byte[] SendMassage(byte[] dataArray)
        {

            transactionIdentifierInternal++;
            if (serialport == null)
            {
                return Array.Empty<byte>();
            }

            if (!serialport.IsOpen)
            {
                throw new Exception("serial port not opened");
            }

            var dataLen = dataArray.Length;
            if (dataLen > 1024)
                throw new ArgumentException("data array length larger than 255");

            byte[] response;
            this.transactionIdentifier = BitConverter.GetBytes((uint)transactionIdentifierInternal);

            this.LEN = dataLen;
            ushort crcLen = Convert.ToUInt16(dataLen);


            byte[] dataBuffer = new byte[2 + dataArray.Length];
            // package data
            dataBuffer[0] = this.transactionIdentifier[1];
            dataBuffer[1] = this.transactionIdentifier[0];
            Array.Copy(dataArray, 0, dataBuffer, 2, dataArray.Length);


            //crc = BitConverter.GetBytes(calculateCRC(dataBuffer, crcLen, 2));
            //dataBuffer[dataBuffer.Length - 2] = crc[0];
            //dataBuffer[dataBuffer.Length - 1] = crc[1];

            // send data to slave by Serial Port

            dataReceived = false;
            bytesToRead = 256;
            readBuffer = new byte[256];
            serialport.Write(dataBuffer, 2, this.LEN);

            if (SendDataChanged != null)
            {
                sendData = new byte[this.LEN];
                Array.Copy(dataBuffer, 2, sendData, 0, this.LEN);
                SendDataChanged(this);
            }

            DateTime dateTimeSend = DateTime.Now;
            while (!(DateTime.Now.Ticks - dateTimeSend.Ticks > TimeSpan.TicksPerMillisecond * this.connectTimeout))
            {
                while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this.connectTimeout))
                    System.Threading.Thread.Sleep(1);
                if (dataReceived)
                {
                    break;
                }
                if (((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this.connectTimeout))
                {
                    throw new TimeoutException("operator timeout!!!!!");
                }
            }

            countRetries = 0;
            response = new byte[receiveData.Length];

            Array.Copy(receiveData, 0, response, 0, receiveData.Length);
            return response;
        }
        #endregion
    }
}
