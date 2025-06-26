using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Workbench.Models;
using Workbench.Models.Data;
using Workbench.Utils;
using Workbench.Utils.Common;

namespace Workbench.ViewModels
{
    public class CreateProjectViewModel : BindableBase, IDialogAware
    {
        private readonly FileHandler _fileHandler;
        private readonly ProjectManager _projectManager;
        private static List<PPEC_Data> _data = new List<PPEC_Data>();
        public CreateProjectViewModel(FileHandler fileHandler, ProjectManager projectManager)
        {
            _fileHandler = fileHandler;
            _projectManager = projectManager;
            OrderItems = new ObservableCollection<ValueName>
            {
                new ValueName { Value = "byTopo", Name = "按拓扑" },
                new ValueName { Value = "byChip", Name = "按芯片" }
            };
            SelectedOrderItem = OrderItems.First();
            InitData();
        }

        #region Property

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        private ObservableCollection<ValueName> _orderItems;
        public ObservableCollection<ValueName> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
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

        private ValueName _selectedOrderItem;
        public ValueName SelectedOrderItem
        {
            get => _selectedOrderItem;
            set
            {
                SetProperty(ref _selectedOrderItem, value);
                IsSelectedTopo = (string)value.Value == "byTopo";
                ExtrackData();
            }
        }

        private Display_PPEC_Data _selectedPpec;
        public Display_PPEC_Data SelectedPpec
        {
            get => _selectedPpec;
            set
            {
                if (value != null)
                {
                    SvgPath = $"/Resource/Images/SVG/{value.Type}.svg";
                }
                SetProperty(ref _selectedPpec, value);
            }
        }

        private ObservableCollection<Display_PPEC_Data> _ppecData = new ObservableCollection<Display_PPEC_Data>();
        public ObservableCollection<Display_PPEC_Data> PpecData
        {
            get => _ppecData;
            set => SetProperty(ref _ppecData, value);
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
                var projectId = Guid.NewGuid().ToString();
                var ppecId = Guid.NewGuid().ToString();
                var project = new PPEC_Project()
                {
                    UID = projectId,
                    Name = FileName,
                    Path = FilePath,
                    Icon = IconUnicode.Project,
                    Label = FileName,
                    Level = ProjectLevel.Project,
                    Children = new ObservableCollection<PPEC_Project>() { new PPEC_Project()
                    {
                        UID = ppecId,
                        Name = SelectedPpec.PPEC,
                        Icon = IconUnicode.PPEC,
                        Label = SelectedPpec.PPEC,
                        Level = ProjectLevel.PPEC,
                        ProjectId = projectId,
                        Children = new ObservableCollection<PPEC_Project>()
                        {
                            new PPEC_Project()
                            {
                                UID = Guid.NewGuid().ToString(),
                                Name = "开发",
                                Label = "开发",
                                Level = ProjectLevel.Develop,
                                Icon = IconUnicode.Develop,
                                PPEC_Id = ppecId,
                                ProjectId = projectId
                            },
                            new PPEC_Project()
                            {
                                UID = Guid.NewGuid().ToString(),
                                Name = "调试",
                                Label = "调试",
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

        private void InitData()
        {
            var path = "Data/PPEC_Data.json";
            var data = _fileHandler.ReadLocalFile(path);
            if (!string.IsNullOrEmpty(data))
            {
                _data = JsonHelper.DeserializeObject<List<PPEC_Data>>(data);
                ExtrackData();
            }
        }

        private void ExtrackData()
        {
            if (!_data.Any()) return;
            PpecData.Clear();
            if (SelectedOrderItem.Value == "byTopo")
            {
                foreach (var item in _data)
                {
                    PpecData.Add(new Display_PPEC_Data()
                    {
                        Icon = "\xe600",
                        Title = item.Title,
                        Content = item.Desc,
                        Tags = item.Tags,
                        Type = item.Type,
                        PPEC = item.Ppec
                    });
                }
            }
            else
            {
                var group = _data.GroupBy(t => t.Ppec).ToDictionary(t => t.Key, t => t.ToList());
                foreach (var kv in group)
                {
                    var item = kv.Value.First();
                    PpecData.Add(new Display_PPEC_Data()
                    {
                        Icon = "\xef4a",
                        Title = kv.Key,
                        Content = $"适用领域：{item.ChipDesc}",
                        Tags = kv.Value.Select(t => t.Title).ToList(),
                        Type = item.Type,
                        PPEC = item.Ppec
                    });
                }
            }
            SelectedPpec = PpecData.First();
        }

        #endregion


    }
}
