using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using log4net;
using PPEC.Communication.DB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Db.IService;
using Workbench.Db.Service;
using Workbench.Utils;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryAddViewModel : BindableBase, IDialogAware
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BindableBase));
        private ProjectManager _projectManager;
        private ICpService _cpService;
        public MainServices mainService { get; set; }

        private readonly IMapper _mapper;
        public TelemetryAddViewModel(FileHandler fileHandler, IContainerProvider containerProvider, IMapper mapper, ProjectManager projectManager, ICpService cpService)
        {
            _projectManager = projectManager;
            _mapper = mapper;
            _cpService = cpService;
        }

        #region pro
        public string _Name;
        public string Name
        {
            get=> _Name; set=>SetProperty(ref _Name,value);
        }

        public string _CodeName;
        public string CodeName
        {
            get => _CodeName; set => SetProperty(ref _CodeName, value);
        }

        public ObservableCollection<OptionModel> _CodeTypeSource = new ObservableCollection<OptionModel>()
        {
            new OptionModel()
        {
            Label = "遥控指令", Value = 0
        },
            new OptionModel()
        {
            Label = "注数指令", Value = 1
        }
        };
        public ObservableCollection<OptionModel> CodeTypeSource
        {
            get => _CodeTypeSource;
            set => SetProperty(ref _CodeTypeSource, value);
        }

        public OptionModel _CodeType;
        public OptionModel CodeType
        {
            get => _CodeType;
            set => SetProperty(ref _CodeType, value);
        }
        #endregion

        public DelegateCommand ComfirmCommand => new DelegateCommand(async () =>
        {
            if(string.IsNullOrEmpty(Name))
            {
                MessageBox.Show("请输入指令名称");
                return;
            }
            if (string.IsNullOrEmpty(CodeName))
            {
                MessageBox.Show("请输入指令码");
                return;
            }
            TelemetryCode telemetryCode= new TelemetryCode();
            telemetryCode.Name = Name;
            telemetryCode.Code= CodeName;
            telemetryCode.Type = CodeType.Value.ToString();
            telemetryCode.Length = CodeName.Length.ToString();
            telemetryCode.ChipId = _projectManager.CurrentProject.Chip.ChipId;
            await _cpService.AddTelemetryAsync(_projectManager.CurrentProject.Chip.ChipId, telemetryCode);
            RequestClose?.Invoke(new Prism.Services.Dialogs.DialogResult(ButtonResult.OK));
        });

        public DelegateCommand CloseCommand => new DelegateCommand(() =>
        {
            RequestClose?.Invoke(new Prism.Services.Dialogs.DialogResult(ButtonResult.Cancel));
        });
        #region DialogInfo

        public event Action<IDialogResult> RequestClose;

        public string Title => "指令信息";

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }
        public int AddOrEdit = 0;
        public async void OnDialogOpened(IDialogParameters parameters)
        {
            CodeType = CodeTypeSource[0];
        }

        #endregion
    }
}
