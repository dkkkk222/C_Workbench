using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using LinqToDB;
using LinqToDB.Data;
using log4net;
using PPEC.Communication.Common;
using PPEC.Communication.DB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SixLabors.ImageSharp.ColorSpaces;
using Workbench.Db;
using Workbench.Db.IService;
using Workbench.Db.Tables;
using Workbench.Utils;
using static LinqToDB.Reflection.Methods.LinqToDB;

namespace Workbench.ViewModels
{
    public class ChipManagerViewModel : BindableBase, IDialogAware
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChipManagerViewModel));
        private ProjectManager _projectManager;
        public MainServices mainService { get; set; }
        private ICpService _cpService;
        private readonly IMapper _mapper;
        public ChipManagerViewModel(FileHandler fileHandler, IContainerProvider containerProvider, IMapper mapper, ProjectManager projectManager, ICpService cpService)
        {
            _projectManager = projectManager;
            _mapper = mapper;
            _cpService = cpService;
        }
        public ObservableCollection<Chip> _chips = new ObservableCollection<Chip>();
        public ObservableCollection<Chip> Chips
        {
            get => _chips;
            set => SetProperty(ref _chips, value);
        }
        private int _chipId;
        public int ChipId
        {
            get => _chipId;
            set => SetProperty(ref _chipId, value);
        }

        private string _chipName;
        public string ChipName
        {
            get => _chipName;
            set => SetProperty(ref _chipName, value);
        }

        private bool _loading = false;
        public bool Loading
        {
            get => _loading;
            set => SetProperty(ref _loading, value);
        }

        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }
        private string _parseFilePath = "";
        public string ParseFilePath
        {
            get => _parseFilePath;
            set => SetProperty(ref _parseFilePath, value);
        }
        #region Command
        public DelegateCommand AddCommand => new DelegateCommand(() =>
        {
            ChipId = 0;
            ChipName = "";
            FilePath = "";
        });
        public DelegateCommand ComfirmCommand => new DelegateCommand(async () =>
        {
            if (string.IsNullOrEmpty(ChipName))
            {
                MessageBox.Show("请输入芯片名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                MessageBox.Show("请输入或选择寄存器文件路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(ParseFilePath))
            {
                MessageBox.Show("请输入或选择解析文件路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var db = new DbContext())
            {
                var check = await db.Chips.AnyAsync(t => t.Name == ChipName && t.IsDeleted == "A");
                if (check)
                {
                    MessageBox.Show("芯片名称重复", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            try
            {
                Loading = true;
                await Task.Run(async () =>
                {
                    await HandleChipAsync();
                });

                ClearForm();
                await InitChips();
            }
            catch (Exception ex)
            {
                MessageBox.Show("新建类型异常，详情请查看日志", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _log.Error(ex);
            }
            finally
            {
                Loading = false;
            }

        });

        private async Task HandleChipAsync()
        {
            try
            {
                var excelData = new RegisterExcelResolve().Parse(FilePath);
                var excelSDPCData = new RegisterExcelResolve().SDPCParse(ParseFilePath, excelData);
                string chipId = Guid.NewGuid().ToString("N");
                var chip = new Chip()
                {
                    Id = chipId,
                    Name = ChipName,
                    IsDeleted = "A",
                    FileName = FilePath,
                    SDPCfileName = ParseFilePath,
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                using (var db = new DbContext())
                {
                    await db.InsertAsync(chip);

                    var registers = new List<Register>();
                    foreach (var meta in excelData)
                    {
                        var register = _mapper.Map<Register>(meta.AddrInfo);
                        string registerId = Guid.NewGuid().ToString("N");
                        register.Id = registerId;
                        register.ChipId = chipId;
                        registers.Add(register);

                        var registerBits = new List<RegisterBit>();
                        //bit field
                        foreach (var bf in meta.AddrInfo.BitFields)
                        {
                            var registerBit = _mapper.Map<RegisterBit>(bf);
                            string registerBitId = Guid.NewGuid().ToString("N");
                            registerBit.Id = registerBitId;
                            registerBit.RegisterId = registerId;
                            registerBit.ParamA = bf.FormParam.ParamA.ToString();
                            registerBit.ParamB = bf.FormParam.ParamB.ToString();
                            registerBit.ParamC = bf.FormParam.ParamC;
                            registerBit.ParamName = bf.FormParam.ParamName;
                            registerBit.UnitName = bf.FormParam.UnitName;
                            registerBits.Add(registerBit);

                            var registerBitOptions = new List<RegisterBitOption>();
                            foreach (var option in bf.Options)
                            {
                                var registerBitOption = _mapper.Map<RegisterBitOption>(option);
                                registerBitOption.Id = Guid.NewGuid().ToString("N");
                                registerBitOption.RegisterBitId = registerBitId;
                                registerBitOptions.Add(registerBitOption);
                            }
                            await db.BulkCopyAsync(registerBitOptions);
                        }
                        await db.BulkCopyAsync(registerBits);
                    }

                    await db.BulkCopyAsync(registers);
                }
                var ListTeleData = TelemetryParse();
                using (var db = new DbContext())
                {
                    List<TelemetryCode> listCode = new List<TelemetryCode>();
                    foreach (var param1 in ListTeleData.Item1)
                    {
                        string telemetryId = Guid.NewGuid().ToString("N");
                        var inTelCode = new TelemetryCode()
                        {
                            Id = telemetryId,
                            ChipId = chipId,
                            Name = param1.CommandName,
                            Code = param1.CommandCode,
                            Type = ((int)param1.CommandType).ToString(),
                            Length = param1.CommandLength.ToString()
                        };
                        listCode.Add(inTelCode);
                    }
                    await db.BulkCopyAsync(listCode);

                    List<ParamDict> listParams = new List<ParamDict>();
                    List<TelemetryMonit> lisstTeleMon = new List<TelemetryMonit>();
                    foreach (var param1 in ListTeleData.Item2.Item1)
                    {
                        var inTelParam = new ParamDict()
                        {
                            ChipId = chipId,
                            Name = param1.CodeName,
                            TypeCode = 0
                        };
                        listParams.Add(inTelParam);
                        string telemetryId = Guid.NewGuid().ToString("N");
                        var inTeleMon = new TelemetryMonit()
                        {
                            Id = telemetryId,
                            ChipId = chipId,
                            Name = param1.CodeName,
                            ByteName = param1.DateLocation,
                            StartByte = param1.StartLocaltion,
                            EndByte = param1.EndLocaltion,
                            ByteLen = param1.LocaltionLen,
                            BitName = param1.BitName,
                            StartBit = param1.StartBit,
                            EndBit = param1.EndBit,
                            BitLen = param1.BitLength,
                            Type = (int)param1.FormParam.Kind,
                            ParamA = param1.FormParam.A.ToString(),
                            ParamB = param1.FormParam.B.ToString(),
                            ParamC = ((int)param1.FormParam.Kind).ToString(),
                            ParamSign = param1.FormParam.Sign.ToString(),
                            FormulaShow = param1.ShowFormParam,
                            Unit = param1.Unit
                        };
                        lisstTeleMon.Add(inTeleMon);
                    }
                    await db.BulkCopyAsync(listParams);
                    await db.BulkCopyAsync(lisstTeleMon);
                    List<TelemetryTagTable> ListTelemTag = new List<TelemetryTagTable>();
                    foreach (var param1 in ListTeleData.Item2.Item2)
                    {
                        string tagId = Guid.NewGuid().ToString("N");
                        ListTelemTag.Add(new TelemetryTagTable()
                        {
                            Id = tagId,
                            ChipId = chipId,
                            Name = param1.Name
                        });
                    }
                    await db.BulkCopyAsync(ListTelemTag);
                }


                MessageBox.Show("添加芯片成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Loading = false;
            }
        }
        public (List<TelemetryMeta>, (List<TelemetryMonitAnalysisMeta>, List<TelemetryTag>)) TelemetryParse()
        {
            string SDPCfileNameTelemetryData = "SDPC_B10遥测数据表.xlsx";//数据解析
            string SDPCfileNameCommand = "SDPC_B10遥控指令表.xlsx";//遥测指令
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileNameCommand);
            string filePath1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileNameTelemetryData);
            RegisterExcelResolve registerExcelResolve = new RegisterExcelResolve();
            var telemetryCommand = registerExcelResolve.Telemetry(filePath);
            var telemetryMonit = registerExcelResolve.TelemetryMonit(filePath1);
            return (telemetryCommand, telemetryMonit);
        }
        public async Task RebuildChipMetadataAsync(string chipId)
        {
            List<RegisterMeta> excelData = _projectManager.CurrentProject.Chip.ChipRegisterInfo;
            using var db = new DbContext();
            using var tr = db.BeginTransaction();

            // 先找出当前芯片的 Reg/Bit/Option
            var oldRegisters = await db.Registers.Where(r => r.ChipId == chipId).ToListAsync();
            var oldRegIds = oldRegisters.Select(r => r.Id).ToList();
            var oldBits = await db.RegisterBits.Where(b => oldRegIds.Contains(b.RegisterId)).ToListAsync();
            var oldBitIds = oldBits.Select(b => b.Id).ToList();

            // 删除（Options -> Bits -> Registers）
            await db.RegisterBitOptions.Where(o => oldBitIds.Contains(o.RegisterBitId)).DeleteAsync();
            await db.RegisterBits.Where(b => oldRegIds.Contains(b.RegisterId)).DeleteAsync();
            await db.Registers.Where(r => r.ChipId == chipId).DeleteAsync();

            // 用“导入”的数据重建（此处把 ID 用导入工程的，如果导入不带 ID 就新建/或用确定性ID）
            var registers = new List<Register>();
            var registerBits = new List<RegisterBit>();
            var registerBitOptions = new List<RegisterBitOption>();

            foreach (var meta in excelData)
            {
                var reg = _mapper.Map<Register>(meta.AddrInfo);
                if (string.IsNullOrWhiteSpace(reg.Id))
                    reg.Id = Guid.NewGuid().ToString("N");
                reg.ChipId = chipId;
                registers.Add(reg);

                foreach (var bf in meta.AddrInfo.BitFields)
                {
                    var bit = _mapper.Map<RegisterBit>(bf);
                    if (string.IsNullOrWhiteSpace(bit.Id))
                        bit.Id = Guid.NewGuid().ToString("N");
                    bit.RegisterId = reg.Id;

                    bit.ParamA = bf.FormParam.ParamA.ToString();
                    bit.ParamB = bf.FormParam.ParamB.ToString();
                    bit.ParamC = bf.FormParam.ParamC;
                    bit.ParamName = bf.FormParam.ParamName;
                    bit.UnitName = bf.FormParam.UnitName;

                    registerBits.Add(bit);

                    foreach (var opt in bf.Options)
                    {
                        var optRow = _mapper.Map<RegisterBitOption>(opt);
                        if (string.IsNullOrWhiteSpace(optRow.Id))
                            optRow.Id = Guid.NewGuid().ToString("N");
                        optRow.RegisterBitId = bit.Id;
                        registerBitOptions.Add(optRow);
                    }
                }
            }

            await db.BulkCopyAsync(registers);
            await db.BulkCopyAsync(registerBits);
            await db.BulkCopyAsync(registerBitOptions);

            tr.Commit();
        }

        public async Task ConnectTeleChip(string chipId)
        {
            try
            {
                var ListTeleData = TelemetryParse();

                List<TelemetryCode> listCode = new List<TelemetryCode>();
                foreach (var param1 in ListTeleData.Item1)
                {
                    string telemetryId = Guid.NewGuid().ToString("N");
                    var inTelCode = new TelemetryCode()
                    {
                        Id = telemetryId,
                        ChipId = chipId,
                        Name = param1.CommandName,
                        Code = param1.CommandCode,
                        Type = ((int)param1.CommandType).ToString(),
                        Length = param1.CommandLength.ToString()
                    };
                    listCode.Add(inTelCode);
                }
                await _cpService.SaveTeleListAsync(chipId, listCode);
                List<ParamDict> listParams = new List<ParamDict>();
                List<TelemetryMonit> lisstTeleMon = new List<TelemetryMonit>();
                foreach (var param1 in ListTeleData.Item2.Item1)
                {
                    var inTelParam = new ParamDict()
                    {
                        ChipId = chipId,
                        Name = param1.CodeName,
                        TypeCode = 0
                    };
                    listParams.Add(inTelParam);
                    string telemetryId = Guid.NewGuid().ToString("N");
                    var inTeleMon = new TelemetryMonit()
                    {
                        Id = telemetryId,
                        ChipId = chipId,
                        Name = param1.CodeName,
                        ByteName = param1.DateLocation,
                        StartByte = param1.StartLocaltion,
                        EndByte = param1.EndLocaltion,
                        ByteLen = param1.LocaltionLen,
                        BitName = param1.BitName,
                        StartBit = param1.StartBit,
                        EndBit = param1.EndBit,
                        BitLen = param1.BitLength,
                        Type = (int)param1.FormParam.Kind,
                        ParamA = param1.FormParam.A.ToString(),
                        ParamB = param1.FormParam.B.ToString(),
                        ParamC = ((int)param1.FormParam.Kind).ToString(),
                        ParamSign = param1.FormParam.Sign.ToString(),
                        FormulaShow = param1.ShowFormParam,
                        Unit = param1.Unit
                    };
                    lisstTeleMon.Add(inTeleMon);
                }
                List<TelemetryTagTable> ListTelemTag = new List<TelemetryTagTable>();
                foreach (var param1 in ListTeleData.Item2.Item2)
                {
                    string tagId = Guid.NewGuid().ToString("N");
                    ListTelemTag.Add(new TelemetryTagTable()
                    {
                        Id = tagId,
                        ChipId = chipId,
                        Name = param1.Name
                    });
                }

                await _cpService.SaveParamsListAsync(chipId, listParams);
                await _cpService.SaveTeleMonListAsync(chipId, lisstTeleMon);
                await _cpService.SaveTeleTagListAsync(chipId, ListTelemTag);

            }
            catch (Exception ex)
            {

            }
        }
        public async Task UpdateChipDoc(Chip e)
        {
            using (var db = new DbContext())
            {
                await db.UpdateAsync(e);
            }
        }
        public DelegateCommand CloseCommand => new DelegateCommand(() =>
        {
            RequestClose?.Invoke(new Prism.Services.Dialogs.DialogResult(ButtonResult.Cancel));
        });
        public DelegateCommand BrowseCommand => new DelegateCommand(() =>
        {
            var path = ChooseDirectory();
            if (!string.IsNullOrEmpty(path))
                FilePath = path;
        });
        public DelegateCommand BrowseParseCommand => new DelegateCommand(() =>
        {
            var path = ChooseDirectory();
            if (!string.IsNullOrEmpty(path))
                ParseFilePath = path;
        });

        public string ChooseDirectory()
        {
            var path = string.Empty;
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Files (*.xlsx)|*.xlsx";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = openFileDialog.FileName;
            }
            return path;
        }

        public async Task InitChips()
        {
            Chips.Clear();
            using (var db = new DbContext())
            {
                var chips = await db.Chips.Where(t => t.IsDeleted == "A").ToListAsync();
                Chips.AddRange(chips);
            }
        }
        public async Task<int> DelChip(int id)
        {
            var result = await mainService.ChipService.DeleteChip(id);
            if (result > 0)
            {
                await InitDataModelService.Instance.InitChipList(mainService.ChipService);
                InitChips();
                ClearForm();
            }
            return result;
        }

        private void ClearForm()
        {
            ChipId = 0;
            ChipName = "";
            FilePath = "";
            ParseFilePath = "";
        }
        #endregion

        #region DialogInfo

        public event Action<IDialogResult> RequestClose;

        public string Title => "故障信息";

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }
        public int AddOrEdit = 0;
        public void OnDialogOpened(IDialogParameters parameters)
        {
            ClearForm();
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await InitChips();
            });
        }

        #endregion
    }
}
