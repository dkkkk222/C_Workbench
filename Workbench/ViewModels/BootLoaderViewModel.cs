using log4net;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Unity;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.BootLoader;
using Workbench.Models.Enums;
using Workbench.Utils;
using Workbench.Utils.CAN;
using Workbench.Utils.Common;
using static Workbench.Utils.ControlCANHelper64;

namespace Workbench.ViewModels
{
    public class BootLoaderViewModel : BindableBase, IDialogAware
    {
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(BootLoaderViewModel));
        public BootLoaderViewModel(IEventAggregator eventAggregator, ProjectManager projectManager, FileHandler fileHandler)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<SerialPortAddRemoveEvent>().Subscribe(() =>
            {
                var serialPortIndex = SerialPortIndex;
                PortList.Clear();
                PortList.AddRange(SerialPortHelper.GetPortNames());
                SerialPortIndex = serialPortIndex >= _portList.Count() ? 0 : serialPortIndex;
            }, ThreadOption.UIThread);
            PortList.AddRange(SerialPortHelper.GetPortNames());

            var upgradeJson = fileHandler.ReadLocalFileObject<Upgrade>("upgrade.json");
            if (upgradeJson != null)
            {
                var host = upgradeJson.Host;
                _baseUrl = host.EndsWith("/") ? host : host + "/";
            }
        }

        #region Properties

        public string SConnect => "连接";
        public string SDisConnect => "断开";
        private uint _canInd = 0; // CAN通道
        private uint _deviceInd = 0; // 设备索引号
        private uint _deviceType = Device64.VCI_USBCAN_2E_U; //Device.VCI_USBCAN2; // 设备类型号
        private const uint JumpToBootSendID = 0x10000600; // 跳转至Boot的ID
        private byte[] _enterBootloader = { 0x5A, 0xA5, 0xC3, 0x3C, 0x00, 0x00, 0x00, 0x00 };
        private byte[] _enterBootloader1 = { 0x01, 0x10, 0x00, 0x1E, 0x00, 0x01, 0x02, 0x5A, 0xA5, 0x5F, 0x35 };
        private byte[] _enterBootloader2 = { 0x01, 0x10, 0x00, 0x1F, 0x00, 0x01, 0x02, 0xC3, 0x3C, 0xF4, 0xDE };
        private byte[] _chipStateQuery = { 0x01, 0x03, 0x00, 0x7B, 0x00, 0x01, 0xF4, 0x13 };
        private ControlCAN controlCAN = null;
        private int _recordChipModel = 0;
        public CANCommConfig CANCommConfig;
        public CANFlashUpgradeClient CanClient;
        private string _directory = System.Environment.CurrentDirectory + @"\\DownLoad\\";
        private string _filePath = "";
        private string _baseUrl = "";


        private ObservableCollection<ComConnectType> _comConnectTypes = CommEntity.ComConnectTypeList;
        public ObservableCollection<ComConnectType> ComConnectTypes
        {
            get { return _comConnectTypes; }
            set { SetProperty(ref _comConnectTypes, value); }
        }

        private int _BBLLCConnectTypeListIndex = 0;
        public int BBLLCConnectTypeListIndex
        {
            get { return _BBLLCConnectTypeListIndex; }
            set
            {
                value = value == -1 ? 0 : value;
                SetProperty(ref _BBLLCConnectTypeListIndex, value);
            }
        }

        private ObservableCollection<string> _portList = new ObservableCollection<string>();
        public ObservableCollection<string> PortList
        {
            get { return _portList; }
            set { SetProperty(ref _portList, value); }
        }

        private int _serialPortIndex = 0;
        public int SerialPortIndex
        {
            get => _serialPortIndex;
            set => SetProperty(ref _serialPortIndex, value);
        }

        private string[] _updateModeItemSource = new string[] { "本地文件升级", "云端升级" };

        /// <summary>
        /// 升级方式可选择的项
        /// </summary>
        public string[] UpdateModeItemSource
        {
            get => _updateModeItemSource;
            set => SetProperty(ref _updateModeItemSource, value);
        }

        private int _updateModeSelectedIndex = 0;
        /// <summary>
        /// 升级方式选择的索引
        /// </summary>
        public int UpdateModeSelectedIndex
        {
            get => _updateModeSelectedIndex;
            set => SetProperty(ref _updateModeSelectedIndex, value);
        }

        private bool _isConnect = false;
        public bool IsConnect
        {
            get => _isConnect;
            set => SetProperty(ref _isConnect, value);
        }

        private bool isUpgrading;
        public bool IsUpgrading
        {
            get => isUpgrading;
            set => SetProperty(ref isUpgrading, value);
        }

        private UpdateTopoEnum _updateTopoEnum = UpdateTopoEnum.移相全桥;
        public UpdateTopoEnum UpdateTopoEnum
        {
            get => _updateTopoEnum;
            set => SetProperty(ref _updateTopoEnum, value);
        }

        private string fileName;
        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public string _IsShowLoading = "Collapsed";
        public string IsShowLoading
        {
            get => _IsShowLoading;
            set => SetProperty(ref _IsShowLoading, value);
        }

        private double processValue;
        public double ProcessValue
        {
            get => processValue;
            set => SetProperty(ref processValue, value);
        }

        public ObservableCollection<BBLLCCANItem> BBLLCCANList => CommEntity.BBLLCCANList;
        public int _BBLLCCANListIndex;
        public int BBLLCCANListIndex
        {
            get => _BBLLCCANListIndex;
            set
            {
                _BBLLCCANListIndex = value;
                RaisePropertyChanged();
            }
        }

        private FlashUpgradeClient flashClient;

        #endregion

        private HexDataContainer hexDataContainer;
        private CANHexDataContainer canHexDataContainer;


        #region Command

        private DelegateCommand _loadFileCommand;
        public DelegateCommand LoadFileCommand =>
            _loadFileCommand ?? (_loadFileCommand = new DelegateCommand(() =>
            {
                if (BBLLCConnectTypeListIndex == 0)
                {
                    hexDataContainer = new HexDataContainer();
                    OpenFileDialog openFile = new OpenFileDialog
                    {
                        Filter = "Files (*.ppecbl)|*.ppecbl|All Files (*.*)|*.*"
                    };
                    if (openFile.ShowDialog() != true) return;
                    Task.Run(() =>
                    {
                        hexDataContainer.LoadHexFile(openFile.FileName);
                        if (hexDataContainer.UpgradeFileTopo == UpdateTopoEnum.None)
                        {
                            MessageBox.Show("升级文件型号有误，请确保升级文件符合条件!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            FileName = openFile.FileName;
                            _log.Info($"文件加载完成:{openFile.FileName}");
                            IsUpgrading = false;
                        });
                    });
                }
                else if (BBLLCConnectTypeListIndex == 1)
                {
                    canHexDataContainer = new CANHexDataContainer();
                    OpenFileDialog openFile = new OpenFileDialog
                    {
                        Filter = "Files (*.ppecbl)|*.ppecbl|All Files (*.*)|*.*"
                    };
                    if (openFile.ShowDialog() != true) return;
                    Task.Run(() =>
                    {
                        canHexDataContainer.LoadHexFile(openFile.FileName);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            FileName = openFile.FileName;
                            //Log.Info($"文件加载完成:{openFile.FileName}");
                            IsUpgrading = true;
                        });
                    });
                }
            }));

        public DelegateCommand ConnectCommand => new DelegateCommand(() =>
        {
            if (BBLLCConnectTypeListIndex == 1)
            {//CAN口
                if (!IsConnect)
                {
                    _canInd = (uint)BBLLCCANListIndex;
                    ConnectAction(null, _canInd);

                    IsConnect = true;

                    try
                    {
                        SendBootJump(_enterBootloader, JumpToBootSendID);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + "\n" + e.StackTrace, "错误",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        controlCAN?.CloseCAN();
                        return;
                    }
                    CanClient = new CANFlashUpgradeClient(controlCAN);

                }
            }
            else if (BBLLCConnectTypeListIndex == 0)
            {//串口
                if (!IsConnect)
                {
                    ConnectAction();
                }
                else
                {
                    DisFlash();
                    IsConnect = false;
                    _log.Info("断开连接");
                    IsUpgrading = false;
                }
            }

        });

        public DelegateCommand UpgradeCommand => new DelegateCommand(() =>
        {
            if (UpdateModeSelectedIndex == 0 && string.IsNullOrEmpty(FileName))
            {
                MessageBox.Show("当前未载入本地文件，请载入后再升级!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            IsShowLoading = "Visible";
            if (BBLLCConnectTypeListIndex == 0)
            {
                Task.Run(() =>
                {
                    int version2;
                    string selectLog;
                    if (UpdateModeSelectedIndex == 0)
                    {
                        version2 = (int)hexDataContainer.UpgradeFileTopo;
                        selectLog = "升级文件";
                    }
                    else
                    {
                        version2 = (int)UpdateTopoEnum;
                        selectLog = "升级拓扑选择项";
                    }
                    var result = JudgeChipMethod(selectLog, version2);
                    IsShowLoading = "Collapsed";
                    return result;
                }).ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        UpgradeAction();
                    }
                });
            }
            else if (BBLLCConnectTypeListIndex == 1)
            {
                bool ret; byte[] response;
                try
                {
                    ret = CanClient.HandShake(out response);
                    if (!ret)
                    {
                        IsShowLoading = "Collapsed";
                        //Log.Info("升级失败");
                        //Log.Info($"response = {BitConverter.ToString(response)}");
                        //CanUpgrade = true;
                        MessageBox.Show("升级失败", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    Task.Run(() => { SendAllDataCAN(); });
                }
                catch (Exception ex) { }

            }

        });

        #endregion

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;


        #region Method

        public void SendAllDataCAN()
        {
            if (canHexDataContainer == null)
                return;

            var dn = canHexDataContainer.HexDatas.Count;
            bool ret = true;
            double total = canHexDataContainer.HexDatas.Select(_ => _.Data.Length).Sum();
            byte[] response;
            try
            {
                foreach (var item in canHexDataContainer.HexDatas)
                {
                    ret = SendFlashDataCAN(item, total, out response);
                    if (!ret)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _log.Info("升级失败");
                            _log.Info($"respone: {BitConverter.ToString(response)}");
                            IsUpgrading = false;
                        });
                        return;
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (UpdateModeSelectedIndex == 1)
                    {
                        DeleteDirectory(_directory);
                    }
                    // 发送SendFinish()
                    //DisFlash();
                    IsConnect = false;
                    IsUpgrading = false;
                    _log.Info("升级成功");
                    MessageBox.Show("升级成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() => _log.Info("升级失败"));
                _log.Error(e.Message + "\n" + e.StackTrace);
                IsUpgrading = false;
            }
        }

        private void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        public bool SendFlashDataCAN(CANHexData hexData, double total, out byte[] response)
        {
            bool ret = CanClient.FlashWipe(hexData.Address, out response);
            if (!ret)
                return false;
            int idx = 0;
            while (idx < hexData.Data.Length)
            {
                Thread.Sleep(10);
                var len = idx + 128 < hexData.Data.Length
                    ? 128 : hexData.Data.Length - idx;
                byte[] sendData = new byte[len];
                Array.Copy(hexData.Data, idx, sendData, 0, len);
                ret = CanClient.SendData(hexData.Address + idx / 2, sendData, out response);
                if (!ret)
                    return false;
                idx += len;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProcessValue += len * 100.0 / total;
                });
            }
            Thread.Sleep(10);
            ret = CanClient.FlashZoneCRC(hexData.Address, hexData.Crc, hexData.Len, out response);
            return ret;
        }

        private void UpgradeAction()
        {
            ProcessValue = 0.0;
            IsUpgrading = true;
            byte[] response;
            bool ret;
            if (UpdateModeSelectedIndex == 0)
            {
                try
                {
                    ret = flashClient.HandShake(out response);
                    if (!ret)
                    {
                        _log.Info("升级失败");
                        _log.Info($"response = {BitConverter.ToString(response)}");
                        IsUpgrading = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _log.Info("升级失败");
                    _log.Info($"{ex.Message}");
                    _log.Info($"{ex.StackTrace}");
                    IsUpgrading = false;
                    return;
                }
                Task.Run(() => { SendAllData(); });
            }
            else
            {
                // 先从网络获取文件，然后再执行后续操作
                string name = "PSFB";
                switch (UpdateTopoEnum)
                {
                    case UpdateTopoEnum.移相全桥:
                        name = "PSFB";
                        break;
                    case UpdateTopoEnum.LC:
                        name = "LC";
                        break;
                    case UpdateTopoEnum.LLC:
                        name = "LLC";
                        break;
                    case UpdateTopoEnum.单相逆变整流:
                        name = "SPRI";
                        break;
                    case UpdateTopoEnum.三相逆变整流:
                        name = "TPRI";
                        break;
                    case UpdateTopoEnum.DAB:
                        name = "DAB";
                        break;
                }
                string url = $"{_baseUrl}";
                DownloadFileFromServer(url, name);
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">下载地址</param>
        private void DownloadFileFromServer(string url, string name)
        {
            try
            {
                string fileName = "PPEC_" + name + ".ppecbl";
                if (!Directory.Exists(_directory))
                {
                    Directory.CreateDirectory(_directory);
                }
                _filePath = Path.Combine(_directory, fileName);
                url = url + fileName;
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileCompleted += WebClientDownloadCompleted;
                    client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                    client.DownloadFileAsync(new Uri(url), _filePath);
                }
            }
            catch
            {
                // 删除文件夹
                DeleteDirectory(_directory);
                // 从服务器下载文件失败
                IsUpgrading = false;
            }
        }

        private void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Download status: {0}%.", e.ProgressPercentage);
        }

        private void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // 下载文件过程中出现异常
                _log.Info("从服务器下载文件出现异常，" + e.Error.Message);
                DeleteDirectory(_directory);
                IsUpgrading = false;
                return;
            }
            // 下载完成
            hexDataContainer = new HexDataContainer();
            if (!File.Exists(_filePath))
            {
                // 未找到刚下载的文件
                IsUpgrading = false;
                return;
            }
            hexDataContainer.LoadHexFile(_filePath);
            if (hexDataContainer.UpgradeFileTopo == UpdateTopoEnum.None)
            {
                MessageBox.Show("升级的文件型号有误，请与管理员联系!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsUpgrading = false;
                return;
            }
            byte[] response;
            bool ret;
            try
            {
                ret = flashClient.HandShake(out response);
                if (!ret)
                {
                    _log.Info("升级失败");
                    _log.Info($"response = {BitConverter.ToString(response)}");
                    IsUpgrading = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Info("升级失败");
                _log.Info($"{ex.Message}");
                _log.Info($"{ex.StackTrace}");
                IsUpgrading = false;
                return;
            }
            Task.Run(() => { SendAllData(); });
        }

        public void SendAllData()
        {
            if (hexDataContainer == null)
                return;

            var dn = hexDataContainer.HexDatas.Count;
            bool ret = true;
            double total = hexDataContainer.HexDatas.Select(_ => _.Data.Length).Sum();
            byte[] response;
            try
            {
                foreach (var item in hexDataContainer.HexDatas)
                {
                    ret = SendFlashData(item, total, out response);
                    if (!ret)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _log.Info("升级失败");
                            _log.Info($"respone: {BitConverter.ToString(response)}");
                            IsUpgrading = false;
                        });
                        return;
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (UpdateModeSelectedIndex == 1)
                    {
                        DeleteDirectory(_directory);
                    }
                    // 发送SendFinish()
                    DisFlash();
                    IsConnect = false;
                    IsUpgrading = false;
                    _log.Info("升级成功");
                    MessageBox.Show("升级成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() => _log.Info("升级失败"));
                _log.Error(e.Message + "\n" + e.StackTrace);
                IsUpgrading = false;
            }
        }

        public bool SendFlashData(HexData hexData, double total, out byte[] response)
        {
            bool ret = flashClient.FlashWipe(hexData.Address, out response);
            if (!ret)
                return false;
            int idx = 0;
            while (idx < hexData.Data.Length)
            {
                Thread.Sleep(10);
                var len = idx + 128 < hexData.Data.Length
                    ? 128 : hexData.Data.Length - idx;
                byte[] sendData = new byte[len];
                Array.Copy(hexData.Data, idx, sendData, 0, len);
                ret = flashClient.SendData(hexData.Address + idx / 2, sendData, out response);
                if (!ret)
                    return false;
                idx += len;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProcessValue += len * 100.0 / total;
                });
            }
            Thread.Sleep(10);
            ret = flashClient.FlashZoneCRC(hexData.Address, hexData.Crc, hexData.Len, out response);
            return ret;
        }

        /// <summary>
        /// 递归实现芯片是否进入boot
        /// </summary>
        /// <param name="selectLog"></param>
        /// <param name="version2"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private bool JudgeChipMethod(string selectLog, int version2, int count = 0)
        {
            try
            {
                var tcs = UtilsFunc.GetTopoChipStatus(flashClient.SendMassage(_chipStateQuery));
                if (tcs == null)
                {
                    _log.Info("无法获取到芯片状态，升级失败");
                    return false;
                }
                _recordChipModel = tcs.Version;
                if (_recordChipModel < 1 || _recordChipModel > 8)
                {
                    _log.Info($"获取到的芯片版本号{_recordChipModel}不在可升级范围内，无法进行升级");
                    return false;
                }
                if (tcs.CurrentChipState == CurrentChipStateEnum.App)
                {
                    if (count == -1)
                    {
                        _log.Info($"芯片未进入BOOT状态，无法升级，请重新检查！");
                        return false;
                    }
                    if (JudgeChipModel(tcs.Version, version2))
                    {
                        // 发送进入bootloader状态的指令
                        flashClient.SendMassage(_enterBootloader1);
                        flashClient.SendMassage(_enterBootloader2);

                        return JudgeChipMethod(selectLog, version2, --count);
                    }
                    else
                    {
                        _log.Info($"App状态下，{selectLog}与芯片型号不匹配，无法升级，请重新检查!");
                        return false;
                    }
                }
                else if (tcs.CurrentChipState == CurrentChipStateEnum.Boot)
                {
                    if (JudgeChipModel(tcs.Version, version2))
                    {
                        return true;
                    }
                    else
                    {
                        _log.Info($"Boot状态下，{selectLog}与芯片型号不匹配，无法升级，请重新检查!");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                flashClient.Disconnect();
                IsConnect = false;
                _log.Info("升级失败");
                _log.Info(ex.Message.ToString());
                return false;
            }
        }

        private bool JudgeChipModel(int version1, int version2)
        {
            if (version1 == version2)
            {
                return true;
            }
            return false;
        }

        private void DisFlash()
        {
            try
            {
                if (flashClient != null)
                {
                    flashClient.ConnectionTimeout = 500;
                    flashClient.SendFinish();
                }
            }
            catch (Exception e) { }
            if (flashClient != null && flashClient.Connected)
                flashClient.Disconnect();
        }

        unsafe private void SendBootJump(byte[] data, uint id)
        {
            if (IsConnect)
            {
                VCI_CAN_OBJ frame = new VCI_CAN_OBJ();
                frame.ID = id;
                frame.Data[0] = data[0];
                frame.Data[1] = data[1];
                frame.Data[2] = data[2];
                frame.Data[3] = data[3];
                frame.Data[4] = data[4];
                frame.Data[5] = data[5];
                frame.Data[6] = data[6];
                frame.Data[7] = data[7];
                frame.TimeFlag = 0;
                frame.TimeStamp = 0;
                frame.RemoteFlag = 0;
                frame.ExternFlag = 1;
                frame.DataLen = 8;

                controlCAN.Transmit64(frame);
            }
        }

        public void ConnectAction(string portName = null, UInt32 canId = 0)
        {
            StartBootLoader(portName, canId);
            if (BBLLCConnectTypeListIndex == 0)
            {
                try
                {
                    flashClient.Connect();
                    IsConnect = true;
                }
                catch (Exception ex)
                {
                    flashClient.Disconnect();
                    _log.Info("连接失败");
                    _log.Info(ex.Message.ToString());
                    IsConnect = false;
                }
            }
            else if (BBLLCConnectTypeListIndex == 1)
            {
                try
                {
                    CanClient.Connect();
                    IsConnect = true;
                }
                catch (Exception ex)
                {
                    CanClient.DisConnect();
                    _log.Info("连接失败");
                    _log.Info(ex.Message.ToString());
                    IsConnect = false;
                }
            }

        }

        private void StartBootLoader(string portName = null, UInt32 canId = 0)
        {
            if (BBLLCConnectTypeListIndex == 0)
            {
                if (flashClient == null || !flashClient.Connected)
                {
                    flashClient = new FlashUpgradeClient();
                    // 需要找到对应的项目使用的串口等信息
                    if (!string.IsNullOrEmpty(portName))
                    {
                        flashClient.SerialPort = portName;
                    }
                    else
                    {
                        flashClient.SerialPort = PortList[SerialPortIndex];
                    }
                    flashClient.Baudrate = 38400;
                    flashClient.ConnectionTimeout = 5000;
                    flashClient.NumberOfRetries = 0;
                    flashClient.Parity = Parity.None;
                    flashClient.StopBits = StopBits.One;
                }
            }
            else if (BBLLCConnectTypeListIndex == 1)
            {
                if (CanClient == null || !CanClient.IsConnected)
                {
                    CANCommConfig = new CANCommConfig();
                    CANCommConfig.CanInd = canId;
                    controlCAN = new ControlCAN(CANCommConfig);
                    CanClient = new CANFlashUpgradeClient(controlCAN);
                }
            }

        }


        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var ppec = _projectManager.GetCachePPEC();
            if (ppec != null)
            {
                if (!string.IsNullOrEmpty(ppec.CommunicationType))
                {
                    var comType = _comConnectTypes.FirstOrDefault(t => t.Name == ppec.CommunicationType);
                    BBLLCConnectTypeListIndex = _comConnectTypes.IndexOf(comType);
                }
                if (!string.IsNullOrEmpty(ppec.PortName))
                {
                    if (ppec.CommunicationType == Constants.Modbus)
                    {
                        SerialPortIndex = _portList.IndexOf(ppec.PortName);
                    }
                    else
                    {
                        var can = BBLLCCANList.FirstOrDefault(t => t.Name == ppec.PortName);
                        BBLLCCANListIndex = BBLLCCANList.ToList().FindIndex(t => t.Name == ppec.PortName);
                    }
                }
            }
        }

        #endregion
    }
}
