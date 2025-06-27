using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using PPEC.Communication.Common;
using PPEC.Communication.DB;
using PPEC.Communication.DB.Model;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Utils;
using static LinqToDB.Reflection.Methods.LinqToDB;

namespace Workbench.ViewModels
{
    public class ChipManagerViewModel : BindableBase, IDialogAware
    {
        public MainServices mainService { get; set; }
        public ChipManagerViewModel(FileHandler fileHandler, IContainerProvider containerProvider)
        {
            InitChips();
            mainService = containerProvider.Resolve<MainServices>();
        }
        public ObservableCollection<smls_chip> _chips=new ObservableCollection<smls_chip>();
        public ObservableCollection<smls_chip> Chips
        {
            get=> _chips;
            set=>SetProperty(ref _chips,value);
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

        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
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
            try
            {
                if (ChipId > 0)
                {
                    var isUpdate = mainService.ChipService.UpdateChip(ChipId, ChipName, FilePath).GetAwaiter();
                    if (isUpdate.GetResult() > 0)
                    {
                        var editChip = InitDataModelService.Instance.ListChip.Where(x => x.Id == ChipId).FirstOrDefault();
                        editChip.Name = ChipName;
                        if (editChip.FilePath != FilePath)
                        {
                            editChip.FilePath = FilePath;
                            RegisterExcelParser rep = new RegisterExcelParser();
                            var list = rep.Parse(FilePath);
                            InitDataModelService.Instance.DicChipAddress[ChipId] = list;
                        }

                        InitChips();
                    }
                }
                if (ChipId <= 0)
                {
                    var isAdd = mainService.ChipService.AddChip(ChipName, FilePath).GetAwaiter();
                    var addChipid = isAdd.GetResult();
                    if (addChipid > 0)
                    {
                        await InitDataModelService.Instance.InitChipList(mainService.ChipService);
                        InitChips();

                        ChipId = 0;
                        ChipName = "";
                        FilePath = "";
                    }
                }
            }
            catch (Exception ex) 
            {
            }
            
        });
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
        public string ChooseDirectory()
        {
            var path = string.Empty;
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                path = folderBrowserDialog.SelectedPath;
            }
            return path;
        }

        public void InitChips()
        {
            Chips.Clear();
            Chips = new ObservableCollection<smls_chip>(InitDataModelService.Instance.ListChip);
        }
        public async Task<int> DelChip(int id)
        {
            var result = await mainService.ChipService.DeleteChip(id);
            if (result > 0)
            {
                await InitDataModelService.Instance.InitChipList(mainService.ChipService);
                InitChips();
                ChipId = 0;
                ChipName = "";
                FilePath = "";
            }
            return result;
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
            ChipName = "";
            FilePath = "";
            ChipId = -1;
        }

        #endregion
    }
}
