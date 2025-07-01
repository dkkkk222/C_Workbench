using PPEC.Communication;
using PPEC.Communication.DB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Utils.Common;

namespace Workbench.ViewModels
{
    public class CreateProjectViewModel : BindableBase, IDialogAware
    {
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        public CreateProjectViewModel(FileHandler fileHandler, ProjectManager projectManager)
        {
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            InitData();
        }

        public void InitData()
        {
            ChipTypeSource.Clear();
            foreach (var chip in InitDataModelService.Instance.ListChip)
            {
                ChipTypeSource.Add(new ValueName { Value = chip.Id, Name = chip.Name, Label = chip.FilePath });
            }

            SelectChipType = ChipTypeSource.First();
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
            _createCommand ?? (_createCommand = new DelegateCommand(() =>
            {
                if (SelectChipType == null)
                    return;
                var projectId = Guid.NewGuid().ToString();
                var ppecId = Guid.NewGuid().ToString();
                var selectChip = InitDataModelService.Instance.DicChipAddress[(int)SelectChipType.Value];
                ChipInfo CreateChipInfo = new ChipInfo();
                CreateChipInfo.ChipId = (int)SelectChipType.Value;
                CreateChipInfo.ChipName = SelectChipType.Name.ToString();
                CreateChipInfo.ChipPath = SelectChipType.Label;
                CreateChipInfo.ChipRegisterInfo = selectChip.Select(r => r.DeepClone()).ToList();
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
        }

        //private void InitData()
        //{
        //    var path = "Data/PPEC_Data.json";
        //    var data = _fileHandler.ReadLocalFile(path);
        //    if (!string.IsNullOrEmpty(data))
        //    {
        //        _data = JsonHelper.DeserializeObject<List<PPEC_Data>>(data);
        //        ExtrackData();
        //    }
        //}

        private void ExtrackData()
        {
            //if (!_data.Any()) return;
            //PpecData.Clear();
            //if (SelectedOrderItem.Value == "byTopo")
            //{
            //    foreach (var item in _data)
            //    {
            //        PpecData.Add(new Display_PPEC_Data()
            //        {
            //            Icon = "\xe600",
            //            Title = item.Title,
            //            Content = item.Desc,
            //            Tags = item.Tags,
            //            Type = item.Type,
            //            PPEC = item.Ppec
            //        });
            //    }
            //}
            //else
            //{
            //    var group = _data.GroupBy(t => t.Ppec).ToDictionary(t => t.Key, t => t.ToList());
            //    foreach (var kv in group)
            //    {
            //        var item = kv.Value.First();
            //        PpecData.Add(new Display_PPEC_Data()
            //        {
            //            Icon = "\xef4a",
            //            Title = kv.Key,
            //            Content = $"适用领域：{item.ChipDesc}",
            //            Tags = kv.Value.Select(t => t.Title).ToList(),
            //            Type = item.Type,
            //            PPEC = item.Ppec
            //        });
            //    }
            //}
            //SelectedPpec = PpecData.First();
        }

        #endregion


    }
}
