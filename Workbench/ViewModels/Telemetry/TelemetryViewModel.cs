using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Xml;
using log4net;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Utilities.Collections;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Workbench.Communication;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;
using Workbench.ViewModels.dw;
using static NPOI.HSSF.Util.HSSFColor;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryViewModel : AvaDocument
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TelemetryViewModel));
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        public TelemetryViewModel(IEventAggregator eventAggregator, ProjectManager projectManager) 
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            SequenceList = _projectManager.CurrentProject.TeleMetrySequences;
            ReadWriteHistory = _projectManager.CurrentProject.TeleMetryReadWriteHistory;
            GetTeleInit();
            InitData();
            InitListen();
        }

        public void InitData()
        {
            try
            {
                UpGridWidth = _projectManager.CurrentProject.TelemetryViewGrid.UpGridWidth;
                DownGridWidth = _projectManager.CurrentProject.TelemetryViewGrid.DownGridWidth;
                ThreeGridWidth = _projectManager.CurrentProject.TelemetryViewGrid.ThreeGridWidth;
                SplitterPositionLeft = _projectManager.CurrentProject.TelemetryViewGrid.SplitterPositionLeft;
                SplitterPositionRight = _projectManager.CurrentProject.TelemetryViewGrid.SplitterPositionRight;
                AllTime = _projectManager.CurrentProject.TelemetryViewGrid.AllTime;
            }
            catch (Exception ex)
            {

            }

        }
        public void InitListen()
        {
            _eventAggregator.GetEvent<SaveProjectEvent>().Subscribe(e => {
                e.TelemetryViewGrid.upGridWidth = upGridWidth;
                e.TelemetryViewGrid.DownGridWidth = DownGridWidth;
                e.TelemetryViewGrid.ThreeGridWidth = ThreeGridWidth;
                e.TelemetryViewGrid.AllTime = AllTime;
                e.TelemetryViewGrid.SplitterPositionLeft = SplitterPositionLeft;
                e.TelemetryViewGrid.SplitterPositionRight = SplitterPositionRight;
            });
        }

        #region Property
        private string _AllTime = "50";
        public string AllTime
        {
            get => _AllTime;
            set => SetProperty(ref _AllTime, value);
        }

        public System.Windows.GridLength upGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength UpGridWidth
        {
            get => upGridWidth;
            set => SetProperty(ref upGridWidth, value);
        }

        public System.Windows.GridLength downGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength DownGridWidth
        {
            get => downGridWidth;
            set => SetProperty(ref downGridWidth, value);
        }

        public System.Windows.GridLength threeGridWidth = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ThreeGridWidth
        {
            get => threeGridWidth;
            set => SetProperty(ref threeGridWidth, value);
        }

        public System.Windows.GridLength splitterPositionLeft = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionLeft
        {
            get => splitterPositionLeft;
            set => SetProperty(ref splitterPositionLeft, value);
        }

        public System.Windows.GridLength splitterPositionRight = new System.Windows.GridLength(1.1, System.Windows.GridUnitType.Star);
        public System.Windows.GridLength SplitterPositionRight
        {
            get => splitterPositionRight;
            set => SetProperty(ref splitterPositionRight, value);
        }

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

        private ObservableCollection<Sequence> _sequenceList = new ObservableCollection<Sequence>();
        public ObservableCollection<Sequence> SequenceList
        {
            get => _sequenceList;
            set => SetProperty(ref _sequenceList, value);
        }

        private ObservableCollection<SingleParamHistory> _readWriteHistory = new ObservableCollection<SingleParamHistory>();

        public ObservableCollection<SingleParamHistory> ReadWriteHistory
        {
            get => _readWriteHistory;
            set => SetProperty(ref _readWriteHistory, value);
        }

        private bool _isLeftOpen = true;
        public bool IsLeftOpen
        {
            get => _isLeftOpen;
            set
            {
                if (_isLeftOpen != value)
                {
                    SetProperty(ref _isLeftOpen, value);
                }
            }
        }

        public bool _batchAllCheck;
        public bool BatchAllCheck
        {
            get => _batchAllCheck;
            set
            {
                SetProperty(ref _batchAllCheck, value);
            }
        }

        private bool _checkAll = false;
        public bool CheckAll
        {
            get => _checkAll;
            set
            {
                SetProperty(ref _checkAll, value);
                foreach (var item in SequenceList)
                {
                    item.IsChecked = value;
                }
            }
        }
        private bool _checkAllResister = false;
        public bool CheckAllResister
        {
            get => _checkAllResister;
            set
            {
                SetProperty(ref _checkAllResister, value);
                foreach (var item in CurrentSequence.TelemetryItems)
                {
                    item.IsChecked = value;
                }
            }
        }

        private Sequence _currentSequence;
        public Sequence CurrentSequence
        {
            get => _currentSequence;
            set => SetProperty(ref _currentSequence, value);
        }

        private TelemetryCode _currentRegister;
        public TelemetryCode CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
        }

        private TelemetryCode _writeCurrentRegister;
        public TelemetryCode WriteCurrentRegister
        {
            get => _writeCurrentRegister;
            set => SetProperty(ref _writeCurrentRegister, value);
        }
        #endregion


        private DelegateCommand _closeCommand;
        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));
        public DelegateCommand ToggleDrawerCommand => new DelegateCommand(() => IsLeftOpen = !IsLeftOpen);

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = ListTele.FirstOrDefault(t => t.Name == param.Title);
                param.IsCheck = !param.IsCheck;

            }));

        public DelegateCommand<object> SelectAllCommand => new DelegateCommand<object>((e) =>
        {
            SingleParamTrees.SetAllLeavesChecked((bool)e);
        });

        #region SqeCommand
        private DelegateCommand _addSequenceCommand;
        public DelegateCommand AddSequenceCommand => _addSequenceCommand ?? (_addSequenceCommand = new DelegateCommand(() =>
        {
            var indexS = SequenceList.Count() + 1;
            string nameS = "序列" + indexS;

            SequenceList.Add(new Sequence
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = nameS
            });
        }));

        private DelegateCommand<Sequence> _sendCommand;
        public DelegateCommand<Sequence> SendCommand => _sendCommand ?? (_sendCommand = new DelegateCommand<Sequence>(async (param) =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await Task.Run(async () =>
            {
                await SendSequence(param);
            });
        }));

        public DelegateCommand<TelemetryCode> SendSignCommand => new DelegateCommand<TelemetryCode>(async(e) =>
        {
            if (!(_projectManager.CurrentProject.CommService is PcmuUartService))
            {
                HandyControl.Controls.MessageBox.Show("系统连接才能使用遥控下发，请重试!");
                return;
            }
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            e.SendState = Constants.Waite;
            await Task.Run(async () =>
            {
                var tempSequence=new Sequence();
                tempSequence.TelemetryItems.Add(e);
                await SendSequence(tempSequence);
            });
        });
        private DelegateCommand _batchSendCommand;
        public DelegateCommand BatchSendCommand => _batchSendCommand ?? (_batchSendCommand = new DelegateCommand(async () =>
        {
            if (!_projectManager.CurrentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var seq in SequenceList.Where(t => t.IsChecked))
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(seq.ParamPushInterval);
                    await SendSequence(seq);
                });
            }
        }));

        public DelegateCommand BatchDelCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否批量删除序列", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var tempRemove = SequenceList.Where(t => t.IsChecked).ToArray();
                        foreach (var seq in tempRemove)
                        {
                            SequenceList.Remove(seq);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }
        });

        private async Task SendSequence(Sequence param)
        {
            var currentProject = _projectManager.CurrentProject;
            param.Progress = 0;
            param.CompletedNumTelemetry = 0;
            Thread.Sleep(1000);
            foreach (var register in param.TelemetryItems)
            {
                bool isSuc = true;
                switch(currentProject.ConnectType)
                {
                    case Constants.PIN_CONNECT:
                        switch (currentProject.CommunicationType)
                        {
                            case Constants.OldSERIAL_PORT:
                            case Constants.Modbus:
                            case Constants.I2C:
                            case Constants.CAN:
                                break;
                        }
                        break;
                    case Constants.SYS_CONNECT:
                        switch (currentProject.CommunicationType)
                        {
                            case Constants.SERIAL_PORT:
                            case Constants.OldSERIAL_PORT:
                            case Constants.Telemetry:
                                if (register.Type == ((int)TelemetryCommandType.IndirectCommand).ToString())
                                {
                                    var cmd = UtilsFunc.HexStringToBytes(register.Code);
                                    var ack1 = await currentProject.CommService.SendRemoteControlAsync(cmd, 1000);
                                    if (!ack1.Success)
                                    {
                                        register.SendState = Constants.Lose;
                                        isSuc = false;
                                        // ack1.RawCode == 0xFFFF 或超时
                                    }
                                    else
                                    {
                                        register.SendState = Constants.Suc;
                                    }
                                }
                                if (register.Type == ((int)TelemetryCommandType.NoteInstruction).ToString())
                                {
                                    var injection = UtilsFunc.HexStringToBytes(register.Code);
                                    var ack1 = await currentProject.CommService.SendInjectionAsync(injection, 1000);
                                    if (!ack1.Success)
                                    {
                                        register.SendState = Constants.Lose;
                                        isSuc = false;
                                    }
                                    else
                                    {
                                        register.SendState = Constants.Suc;
                                    }
                                }
                                break;
                            case Constants.CAN:
                                break;
                        }
                        break;
                }
                //switch (currentProject.CommunicationType)
                //{
                //    case Constants.OldSERIAL_PORT:
                //    case Constants.Modbus: 
                //    case Constants.I2C:
                //    case Constants.CAN:                         
                //        break;
                //    case Constants.Telemetry:
                //        if(register.Type == ((int)TelemetryCommandType.IndirectCommand).ToString())
                //        {
                //            var cmd = UtilsFunc.HexStringToBytes(register.Code);
                //            var ack1 = await currentProject.CommService.SendRemoteControlAsync(cmd,1000);
                //            if (!ack1.Success)
                //            {
                //                isSuc = false;
                //                // ack1.RawCode == 0xFFFF 或超时
                //            }
                //        }
                //        if (register.Type == ((int)TelemetryCommandType.NoteInstruction).ToString())
                //        {
                //            var injection = UtilsFunc.HexStringToBytes(register.Code);
                //            var ack1 = await currentProject.CommService.SendInjectionAsync(injection, 1000);
                //            if (!ack1.Success)
                //            {
                //                isSuc = false;
                //            }
                //        }
                        
                //        break;
                //}
                //await currentProject.CommService.SendAsync(calcResult.bytes);
                param.CompletedNumTelemetry += 1;
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
                var history = new SingleParamHistory
                {
                    ReadWrite = "W",
                    Address = register.Code,
                    Hex = register.Code,
                    Name = register.Name,
                    Type= register.Type=="0"?"间接指令":"注数指令",
                    State = isSuc?"成功":"失败",
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _projectManager.CurrentProject.TeleMetryReadWriteHistory.Add(history);
                });
            }
        }

        private DelegateCommand _addRegisterToSequenceCommand;
        public DelegateCommand AddRegisterToSequenceCommand => _addRegisterToSequenceCommand ?? (_addRegisterToSequenceCommand = new DelegateCommand(() =>
        {
            if (CurrentRegister == null)
            {
                MessageBox.Show("请选择寄存器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (CurrentSequence == null)
            {
                MessageBox.Show("请选择序列", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var SelectAddress = SingleParamTrees.GetDeepestChecked().ToList();
            foreach (var item in SelectAddress)
            {
                var register = ListTele.FirstOrDefault(t => t.Name == item.Title);
                var clone = JsonHelper.DeepClone(register);
                clone.Id = Guid.NewGuid().ToString("N");
                CurrentSequence.TelemetryItems.Add(clone);
                item.IsCheck = false;
            }
            this.BatchAllCheck = false;
        }));

        public DelegateCommand BatchDelRegisterCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否批量删除序列详情", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            var delSeq = CurrentSequence.TelemetryItems.Where(t => t.IsChecked).ToArray();
            foreach (var item in delSeq)
            {
                CurrentSequence.TelemetryItems.Remove(item);
            }
            CollectionViewSource.GetDefaultView(CurrentSequence.TelemetryItems).Refresh();
        });

        public DelegateCommand CleraHistoryCommand => new DelegateCommand(() =>
        {
            var result = System.Windows.Forms.MessageBox.Show("是否清除历史记录!", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _projectManager.CurrentProject.TeleMetryReadWriteHistory.Clear();
                ReadWriteHistory.Clear();
            }

        });

        private DelegateCommand _historyDownloadCommand;
        public DelegateCommand HistoryDownloadCommand => _historyDownloadCommand ?? (_historyDownloadCommand = new DelegateCommand(() =>
        {
            if (!ReadWriteHistory.Any())
            {
                MessageBox.Show("无历史数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var fbd = new FolderBrowserDialog();
            fbd.Description = "请选择保存路径";
            var result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                var path = fbd.SelectedPath;
                HistoryToExcel(path);
            }
        }));
        #endregion
        public override async void LoadData()
        {
            var tree =await _projectManager.GetChipCategoryTreeForTele();
            SingleParamTrees.AddRange(tree);
        }

        public List<TelemetryCode> ListTele { get; set; }

        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                SetProperty(ref _treeKeyword, value);
                //OrderByType(value);
                if (IsOrderByCategory)
                {
                    SearchCategoryTree(value, IsOrderByAddress);
                }
                else if (IsOrderByName)
                {
                    OrderByType(value, OrderByTypeEnum.Name);
                }
                else if (IsOrderByAddress)
                {
                    OrderByType(value, OrderByTypeEnum.Address);
                }
            }
        }

        private bool _isOrderByCategory = true;
        public bool IsOrderByCategory
        {
            get => _isOrderByCategory;
            set
            {
                if (value)
                {
                    SearchCategoryTree(TreeKeyword, IsOrderByAddress);
                }
                SetProperty(ref _isOrderByCategory, value);
            }
        }

        private bool _isOrderByName = false;
        public bool IsOrderByName
        {
            get => _isOrderByName;
            set
            {
                if (value)
                {
                    //return SerialPort.GetPortNames().OrderBy(t => t, new NaturalStringComparer()).ToList();
                    var tempList = SingleParamTrees.GetMaxDepthLeaves().ToList().OrderBy(x => x.Title,new SerialAsistant.Utils.ChineseNaturalSortComparerWithRegex()).ToList();
                    SingleParamTrees.Clear();
                    SingleParamTrees.AddRange(tempList);
                }
                SetProperty(ref _isOrderByName, value);
            }
        }

        private bool _isOrderByAddress = false;
        public bool IsOrderByAddress
        {
            get => _isOrderByAddress;
            set
            {
                if (value)
                {
                    var tempList = SingleParamTrees.GetMaxDepthLeaves()
    .OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue)
    .ToList();
                    SingleParamTrees.Clear();
                    SingleParamTrees.AddRange(tempList);
                }
                SetProperty(ref _isOrderByAddress, value);
            }
        }

        private async void SearchCategoryTree(string keyword, bool isOrderByAddress = true)
        {
            SingleParamTrees.Clear();
            var source =await _projectManager.GetChipCategoryTreeForTele(isOrderByAddress: isOrderByAddress);
            if (string.IsNullOrEmpty(keyword))
            {
                SingleParamTrees.AddRange(source);
            }
            else
            {
                var searcher = new TreeSearcher();
                var filteredResult = searcher.SearchInForest(source, keyword);
                SingleParamTrees.AddRange(filteredResult);
            }
        }

        private async void SearchCodeTree(string keyword, bool isOrderByAddress = true)
        {
            SingleParamTrees.Clear();
            var source = await _projectManager.GetChipCategoryTreeForTele(); 
            if (string.IsNullOrEmpty(keyword))
            {
                SingleParamTrees.AddRange(source);
            }
            else
            {
                var searcher = new TreeSearcher();
                var filteredResult = searcher.SearchInForest(source, keyword);
                SingleParamTrees.AddRange(filteredResult);
            }
        }
        private async Task OrderByType(string value, OrderByTypeEnum NameOrAddress=OrderByTypeEnum.Name)
        {
            var source = await _projectManager.GetChipCategoryTreeForTele();
            var tempList = source.GetMaxDepthLeaves().ToList().OrderBy(x => x.Title, new SerialAsistant.Utils.ChineseNaturalSortComparerWithRegex());
            if (NameOrAddress == OrderByTypeEnum.Address)
            {
                tempList = SingleParamTrees.GetMaxDepthLeaves().ToList().OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue);

            }
            if (!string.IsNullOrEmpty(value))
            {
                tempList = tempList.Where(x => x.AddressHex.Contains(value) || x.Title.Contains(value)).OrderBy(x => x.Title, new SerialAsistant.Utils.ChineseNaturalSortComparerWithRegex());
            }
            SingleParamTrees.Clear();
            SingleParamTrees.AddRange(tempList);
        }
        #region Method
        public async void GetTeleInit()
        {
            ListTele =await _projectManager.GetTeleLisst(_projectManager.CurrentProject.Chip.ChipId);
            CurrentRegister = ListTele.FirstOrDefault();
        }

        public void ChangeIsConfigPaneOpen(TelemetryCode param)
        {
            var selected = param;
            bool same = IsSameRegister(selected, WriteCurrentRegister);
            if (same && selected != null)
            {
                //IsConfigPaneOpen = true;
            }
            else
            {
                WriteCurrentRegister = param;
                //IsConfigPaneOpen = true;
            }
        }

        private static bool IsSameRegister(TelemetryCode a, TelemetryCode b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;

            // 优先按 AddressHex 比较（你两边通常都有这个字段）
            var aAddr = GetStringProp(a, "Name");
            var bAddr = GetStringProp(b, "Name");
            if (!string.IsNullOrEmpty(aAddr) && !string.IsNullOrEmpty(bAddr))
                return string.Equals(aAddr, bAddr, StringComparison.OrdinalIgnoreCase);

            // 备用：按 Id 比较（如果你的模型有 Id）
            var aId = GetStringProp(a, "Code");
            var bId = GetStringProp(b, "Code");
            if (!string.IsNullOrEmpty(aId) && !string.IsNullOrEmpty(bId))
                return string.Equals(aId, bId, StringComparison.OrdinalIgnoreCase);

            return false;
        }

        private static string GetStringProp(object o, string name)
       => o?.GetType().GetProperty(name)?.GetValue(o)?.ToString();

        private void HistoryToExcel(string path)
        {
            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Sheet1");
                var headerRow = sheet.CreateRow(0);
                string[] headerColumns = new string[] { "读/写", "名称", "指令(HEX)","类型", "状态", "操作时间" };
                for (int i = 0; i < headerColumns.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(headerColumns[i]);
                }

                int startRow = 1;
                foreach (var history in ReadWriteHistory)
                {
                    var row = sheet.CreateRow(startRow);
                    row.CreateCell(0).SetCellValue(history.ReadWrite);
                    row.CreateCell(1).SetCellValue(history.Name);
                    row.CreateCell(2).SetCellValue(history.Hex);
                    row.CreateCell(3).SetCellValue(history.Type);
                    row.CreateCell(4).SetCellValue(history.State);
                    row.CreateCell(5).SetCellValue(history.Datetime);
                    startRow++;
                }

                for (int i = 0; i < headerColumns.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                string fileName = "指令操作历史数据.xlsx";
                string filePath = Path.Combine(path, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }
                MessageBox.Show("下载成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

    }
}
