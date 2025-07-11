using log4net;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PPEC.Communication;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                SetProperty(ref _treeKeyword, value);
                SearchCategoryTree(value, IsOrderByAddress);
            }
        }

        private bool _isOrderByName = true;
        public bool IsOrderByName
        {
            get => _isOrderByName;
            set => SetProperty(ref _isOrderByName, value);
        }

        private bool _isOrderByAddress = true;
        public bool IsOrderByAddress
        {
            get => _isOrderByAddress;
            set => SetProperty(ref _isOrderByAddress, value);
        }

        private RegisterAddrInfo _currentRegister;
        public RegisterAddrInfo CurrentRegister
        {
            get => _currentRegister;
            set => SetProperty(ref _currentRegister, value);
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
                await currentProject.CommService.SendAsync(calcResult.bytes);

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
                await currentProject.CommService.SendAsync(calcResult.bytes);
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

        private DelegateCommand<BitField> _optionChangeCommand;
        public DelegateCommand<BitField> OptionChangeCommand => _optionChangeCommand ?? (_optionChangeCommand = new DelegateCommand<BitField>((param) =>
        {
            var bnr = Utility.HexToBinaryStringLarge(param.SelectedValue, param.Length);
            param.WriteBinary = bnr;
            var bs = CurrentRegister.BinaryStr;
            var str = Utility.ReplaceBitsInString(bs, param.EndBit, param.StartBit, bnr);
            var dec = Utility.BinaryToDec(str);
            _projectManager.SetRegisterValue(param.Name, dec);

        }));

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

        public override void LoadData()
        {
            var tree = _projectManager.GetChipCategoryTree();
            SingleParamTrees.AddRange(tree);
            CurrentRegister = _projectManager.CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo)
                .FirstOrDefault(t => t.Name == tree[0].Children[0].Children[0].Title);

        }
    }
}
