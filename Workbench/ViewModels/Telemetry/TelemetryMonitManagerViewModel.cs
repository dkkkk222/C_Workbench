using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using log4net;
using PPEC.Communication.DB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Db.IService;
using Workbench.Db.Service;
using Workbench.Db.Tables;
using Workbench.Events;
using Workbench.Utils;
using static LinqToDB.Reflection.Methods.LinqToDB;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryMonitManagerViewModel : BindableBase, IDialogAware
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TelemetryMonitManagerViewModel));
        private ProjectManager _projectManager;
        private ICpService _cpService;
        public MainServices mainService { get; set; }
        private IDialogService _dialogService;
        private readonly IMapper _mapper;
        private IEventAggregator _eventAggregator;

        public TelemetryMonitManagerViewModel(FileHandler fileHandler, IContainerProvider containerProvider, IMapper mapper, ProjectManager projectManager, ICpService cpService, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _projectManager = projectManager;
            _mapper = mapper;
            _cpService = cpService;
            _eventAggregator= eventAggregator;
        }

        public DelegateCommand ExportCommand => new DelegateCommand(() =>
        {
            try
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "请选择保存路径";
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = fbd.SelectedPath;
                    string SDPCfileNameTelemetryData = "SDPC_B10遥测数据表.xlsx";//数据解析
                    string filePath1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SDPCfileNameTelemetryData);
                    string fileName = Path.Combine(path, SDPCfileNameTelemetryData);
                    CopyFileSimple(filePath1, fileName);


                    MessageBox.Show("遥控指令导出完成!");
                }
            }
            catch (Exception ex)
            {
            }
        });

        public static void CopyFileSimple(string srcPath, string dstPath, bool overwrite = true)
        {
            if (!File.Exists(srcPath))
                throw new FileNotFoundException("源文件不存在", srcPath);

            var dstDir = Path.GetDirectoryName(dstPath);
            if (string.IsNullOrEmpty(dstDir))
                throw new IOException("目标路径必须包含文件名，例如 C:\\Out\\file.bin");
            Directory.CreateDirectory(dstDir); // 确保目标目录存在

            File.Copy(srcPath, dstPath, overwrite);
        }
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
        public DelegateCommand ImportCommand => new DelegateCommand(async () =>
        {
            try
            {
                string filePath = ChooseDirectory();
                if (string.IsNullOrEmpty(filePath))
                    return;
                RegisterExcelResolve registerExcelResolve = new RegisterExcelResolve();
                var telemetryMonit = registerExcelResolve.TelemetryMonit(filePath);
                List< ParamDict >ListParmDic=new List< ParamDict >();
                List< TelemetryMonit > ListTelemMon=new List< TelemetryMonit >();
                List<TelemetryTagTable> ListTelemTag = new List<TelemetryTagTable>();
                foreach (var param1 in telemetryMonit.Item1)
                {
                    ListParmDic.Add(new ParamDict
                    {
                        ChipId= _projectManager.CurrentProject.Chip.ChipId,
                        Name = param1.CodeName,
                        TypeCode=0
                    });
                    string telemetryId = Guid.NewGuid().ToString("N");
                    ListTelemMon.Add(new TelemetryMonit
                    {
                        Id = telemetryId,
                        ChipId = _projectManager.CurrentProject.Chip.ChipId,
                        Name = param1.CodeName,
                        Category=param1.Category,
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
                        Unit = param1.Unit,
                    });
                }
                //foreach (var param1 in telemetryMonit.Item2)
                //{
                //    string tagId = Guid.NewGuid().ToString("N");
                //    ListTelemTag.Add(new TelemetryTagTable()
                //    {
                //        Id= tagId,
                //        ChipId= _projectManager.CurrentProject.Chip.ChipId,
                //        Name=param1.Name
                //    });
                //}
                await _cpService.SaveParamsListAsync(_projectManager.CurrentProject.Chip.ChipId, ListParmDic);
                await _cpService.SaveTeleMonListAsync(_projectManager.CurrentProject.Chip.ChipId, ListTelemMon);
                //await _cpService.SaveTeleTagListAsync(_projectManager.CurrentProject.Chip.ChipId, ListTelemTag);
                _eventAggregator.GetEvent<TelemetryImportEvent>().Publish(_projectManager.CurrentProject);
                if(_projectManager.CurrentProject.IsConnecting)
                {
                    MessageBox.Show("遥控指令导入完成,请重新连接已更新!");
                }
                else
                {
                    MessageBox.Show("遥控指令导入完成!");
                }
                
            }
            catch (Exception ex)
            {

            }
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


        }

        #endregion
    }
}
