using Common.Controls;
using log4net;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.Mozilla;
using PPEC.Communication;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks; 
using System.Windows.Forms;
using System.Windows.Input;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils;

namespace Workbench.ViewModels.dw
{
    public class SingleParamsViewModel : AvaDocument
    {
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        private static readonly ILog _log = LogManager.GetLogger(typeof(SingleParamsViewModel));

        public SingleParamsViewModel(ProjectManager projectManager, IEventAggregator eventAggregator)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            ReadWriteHistory = _projectManager.CurrentProject.ReadWriteHistory;
        }

        private bool _isLeftOpen= true;
        public bool IsLeftOpen
        {
            get => _isLeftOpen;
            set { 
                if (_isLeftOpen != value) 
                {
                    SetProperty(ref _isLeftOpen, value);
                }
            }
        }

        public DelegateCommand ToggleDrawerCommand => new DelegateCommand(() => IsLeftOpen = !IsLeftOpen);

        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                SetProperty(ref _treeKeyword, value);
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
                    var tempList = SingleParamTrees.GetMaxDepthLeaves().ToList().OrderBy(x => x.Title);
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
    .OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue)
    .ToList();
                    SingleParamTrees.Clear();
                    SingleParamTrees.AddRange(tempList);
                }
                SetProperty(ref _isOrderByAddress, value);
            }
        }

        private RegisterAddrInfo _currentRegister;
        public RegisterAddrInfo CurrentRegister
        {
            get => _currentRegister;
            set
            {
                if(SetProperty(ref _currentRegister, value))
                {
                    WriteCurrentRegister = JsonHelper.DeepClone(value);
                    HookBitFields();
                }                
            } 
        }

        #region ShowClomn
        private bool _hasOptionsColumn = true;
        public bool HasOptionsColumn
        {
            get => _hasOptionsColumn;
            set => SetProperty(ref _hasOptionsColumn, value);
        }

        private void HookBitFields()
        {
            var fields = WriteCurrentRegister?.BitFields;
            if (fields == null) { HasOptionsColumn = false; return; }

            // TODO: 解绑旧订阅（略）

            // 集合替换时重算
            // 如果 BitFields 不是 ObservableCollection，建议先换成它

            //foreach (var f in fields)
            //{
            //    // Options 被替换
            //    f.PropertyChanged += (_, e) =>
            //    {
            //        if (e.PropertyName == nameof(BitField.Options))
            //            RecalcHasOptions();
            //    };

            //    // Options 内部增删
            //    if (f.Options is INotifyCollectionChanged ncc)
            //        ncc.CollectionChanged += (_, __) => RecalcHasOptions();
            //}

            RecalcHasOptions();
        }

        private void RecalcHasOptions()
        {
            var fields = WriteCurrentRegister?.BitFields;
            HasOptionsColumn = fields != null && fields.Any(x => x.Options != null && x.Options.Count > 0);
        }
        #endregion
        private RegisterAddrInfo _writeCurrentRegister;
        public RegisterAddrInfo WriteCurrentRegister
        {
            get => _writeCurrentRegister;
            set => SetProperty(ref _writeCurrentRegister, value);
        }

        private ObservableCollection<ValueLabelOption> _settingCategoryList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> SettingCategoryList
        {
            get => _settingCategoryList;
            set => SetProperty(ref _settingCategoryList, value);
        }

        private ValueLabelOption _currentCategory;
        public ValueLabelOption CurrentCategory
        {
            get => _currentCategory;
            set
            {
                if (SetProperty(ref _currentCategory, value))
                {
                    CategoryAddressList.Clear();
                    var CategoryAddressListOptions = _projectManager.GetRegisterForCategories(value.Value.ToString()).Select(t => new ValueLabelOption() { Value = t.AddressDec, Label = t.ShowAddressStr });
                    CategoryAddressList.AddRange(CategoryAddressListOptions);
                    CategoryAddress = CategoryAddressList.FirstOrDefault();

                    UtilsFunc.SerachCategoryNode(SingleParamTrees,value);
                }
            }
        }

        

        private ObservableCollection<ValueLabelOption> _categoryAddressList = new ObservableCollection<ValueLabelOption>();
        public ObservableCollection<ValueLabelOption> CategoryAddressList
        {
            get => _categoryAddressList;
            set => SetProperty(ref _categoryAddressList, value);
        }

        private ValueLabelOption _categoryAddress;
        public ValueLabelOption CategoryAddress
        {
            get => _categoryAddress;
            set
            {
                if (SetProperty(ref _categoryAddress, value))
                {
                    if (value != null)
                        CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.AddressDec == (uint)value.Value);
                }
            }
        }

        private void SearchCategoryTree(string keyword, bool isOrderByAddress = true)
        {
            SingleParamTrees.Clear();
            var source = _projectManager.GetChipCategoryTree(isOrderByAddress: isOrderByAddress);
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

        private ObservableCollection<CategoryTree> _singleParamTrees = new ObservableCollection<CategoryTree>();
        public ObservableCollection<CategoryTree> SingleParamTrees
        {
            get => _singleParamTrees;
            set => SetProperty(ref _singleParamTrees, value);
        }

        private ObservableCollection<SingleParamHistory> _readWriteHistory = new ObservableCollection<SingleParamHistory>();

        public ObservableCollection<SingleParamHistory> ReadWriteHistory
        {
            get => _readWriteHistory;
            set => SetProperty(ref _readWriteHistory, value);
        }

        private DelegateCommand<CategoryTree> _selectedItemChangedCommand;
        public DelegateCommand<CategoryTree> SelectedItemChangedCommand => _selectedItemChangedCommand ??
            (_selectedItemChangedCommand = new DelegateCommand<CategoryTree>((param) =>
            {
                if (param == null || param.Type != CategoryTreeType.Register) return;

                CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == param.Title);
                SetCurrentRegisterValue(0);
            }));

        private DelegateCommand _checkboxChangeCommand;
        public DelegateCommand CheckboxChangeCommand => _checkboxChangeCommand ?? (_checkboxChangeCommand = new DelegateCommand(() =>
        {
            SearchCategoryTree(TreeKeyword, IsOrderByAddress);
        }));

        private DelegateCommand _readRegisterCommand;
        public DelegateCommand ReadRegisterCommand => _readRegisterCommand ?? (_readRegisterCommand = new DelegateCommand(async () =>
        {
            if (CurrentRegister == null)
                return;

            var currentProject = _projectManager.CurrentProject;

            if (!currentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await Task.Run(async () =>
            {
                var calcResult = UtilsFunc.GetReadCommandByAddress(CurrentRegister.AddressHex, currentProject.CommunicationType);
                switch (currentProject.CommunicationType)
                {
                    case Constants.OldSERIAL_PORT:
                    case Constants.Modbus:
                        await currentProject.CommService.SendAsync(calcResult.bytes);
                        break;
                    case Constants.I2C:
                        if (ushort.TryParse(CurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                        {
                            await currentProject.CommService.ReadRegisterAsync(reg);
                        }
                        //CurrentProject.CommService.Read(item.Param.AddressHex);
                        break;
                    case Constants.CAN:
                        if (ushort.TryParse(CurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg1))
                        {
                            await currentProject.CommService.ReadRegisterAsync(reg1);
                        }
                        break;
                }
                //await currentProject.CommService.SendAsync(calcResult.bytes);

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                var read = currentProject.CommService.Read(CurrentRegister.AddressHex);
                if (read.HasValue)
                {
                    SetCurrentRegisterValue(read.Value);
                    var history = new SingleParamHistory
                    {
                        ReadWrite = "R",
                        Address = CurrentRegister.AddressHex,
                        Hex = Utility.DecToHex(read.Value),
                        Name = CurrentRegister.Name,
                        State = "正常",
                        Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _projectManager.CurrentProject.ReadWriteHistory.Add(history);
                    });
                }
            });
        }));

        private void SetCurrentRegisterValue(uint value)
        {
            _projectManager.SetRegisterValue(CurrentRegister.Name, value);
        }

        private DelegateCommand _sendRegisterCommand;
        public DelegateCommand SendRegisterCommand => _sendRegisterCommand ?? (_sendRegisterCommand = new DelegateCommand(async () =>
        {
            if (CurrentRegister == null)
                return;

            var currentProject = _projectManager.CurrentProject;

            if (!currentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await Task.Run(async () =>
            {
                var calcResult = UtilsFunc.GetWriteCommandByAddress(CurrentRegister.AddressHex, currentProject.CommunicationType, CurrentRegister.DecValue);
                switch (currentProject.CommunicationType)
                {
                    case Constants.OldSERIAL_PORT:
                    case Constants.Modbus:
                        await currentProject.CommService.SendAsync(calcResult.bytes);
                        break;
                    case Constants.I2C:
                        if (ushort.TryParse(WriteCurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                        {
                            await currentProject.CommService.WriteRegisterAsync(reg, WriteCurrentRegister.DecValue);
                        }
                        break;
                    case Constants.CAN:
                        byte[] byteArray = BitConverter.GetBytes(WriteCurrentRegister.DecValue);
                        if (ushort.TryParse(WriteCurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg1))
                        {
                            await currentProject.CommService.WriteRegisterAsync(reg1, byteArray);
                        }
                        break;
                }
                //await currentProject.CommService.SendAsync(calcResult.bytes);
                var history = new SingleParamHistory
                {
                    ReadWrite = "W",
                    Address = CurrentRegister.AddressHex,
                    Hex = CurrentRegister.HexValue,
                    Name = CurrentRegister.Name,
                    State = "正常",
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _projectManager.CurrentProject.ReadWriteHistory.Add(history);
                    SetCurrentRegisterValue(0);

                });

            });
        }));
        private void OrderByType(string value, OrderByTypeEnum NameOrAddress)
        {
            var tempList = _projectManager.GetChipCategoryTree().GetMaxDepthLeaves().ToList().OrderBy(x => x.Title);
            if (NameOrAddress == OrderByTypeEnum.Address)
            {
                tempList = _projectManager.GetChipCategoryTree().GetMaxDepthLeaves().ToList().OrderBy(n => ulong.TryParse(n.AddressDec?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : ulong.MaxValue);

            }
            if (!string.IsNullOrEmpty(value))
            {
                tempList = tempList.Where(x => x.AddressHex.Contains(value) || x.Title.Contains(value)).OrderBy(x => x.Title);
            }
            SingleParamTrees.Clear();
            SingleParamTrees.AddRange(tempList);
        }
        public async Task SendRegister()
        {
            if (WriteCurrentRegister == null)
                return;

            var currentProject = _projectManager.CurrentProject;

            if (!currentProject.IsConnecting)
            {
                MessageBox.Show("当前工程未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await Task.Run(async () =>
            {
                var calcResult = UtilsFunc.GetWriteCommandByAddress(WriteCurrentRegister.AddressHex, currentProject.CommunicationType, WriteCurrentRegister.DecValue);
                switch (currentProject.CommunicationType)
                {
                    case Constants.OldSERIAL_PORT:
                    case Constants.Modbus:
                        await currentProject.CommService.SendAsync(calcResult.bytes);
                        break;
                    case Constants.I2C:
                        if (ushort.TryParse(WriteCurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg))
                        {
                            await currentProject.CommService.WriteRegisterAsync(reg, WriteCurrentRegister.DecValue);
                        }
                        break;
                    case Constants.CAN:
                        byte[] byteArray = BitConverter.GetBytes(WriteCurrentRegister.DecValue);
                        var frame = new List<byte>();
                        if (ushort.TryParse(WriteCurrentRegister.AddressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort reg1))
                        {
                            UtilsFunc.WriteUInt32(WriteCurrentRegister.DecValue, EndianMode.BigEndian, frame);
                            await currentProject.CommService.WriteRegisterAsync(reg1, frame.ToArray());
                        }
                        break;
                }
                var tempHex = Utility.DecToHex(WriteCurrentRegister.DecValue);
                //await currentProject.CommService.SendAsync(calcResult.bytes);
                var history = new SingleParamHistory
                {
                    ReadWrite = "W",
                    Address = WriteCurrentRegister.AddressHex,
                    Hex = tempHex,//WriteCurrentRegister.HexValue,
                    Name = WriteCurrentRegister.Name,
                    State = "正常",
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _projectManager.CurrentProject.ReadWriteHistory.Add(history);
                    //SetCurrentRegisterValue(0);

                });

            });
        }
        private DelegateCommand _closeCommand;

        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

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

        public DelegateCommand CleraHistoryCommand => new DelegateCommand(() =>
        {
            var result = System.Windows.Forms.MessageBox.Show("是否清除历史记录!", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _projectManager.CurrentProject.ReadWriteHistory.Clear();
                ReadWriteHistory.Clear();
            }
                
        });

        private DelegateCommand<BitField> _optionChangeCommand;
        public DelegateCommand<BitField> OptionChangeCommand => _optionChangeCommand ?? (_optionChangeCommand = new DelegateCommand<BitField>((param) =>
        {
            if (param.SelectedValue == null)
                return;
            var bnr = Utility.HexToBinaryStringLarge(param.SelectedValue, param.Length);
            param.WriteBinary = bnr;
            //UpdateBinaryString(param.Name, param.EndBit, param.StartBit, bnr);
            UpdateWriteRegister(param.Name, param.EndBit, param.StartBit, bnr);
        }));

        public void UpdateWriteRegister(string name, int endBit, int startBit, string replaceStr)
        {
            var bs = WriteCurrentRegister.BinaryStr;
            var str = Utility.ReplaceBitsInString(bs, endBit, startBit, replaceStr);
            var dec = Utility.BinaryToDec(str);
            _projectManager.SetWriteRegisterValue(WriteCurrentRegister,name, dec);
        }
        internal void UpdateBinaryString(string name, int endBit, int startBit, string replaceStr)
        {
            //var bs = CurrentRegister.BinaryStr;
            //var str = Utility.ReplaceBitsInString(bs, endBit, startBit, replaceStr);
            //var dec = Utility.BinaryToDec(str);
            //_projectManager.SetRegisterValue(name, dec);
        }

        private void HistoryToExcel(string path)
        {
            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Sheet1");
                var headerRow = sheet.CreateRow(0);
                string[] headerColumns = new string[] { "读/写", "地址", "名称", "数据(HEX)", "状态", "操作时间" };
                for (int i = 0; i < headerColumns.Length; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(headerColumns[i]);
                }

                int startRow = 1;
                foreach (var history in ReadWriteHistory)
                {
                    var row = sheet.CreateRow(startRow);
                    row.CreateCell(0).SetCellValue(history.ReadWrite);
                    row.CreateCell(1).SetCellValue(history.Address);
                    row.CreateCell(2).SetCellValue(history.Name);
                    row.CreateCell(3).SetCellValue(history.Hex);
                    row.CreateCell(4).SetCellValue(history.State);
                    row.CreateCell(5).SetCellValue(history.Datetime);
                    startRow++;
                }

                for (int i = 0; i < headerColumns.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                string fileName = "寄存器操作历史数据.xlsx";
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

        public DelegateCommand ClearSettingCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否清空所选寄存器配置", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                foreach (var item in WriteCurrentRegister.BitFields)
                {
                    item.SelectedValue = "";
                    item.WriteHex = "";

                    var binValue = Utility.HexToBinaryStringLarge(item.WriteHex, item.Length);
                    item.WriteBinary = binValue;
                    UpdateWriteRegister(item.Name, item.EndBit, item.StartBit, binValue);
                }
            }
        });
        public override void LoadData()
        {
            var tree = _projectManager.GetChipCategoryTree();
            SingleParamTrees.AddRange(tree);
            CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo)
                .FirstOrDefault(t => t.Name == tree[0].Children[0].Children[0].Title);

            var categoryOptions = _projectManager.GetCategories().Select(t => new ValueLabelOption() { Value = t, Label = t });
            SettingCategoryList.Clear();
            SettingCategoryList.AddRange(categoryOptions);
            CurrentCategory = SettingCategoryList.FirstOrDefault();

            CategoryAddressList.Clear();

            var CategoryAddressListOptions = _projectManager.GetRegisterForCategories(CurrentCategory.Value.ToString()).Select(t => new ValueLabelOption() { Value = t.AddressDec, Label = t.ShowAddressStr });
            CategoryAddressList.AddRange(CategoryAddressListOptions);
            CategoryAddress = CategoryAddressList.FirstOrDefault();
        }
    }
}
