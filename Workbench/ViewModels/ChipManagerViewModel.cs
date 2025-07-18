using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using LinqToDB;
using LinqToDB.Data;
using log4net;
using PPEC.Communication.Common;
using PPEC.Communication.DB;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SixLabors.ImageSharp.ColorSpaces;
using Workbench.Db;
using Workbench.Db.Tables;
using Workbench.Utils;

namespace Workbench.ViewModels
{
    public class ChipManagerViewModel : BindableBase, IDialogAware
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        public MainServices mainService { get; set; }

        private readonly IMapper _mapper;
        public ChipManagerViewModel(FileHandler fileHandler, IContainerProvider containerProvider, IMapper mapper)
        {
            _mapper = mapper;
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

            Loading = true;
            await Task.Run(async () =>
            {
                await HandleChipAsync();
            });

            ClearForm();
            await InitChips();
            Loading = false;
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

                        if(register.AddressDec==108)
                        {
                            string a = "";
                        }
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

                MessageBox.Show("添加芯片成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Loading = false;
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
