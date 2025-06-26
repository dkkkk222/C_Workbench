using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Workbench.Utils.ControlCANHelper64;
using System.Windows;
using Workbench.Utils.CAN;

namespace Workbench.Utils
{
    public class CANFlashUpgradeClient
    {
        private static byte startByte = 0x5a;
        private static byte stopByte = 0xa5;
        private const byte FrameDataLength = 8;
        private const uint UpgradeSendID = 0x90000100; // 此处为CAN通讯发送的ID
        private const uint UpgradeReceiveID = 0x10000200; // 此处为CAN通讯接收的ID，开头为9时接收会是1

        private const uint JumpToBootSendID = 0x10000600; // 跳转至Boot的ID

        private ControlCAN _controlCAN;

        public CANFlashUpgradeClient(ControlCAN controlCAN)
        {
            _controlCAN = controlCAN;
        }

        #region Properties
        private bool _isConnectd = false;
        public bool IsConnected
        {
            get { return _isConnectd; }
        }

        #endregion

        #region Methods
        public void Connect()
        {
            _isConnectd = false;
            if (!_controlCAN.OpenCAN64())
            {
                MessageBox.Show("打开设备失败,请检查设备类型和设备索引号是否正确", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!_controlCAN.StartCan64(_controlCAN.GetCurrentCanId()))
            {
                return;
            }

            _isConnectd = true;
        }

        public void DisConnect()
        {
            _controlCAN.CloseCAN();
            _isConnectd = false;
        }

        #endregion

        #region Upgrade Method

        private byte[] GetSendBytesData(byte cmd, Int32 flashAddress, byte[] dataArray)
        {
            if (cmd > 0x05 | cmd < 0x00)
            {
                throw new ArgumentException("cmd must be 0x00 - 0x05;");
            }
            var dataLen = dataArray.Length;
            ushort crcLen = Convert.ToUInt16(dataLen + 7);
            List<byte> bytes = new List<byte>();
            bytes.Add(startByte);
            bytes.Add(cmd);
            var address = BitConverter.GetBytes(flashAddress);
            bytes.Add(address[3]);
            bytes.Add(address[2]);
            bytes.Add(address[1]);
            bytes.Add(address[0]);
            bytes.Add((byte)dataLen);
            bytes.AddRange(dataArray);
            var crc = BitConverter.GetBytes(UtilsFunc.CalculateCRC(bytes.ToArray(), crcLen, 0));
            bytes.Add(crc[1]);
            bytes.Add(crc[0]);
            bytes.Add(stopByte);
            return bytes.ToArray();
        }

        unsafe public byte[] InteractData(byte cmd, Int32 flashAddress, byte[] dataArray)
        {
            // 要考虑分包发送的问题
            var datas = GetSendBytesData(cmd, flashAddress, dataArray);
            int blockCount = datas.Length / FrameDataLength;
            int lastSize = datas.Length % FrameDataLength;

            List<VCI_CAN_OBJ> list = new List<VCI_CAN_OBJ>();
            uint upgradeSendID = UpgradeSendID;
            for (int i = 1; i <= blockCount; i++)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, (i - 1) * FrameDataLength, temp, 0, FrameDataLength);
                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = upgradeSendID;
                frame.Data[0] = datas[0];
                frame.Data[1] = datas[1];
                frame.Data[2] = datas[2];
                frame.Data[3] = datas[3];
                frame.Data[4] = datas[4];
                frame.Data[5] = datas[5];
                frame.Data[6] = datas[6];
                frame.Data[7] = datas[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = 8;

                list.Add(frame);
                upgradeSendID++;
            }
            if (lastSize != 0)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, blockCount * FrameDataLength, temp, 0, lastSize);
                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = upgradeSendID;
                frame.Data[0] = datas[0];
                frame.Data[1] = datas[1];
                frame.Data[2] = datas[2];
                frame.Data[3] = datas[3];
                frame.Data[4] = datas[4];
                frame.Data[5] = datas[5];
                frame.Data[6] = datas[6];
                frame.Data[7] = datas[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = FrameDataLength;

                list.Add(frame);
            }

            _controlCAN.Transmit64(list);

            // 接收并校验返回值
            var dataBuffer = Read(ResponeLength(cmd));
            if (dataBuffer.Length < 2 || dataBuffer[1] != cmd)
            {
                throw new Exception($"Response CMD false, receivedCmd={BitConverter.ToString(dataBuffer)}");
            }
            return dataBuffer;
        }

        unsafe private byte[] Read(int count, int timeout = 1000)
        {
            int numBytesRead = 0;
            DateTime dateTime = DateTime.Now;
            List<byte> list = new List<byte>();
            while (numBytesRead < count && (DateTime.Now - dateTime).TotalMilliseconds < timeout)
            {
                if (_controlCAN.HasReceive())
                {
                    dateTime = DateTime.Now;
                    var data = _controlCAN.Receive64();
                    foreach (var item in data)
                    {
                        if (item.ID == UpgradeReceiveID)
                        {
                            numBytesRead += item.DataLen;
                            byte[] tempD = new byte[item.DataLen];
                            tempD[0] = item.Data[0];
                            tempD[1] = item.Data[1];
                            tempD[2] = item.Data[2];
                            tempD[3] = item.Data[3];
                            tempD[4] = item.Data[4];
                            tempD[5] = item.Data[5];
                            tempD[6] = item.Data[6];
                            tempD[7] = item.Data[7];
                            list.AddRange(tempD);
                        }
                    }
                }
            }
            if (numBytesRead > count)
            {
                list.RemoveRange(count, numBytesRead - count);
            }
            return list.ToArray();
        }

        private int ResponeLength(byte cmd)
        {
            switch (cmd)
            {
                case 0x00:
                    return 10;
                case 0x01:
                    return 11;
                case 0x02:
                    return 11;
                case 0x03:
                    return 14;
            }
            return 0;
        }

        // 编写各个协议的代码，例如握手等

        public bool HandShake(out byte[] ret)
        {
            byte cmd = 0x00;
            ret = InteractData(cmd, 0, Array.Empty<byte>());
            if (ret.Length != ResponeLength(cmd))
            {
                return false;
            }
            return true;
        }

        public bool SendFinish()
        {
            InteractData(0x04, 0, Array.Empty<byte>());
            return true;
        }

        public bool FlashWipe(int address, out byte[] ret)
        {
            byte cmd = 0x01;
            ret = InteractData(cmd, address, Array.Empty<byte>());
            if (ret.Length != ResponeLength(cmd))
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
            byte cmd = 0x02;
            ret = InteractData(cmd, address, dataArray);
            if (ret.Length != ResponeLength(cmd))
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

        public bool FlashZoneCRC(int address, UInt16 aLCrc, int dataLen, out byte[] ret)
        {
            byte cmd = 0x03;
            var lenBytes = BitConverter.GetBytes(dataLen);
            ret = InteractData(cmd, address, new byte[] { lenBytes[1], lenBytes[0] });
            if (ret.Length != ResponeLength(cmd))
                return false;

            if (address != BitConverter.ToInt32(new byte[] { ret[5], ret[4], ret[3], ret[2] }, 0))
                return false;

            if (dataLen != BitConverter.ToUInt16(new byte[] { ret[8], ret[7] }, 0))
                return false;

            if (aLCrc != BitConverter.ToUInt16(new byte[] { ret[10], ret[9] }, 0))
                return false;

            return true;
        }

        unsafe public void SendToBootMassage(byte[] dataArray)
        {
            // 要考虑分包发送的问题
            var datas = dataArray;
            int blockCount = datas.Length / FrameDataLength;
            int lastSize = datas.Length % FrameDataLength;

            List<VCI_CAN_OBJ> list = new List<VCI_CAN_OBJ>();
            uint upgradeSendID = JumpToBootSendID;
            for (int i = 1; i <= blockCount; i++)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, (i - 1) * FrameDataLength, temp, 0, FrameDataLength);

                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = upgradeSendID;
                frame.Data[0] = temp[0];
                frame.Data[1] = temp[1];
                frame.Data[2] = temp[2];
                frame.Data[3] = temp[3];
                frame.Data[4] = temp[4];
                frame.Data[5] = temp[5];
                frame.Data[6] = temp[6];
                frame.Data[7] = temp[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = FrameDataLength;

                list.Add(frame);

                upgradeSendID++;
            }
            if (lastSize != 0)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, blockCount * FrameDataLength, temp, 0, lastSize);

                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = upgradeSendID;
                frame.Data[0] = temp[0];
                frame.Data[1] = temp[1];
                frame.Data[2] = temp[2];
                frame.Data[3] = temp[3];
                frame.Data[4] = temp[4];
                frame.Data[5] = temp[5];
                frame.Data[6] = temp[6];
                frame.Data[7] = temp[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = FrameDataLength;
                list.Add(frame);
            }

            _controlCAN.Transmit64(list);
        }

        unsafe public byte[] SendMassage(byte[] dataArray, uint sendId)
        {
            // 要考虑分包发送的问题
            var datas = dataArray;
            int blockCount = datas.Length / FrameDataLength;
            int lastSize = datas.Length % FrameDataLength;

            List<VCI_CAN_OBJ> list = new List<VCI_CAN_OBJ>();
            uint messageSendID = sendId;
            for (int i = 1; i <= blockCount; i++)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, (i - 1) * FrameDataLength, temp, 0, FrameDataLength);


                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = messageSendID;
                frame.Data[0] = temp[0];
                frame.Data[1] = temp[1];
                frame.Data[2] = temp[2];
                frame.Data[3] = temp[3];
                frame.Data[4] = temp[4];
                frame.Data[5] = temp[5];
                frame.Data[6] = temp[6];
                frame.Data[7] = temp[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = FrameDataLength;

                list.Add(frame);
                messageSendID++;
            }
            if (lastSize != 0)
            {
                byte[] temp = new byte[FrameDataLength];
                Buffer.BlockCopy(datas, blockCount * FrameDataLength, temp, 0, lastSize);

                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = messageSendID;
                frame.Data[0] = temp[0];
                frame.Data[1] = temp[1];
                frame.Data[2] = temp[2];
                frame.Data[3] = temp[3];
                frame.Data[4] = temp[4];
                frame.Data[5] = temp[5];
                frame.Data[6] = temp[6];
                frame.Data[7] = temp[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = FrameDataLength;

                list.Add(frame);
            }

            _controlCAN.Transmit64(list);

            // 接收并校验返回值
            var dataBuffer = Read(3, 2000);
            if (dataBuffer.Length < 2)
            {
                throw new TimeoutException($"Response Error, receivedCmd={BitConverter.ToString(dataBuffer)}");
            }
            return dataBuffer;
        }

        #endregion

    }
}
