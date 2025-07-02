using LinqToDB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Workbench.Db;
using Workbench.Db.IService;
using Workbench.Db.Tables;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Utils.Common;

namespace Workbench.ViewModels
{
    public class CreateProjectViewModel : BindableBase, IDialogAware
    {
        private readonly ICpService _cpService;
        private readonly ProjectManager _projectManager;
        public CreateProjectViewModel(ProjectManager projectManager, ICpService cpService)
        {
            _cpService = cpService;
            _projectManager = projectManager;
        }

        public async Task InitData()
        {
            ChipTypeSource.Clear();

            using (var db = new DbContext())
            {
                var chips = await db.Chips.Where(t => t.IsDeleted == "A").ToListAsync();
                foreach (var chip in chips)
                {
                    ChipTypeSource.Add(new ValueName { Value = chip.Id, Name = chip.Name, Label = chip.FileName });
                }
                SelectChipType = ChipTypeSource.FirstOrDefault();
            }
        }
        #region Property

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        private ObservableCollection<ValueName> _chipTypeSource = new ObservableCollection<ValueName>();
        public ObservableCollection<ValueName> ChipTypeSource
        {
            get => _chipTypeSource;
            set => SetProperty(ref _chipTypeSource, value);
        }

        private bool _isSelectedTopo;
        public bool IsSelectedTopo
        {
            get => _isSelectedTopo;
            set => SetProperty(ref _isSelectedTopo, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Project");
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private string _svgPath = "/Resource/Images/SVG/PSFB.svg";
        public string SvgPath
        {
            get => _svgPath;
            set => SetProperty(ref _svgPath, value);
        }
        private string _projectMark = "";
        public string ProjectMark
        {
            get => _projectMark;
            set => SetProperty(ref _projectMark, value);
        }

        private ValueName _selectChipType;
        public ValueName SelectChipType
        {
            get => _selectChipType;
            set
            {
                SetProperty(ref _selectChipType, value);
                //IsSelectedTopo = (string)value.Value == "byTopo";
                //ExtrackData();
            }
        }

        #endregion

        #region Command

        public DelegateCommand _closeCommand;
        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }));

        public DelegateCommand _browseCommand;
        public DelegateCommand BrowseCommand =>
            _browseCommand ?? (_browseCommand = new DelegateCommand(() =>
            {
                var path = _projectManager.ChooseDirectory();
                if (!string.IsNullOrEmpty(path))
                    FilePath = path;
            }));

        private DelegateCommand _createCommand;
        public DelegateCommand CreateCommand =>
            _createCommand ?? (_createCommand = new DelegateCommand(async () =>
            {
                if (SelectChipType == null)
                    return;
                var projectId = Guid.NewGuid().ToString();
                var ppecId = Guid.NewGuid().ToString();
                //var selectChip = InitDataModelService.Instance.DicChipAddress[(int)SelectChipType.Value];
                string chipId = SelectChipType.Value.ToString();
                var chip = await _cpService.GetChipById(chipId);
                ChipInfo CreateChipInfo = new ChipInfo();
                CreateChipInfo.ChipId = chip.Id;
                CreateChipInfo.ChipName = chip.Name;
                CreateChipInfo.ChipPath = chip.FileName;
                CreateChipInfo.ChipRegisterInfo = await _cpService.GetChipRegisters(chipId);
                var project = new PpecProject()
                {
                    UID = projectId,
                    Name = FileName,
                    Path = FilePath,
                    ProjectMark = ProjectMark,
                    Icon = IconUnicode.Project,
                    Label = FileName,
                    Level = ProjectLevel.Project,
                    Chip = CreateChipInfo,
                    Children = new ObservableCollection<PpecProject>() { new PpecProject()
                    {
                        UID = ppecId,
                        Name =CreateChipInfo.ChipName,// SelectedPpec.PPEC,
                        Icon = IconUnicode.PPEC,
                        Label =CreateChipInfo.ChipName,//  SelectedPpec.PPEC,
                        Level = ProjectLevel.PPEC,
                        ProjectId = projectId,
                        Children = new ObservableCollection<PpecProject>()
                        {
                            new PpecProject()
                            {
                                UID = Guid.NewGuid().ToString(),
                                Name = "单参数",
                                Label = "单参数",
                                Level = ProjectLevel.SingleParams,
                                Icon = IconUnicode.Develop,
                                PPEC_Id = ppecId,
                                ProjectId = projectId
                            },
                            new PpecProject()
                            {
                                UID = Guid.NewGuid().ToString(),
                                Name = "批量参数",
                                Label = "批量参数",
                                Level = ProjectLevel.BatchParams,
                                Icon = IconUnicode.Develop,
                                PPEC_Id = ppecId,
                                ProjectId = projectId
                            },
                            new PpecProject()
                            {
                                UID = Guid.NewGuid().ToString(),
                                Name = "状态监测",
                                Label = "状态监测",
                                Level = ProjectLevel.Debug,
                                Icon = IconUnicode.Debug,
                                PPEC_Id = ppecId,
                                ProjectId = projectId
                            }
                        }
                    }}
                };

                var result = _projectManager.CreateProject(project);
                if (result) RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }));

        #endregion

        #region Method

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await InitData();
            });
        }

        #endregion


    }
}
