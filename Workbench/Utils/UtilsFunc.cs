using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Prism.Ioc;
using Workbench.Models;
using PPEC.Communication.Enum;
using PPEC.Communication;
using System.Threading;
using Workbench.SerialAsistant.Utils;
using PPEC.Communication.Model;
using Workbench.Models.dw;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using Org.BouncyCastle.Utilities.Encoders;

namespace Workbench.Utils
{
    public static class UtilsFunc
    {

        public class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // 使用正则表达式匹配数字
                Regex regex = new Regex(@"\d+");

                // 提取x和y中的数字
                MatchCollection matchesX = regex.Matches(x);
                MatchCollection matchesY = regex.Matches(y);

                // 比较每对数字
                for (int i = 0; i < Math.Min(matchesX.Count, matchesY.Count); i++)
                {
                    int numX = int.Parse(matchesX[i].Value);
                    int numY = int.Parse(matchesY[i].Value);

                    if (numX != numY)
                    {
                        return numX.CompareTo(numY);
                    }
                }

                // 如果数字完全一样，那么按照原字符串比较
                return x.CompareTo(y);
            }
        }

        public static uint GetStrToUint(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            uint value = Convert.ToUInt32(hex, 16);
            return value;
        }

        public static byte[] HexStringToBytes(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);

            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("输入不能为空。");

            hex = hex.Replace(" ", ""); // 去掉空格
            if (hex.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数。");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }

        public static string BitsToHex(ulong raw, int bitLength)
        {
            if (bitLength <= 0) return "";

            // 计算需要的十六进制字符数（每4位一个字符）
            int hexDigits = (bitLength + 3) / 4;

            // 格式化为十六进制，并根据需要的位数截取
            string hex = raw.ToString($"X{hexDigits}");

            // 如果结果太长（可能发生在bitLength不是4的倍数时），取最后hexDigits个字符
            if (hex.Length > hexDigits)
            {
                hex = hex.Substring(hex.Length - hexDigits);
            }

            return hex;
        }
        public static UInt16 CalculateCRC(byte[] data, UInt16 numberOfBytes, int startByte)
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

        public static void UpdateLocalJson(string parentDirectoryFileName, object obj)
        {
            string updatedJsonContent = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if (File.Exists(parentDirectoryFileName))
                File.WriteAllText(parentDirectoryFileName, updatedJsonContent);
        }

        public static T GetLocalInfo<T>(string jsonFileName)
        {
            T jsonFileDes = default(T);
            if (!File.Exists(jsonFileName))
                return jsonFileDes;
            try
            {
                var json = File.ReadAllText(jsonFileName);
                jsonFileDes = JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {

            }
            return jsonFileDes;
        }

        public static void ChangeTheme(bool isDarkTheme)
        {
            Uri lightTheme = new Uri("pack://application:,,,/Workbench.Themes;component/Resource/Themes/Light.xaml", UriKind.Absolute);
            Uri darkTheme = new Uri("pack://application:,,,/Workbench.Themes;component/Resource/Themes/Dark.xaml", UriKind.Absolute);
            Uri blueTheme = new Uri("pack://application:,,,/Workbench.Themes;component/Resource/Themes/Blue.xaml", UriKind.Absolute);
            ResourceDictionary darkThemeDict = new ResourceDictionary() { Source = blueTheme };
            ResourceDictionary lightThemeDict = new ResourceDictionary() { Source = lightTheme };
            if (isDarkTheme)
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Remove(lightThemeDict);
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(darkThemeDict);
            }
            else
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Remove(darkThemeDict);
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(lightThemeDict);
            }
            var fileHandler = ContainerLocator.Container.Resolve<FileHandler>();
        }

        public static bool CheckPassword(Int32 p, ITopologyMaster CurrentTopoMaster)
        {
            if (p == 0)
                return false;
            //将密码发送到下位机解保护
            CurrentTopoMaster.SetValue(p, AddressName.PROTECTION_REGISTER_UNLOCK);
            Thread.Sleep(200);
            CurrentTopoMaster.SlaveToBufferBatch(AddressName.PASSWORD, 4);
            //解保护之后才能读取正确的密码，解保护一次后获取到的就一直是正确的密码
            var realPassword = CurrentTopoMaster.GetValue<Int32>(AddressName.PASSWORD, false);
            //解保护的密码和正确的密码进行比较
            var ulockPassword = CurrentTopoMaster.GetValue<Int32>(AddressName.PROTECTION_REGISTER_UNLOCK, false);
            return realPassword == ulockPassword;
        }

        internal static void PowerOffEvent()
        {
            //遍历打开的工程
            var projectManager = ContainerLocator.Container.Resolve<ProjectManager>();
            var portNames = SerialPortHelper.GetPortNames();
            foreach (var project in projectManager.OpenedProjectList)
            {
                foreach (var ppec in project.Children)
                {
                    if (!ppec.IsTrueConnected)
                        continue;
                    //如果串口没了
                    if (!portNames.Contains(ppec.LastPortName))
                    {
                        ppec.Disconnect();
                        if (projectManager.CurrentPPEC.UID == ppec.UID)
                            projectManager.SetCurrentPpec(ppec);
                    }
                }
            }
        }

        internal static async void PowerOnEvent()
        {
            //遍历打开的工程
            var projectManager = ContainerLocator.Container.Resolve<ProjectManager>();
            var portNames = SerialPortHelper.GetPortNames();
            foreach (var project in projectManager.OpenedProjectList)
            {
                foreach (var ppec in project.Children)
                {
                    if (ppec.IsTrueConnected || !ppec.Password.HasValue)
                        continue;
                    //如果最后一次连接串口在串口列表中
                    if (portNames.Contains(ppec.LastPortName))
                    {
                        ppec.PortName = ppec.LastPortName;
                        await projectManager.ConnectAsync(ppec);
                    }
                }
            }
        }

        public static (byte[] bytes, string hexStr) GetReadCommandByAddress(string addressHex, string commType)
        {
            switch (commType)
            {
                case Constants.OldSERIAL_PORT:
                case Constants.Modbus:
                    return GetReadCommandUsart(addressHex);
                default:
                    return ([], string.Empty);
            }
        }

        /// <summary>
        /// 根据寄存器十六进制地址获取读命令
        /// </summary>
        /// <param name="currentRegister"></param>
        /// <returns></returns>
        private static (byte[] bytes, string hexStr) GetReadCommandUsart(string addressHex)
        {
            string crc16 = Crc16CcittFalse.Calculate(addressHex);
            //D28C000AFFFFFFFFFFFFFF000AFF0003017050A9
            string command = $"D28C000AFFFFFFFFFFFFFF000AFF0002{addressHex}{crc16}";
            var data = Utility.HexToBytes(command);

            return (data, command);
        }

        internal static (byte[] bytes, string hexStr) GetWriteCommandByAddress(string addressHex, string communicationType, uint data)
        {
            string hex = Utility.DecToHex(data, false);
            string crc16 = Crc16CcittFalse.Calculate(addressHex + hex);
            string command = $"D28C000AFFFFFFFFFFFFFF000FFF0006{addressHex}{hex}{crc16}";
            var b = Utility.HexToBytes(command);
            return (b, command);

        }
        public static PpecProject FindNodeDfs(PpecProject root, string targetUid)
        {
            if (root == null) return null;
            if (root.UID == targetUid) return root;

            foreach (var child in root.Children)
            {
                PpecProject foundNode = FindNodeDfs(child, targetUid);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }
            return null;
        }

        public static PpecProject FindNodeDfs(List<PpecProject> nodeList, string targetUid)
        {
            if (nodeList == null) return null;

            // 遍历列表中的每个根节点
            foreach (var rootNode in nodeList)
            {
                // 对每个根节点启动递归搜索
                var foundNode = FindRecursive(rootNode, targetUid);
                if (foundNode != null)
                {
                    // 一旦找到，立即返回
                    return foundNode;
                }
            }

            // 遍历完所有树都未找到
            return null;
        }

        private static PpecProject FindRecursive(PpecProject currentNode, string targetUid)
        {
            if (currentNode == null) return null;

            // 检查当前节点
            if (currentNode.UID == targetUid)
            {
                return currentNode;
            }

            // 递归搜索所有子节点
            foreach (var child in currentNode.Children)
            {
                var foundInChild = FindRecursive(child, targetUid);
                if (foundInChild != null)
                {
                    return foundInChild;
                }
            }

            return null;
        }
        public static double GetValueForFormula(FormulaEnum formula, double paramA, double paramB, uint value)
        {
            double result = 1 * value;
            if (formula == FormulaEnum.None)
            {
                return result;
            }

            switch (formula)
            {
                case FormulaEnum.Add:
                    result = paramA * value + paramB;
                    break;
                case FormulaEnum.Sub:
                    result = paramA * value - paramB;
                    break;
                case FormulaEnum.Mul:
                    result = paramA * value * paramB;
                    break;
                case FormulaEnum.Exc:
                    result = paramA * value / paramB;
                    break;
            }
            return result;
        }
        public static void SerachCategoryNode(ObservableCollection<CategoryTree> SingleParamTrees, ValueLabelOption NodeCategory)
        {
            var serachNode = SingleParamTrees.Where(x => x.Title == NodeCategory.Label).FirstOrDefault();

            if (serachNode == null) return;
            CollapseAll(SingleParamTrees);

            for (var p = serachNode; p != null; p = p.Parent)
                p.IsExpanded = true;

            ExpandDescendants(serachNode);
            serachNode.IsSelected = true;
        }
        private static void CollapseAll(IEnumerable<CategoryTree> nodes)
        {
            foreach (var n in nodes)
            {
                n.IsExpanded = false;
                if (n.Children != null && n.Children.Any())
                    CollapseAll(n.Children);
            }
        }
        private static void ExpandDescendants(CategoryTree node)
        {
            node.IsExpanded = true;
            if (node.Children != null && node.Children.Any())
                foreach (var child in node.Children)
                    ExpandDescendants(child);
        }

        public static void SyncTreeCheckNode(IEnumerable<CategoryTree> currentTreeNode, IEnumerable<RegisterAddrInfo> IsCheckAdd)
        {
            if (IsCheckAdd == null || IsCheckAdd.Count() == 0)
                return;
            foreach (var treeNode in currentTreeNode)
            {
                if (IsCheckAdd != null && IsCheckAdd.Count() > 0 && IsCheckAdd.Where(x => x.Name == treeNode.Title).FirstOrDefault() != null)
                {
                    treeNode.IsCheck = true;
                }
            }
        }

        public static void WriteUInt16(ushort v, EndianMode mode, IList<byte> buf)
        {
            byte hi = (byte)(v >> 8);
            byte lo = (byte)(v & 0xFF);
            switch (mode)
            {
                case EndianMode.BigEndian: buf.Add(hi); buf.Add(lo); break;     // AB
                case EndianMode.LittleEndian: buf.Add(lo); buf.Add(hi); break;     // BA
                case EndianMode.ByteSwapInWord: buf.Add(lo); buf.Add(hi); break;     // 同Little（对16位）
                case EndianMode.WordSwap: buf.Add(hi); buf.Add(lo); break;     // 对16位“词交换”无意义=BE
            }
        }

        public static void WriteUInt32(uint v, EndianMode mode, IList<byte> buf)
        {
            byte a = (byte)((v >> 24) & 0xFF);
            byte b = (byte)((v >> 16) & 0xFF);
            byte c = (byte)((v >> 8) & 0xFF);
            byte d = (byte)(v & 0xFF);
            switch (mode)
            {
                case EndianMode.BigEndian: buf.Add(a); buf.Add(b); buf.Add(c); buf.Add(d); break; // ABCD
                case EndianMode.LittleEndian: buf.Add(d); buf.Add(c); buf.Add(b); buf.Add(a); break; // DCBA
                case EndianMode.WordSwap: buf.Add(c); buf.Add(d); buf.Add(a); buf.Add(b); break; // CDAB
                case EndianMode.ByteSwapInWord: buf.Add(b); buf.Add(a); buf.Add(d); buf.Add(c); break; // BADC
            }
        }

        public static void WriteFloat(float f, EndianMode mode, IList<byte> buf)
        {
            var bytes = BitConverter.GetBytes(f); // Windows下默认 LE: [d c b a]
                                                  // 统一转换成 ABCD 再按模式排列
            byte a, b, c, d;
            if (BitConverter.IsLittleEndian)
            {
                d = bytes[0]; c = bytes[1]; b = bytes[2]; a = bytes[3];
            }
            else
            {
                a = bytes[0]; b = bytes[1]; c = bytes[2]; d = bytes[3];
            }
            WriteUInt32((uint)((a << 24) | (b << 16) | (c << 8) | d), mode, buf);
        }
    }
}
