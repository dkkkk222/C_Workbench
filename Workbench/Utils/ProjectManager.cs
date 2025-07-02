using log4net;
using Newtonsoft.Json;
using PPEC.Communication;
using PPEC.Communication.CAN;
using PPEC.Communication.Enum;
using PPEC.Communication.Model;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unity;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils.Common;

namespace Workbench.Utils
{
    public class ProjectManager : BindableBase
    {
        private readonly FileHandler _fileHandler;
        private readonly IUnityContainer _container;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        private static readonly ILog _log = LogManager.GetLogger(typeof(ProjectManager));

        public ProjectManager(FileHandler fileHandler, IEventAggregator eventAggregator, IUnityContainer container, IDialogService dialogService)
        {
            _container = container;
            _fileHandler = fileHandler;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
        }

        #region Property

        /// <summary>
        /// 当前工程
        /// </summary>
        public PpecProject CurrentProject { get; set; }

        /// <summary>
        /// 当前PPEC
        /// </summary>
        private PpecProject _currentPPEC;
        public PpecProject CurrentPPEC
        {
            get { return _currentPPEC; }
            set
            {
                SetProperty(ref _currentPPEC, value);
                _eventAggregator.GetEvent<CurrentPpecChangedEvent>().Publish(value);
            }
        }

        /// <summary>
        /// 已打开工程列表
        /// </summary>
        public List<PpecProject> OpenedProjectList { get; set; } = new List<PpecProject>();

        #endregion
        #region Method

        /// <summary>
        /// 获取列表中的当前选中的PPEC
        /// </summary>
        /// <returns></returns>
        public PpecProject GetCachePPEC()
        {
            if (CurrentProject == null)
                return null;
            var project = OpenedProjectList.FirstOrDefault(t => t.UID == CurrentProject.UID);
            if (project != null)
            {
                return project.Children.FirstOrDefault(t => t.UID == CurrentPPEC?.UID);
            }
            return null;
        }


        /// <summary>
        /// 创建工程
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public bool CreateProject(PpecProject project)
        {
            var dirPath = project.Path;
            var fileName = project.Name;
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("请输入工程名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (string.IsNullOrEmpty(dirPath))
            {
                MessageBox.Show("请选择工程路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                if (!string.IsNullOrEmpty(dirPath) && !string.IsNullOrEmpty(fileName))
                {
                    SaveProject(project);
                    _fileHandler.AddToRecentFile(fileName, dirPath, project.UID);
                    _eventAggregator.GetEvent<AddedProjectEvent>().Publish(project);
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 保存工程
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public bool SaveProject(PpecProject project)
        {
            if (project == null)
            {
                MessageBox.Show("请选择待保存项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            var dirPath = project.Path;
            var fileName = project.Name;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var filePath = Path.Combine(dirPath, fileName + ".ppec");
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine(JsonHelper.SerializeObject(project, new JsonSerializerSettings()
                {
                    ContractResolver = new IgnorePropertyContractResolver(new[] { "_isSelected", "IsSelected" })
                }));
            }
            return true;
        }

        /// <summary>
        /// 打开工程
        /// </summary>
        /// <returns></returns>
        public bool OpenProject()
        {
            var content = string.Empty;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Files (*.ppec)|*.ppec|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = _fileHandler.GetDefaultFilePath();
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return false;

            var filePath = openFileDialog.FileName;
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);
                //更新最近文件列表中的时间
                _fileHandler.UpdateRecentFileDatetime(content);
                _eventAggregator.GetEvent<AddedProjectEvent>().Publish(JsonHelper.DeserializeObject<PpecProject>(content));
            }
            return true;
        }

        internal bool OpenRecentFile(RecentFile recentFile)
        {
            var filePath = Path.Combine(recentFile.DirPath, recentFile.FileName + ".ppec");
            if (File.Exists(filePath))
            {
                var projectStr = File.ReadAllText(filePath);
                //更新最近文件列表中的时间
                _fileHandler.UpdateRecentFileDatetime(projectStr);
                _eventAggregator.GetEvent<AddedProjectEvent>().Publish(JsonHelper.DeserializeObject<PpecProject>(projectStr));
                return true;
            }
            else
            {
                var result = MessageBox.Show("文件路径已变更，是否从列表中删除？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                var isDeleted = result == System.Windows.Forms.DialogResult.Yes;
                if (isDeleted)
                {
                    _fileHandler.DeleteRecentFile(recentFile);
                    //刷新页面最近列表
                    _eventAggregator.GetEvent<RefreshRecentFileEvent>().Publish();
                }

            }
            return true;
        }

        /// <summary>
        /// 另存为
        /// </summary>
        /// <param name="saveAsProject"></param>
        internal void SaveAsProject(PpecProject saveAsProject)
        {
            if (saveAsProject == null)
            {
                MessageBox.Show("请选择待保存项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PpecProject saveAsFileProject = null;
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "另存为",
                Filter = "Files (*.ppec)|*.ppec|All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() != true) return;
            var fileName = saveFileDialog.FileName;
            try
            {
                if (File.Exists(fileName))
                {
                    //不能覆盖已打开的工程文件
                    var saveAsFile = File.ReadAllText(fileName);
                    saveAsFileProject = JsonHelper.DeserializeObject<PpecProject>(saveAsFile);
                    if (OpenedProjectList.Any(t => t.UID == saveAsFileProject.UID))
                    {
                        MessageBox.Show("不能覆盖已打开的工程文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                var project = JsonHelper.DeepClone(saveAsProject);
                project.Path = Path.GetDirectoryName(fileName);
                project.Name = Path.GetFileNameWithoutExtension(fileName);
                project.Label = project.Name;
                //生成新的uid
                project.UID = Guid.NewGuid().ToString();
                //更新最近文件列表
                _fileHandler.AppendToRecentFile(project, saveAsFileProject?.UID);
                //保存文件
                SaveProject(project);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        internal string ChooseDirectory()
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

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync(PpecProject cachePpec)
        {
            if (cachePpec.IsTrueConnected)
            {
                cachePpec.Disconnect();
                SetCurrentPpec(cachePpec);
                return;
            }
            await CreateMasterAsync(cachePpec);

        }

        public void SetCurrentPpec(PpecProject ppec)
        {
            CurrentPPEC = ppec;
        }

        public async Task CreateMasterAsync(PpecProject cachePpec)
        {
            ITopologyMaster master = null;
            switch (cachePpec.CommunicationType)
            {
                case Constants.Modbus:
                    master = _container.Resolve<ITopologyMaster>();
                    master.Id = GetMasterId(cachePpec.Label);
                    var comMaster = _container.Resolve<ConcurrentComMaster>();
                    comMaster.initConfig(cachePpec.PortName ?? SerialPortHelper.GetPortNames().FirstOrDefault());
                    comMaster.CreateMaster();
                    master.ComMaster = comMaster;
                    break;
                case Constants.CAN:
                    master = _container.Resolve<CANMaster>();
                    master.ComMaster = CreateCANComMaster();
                    break;
                default:
                    break;
            }
            master.Start();
            cachePpec.Master = master;
        }

        private TopologyId GetMasterId(string label)
        {
            var ppec = label.Replace("-", "");
            TopologyId id = (TopologyId)Enum.Parse(typeof(TopologyId), ppec);
            return id;
        }

        private ICommunicationMaster CreateCANComMaster()
        {
            throw new NotImplementedException();
        }
        #endregion

        /// <summary>
        /// 获取分类树结构
        /// </summary>
        /// <returns></returns>
        internal List<SingleParamTree> GetChipCategoryTree()
        {
            var list = new List<SingleParamTree>();
            var infos = CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).ToList();
            var categories = infos.Select(t => t.Category).Distinct().ToList();
            foreach (var category in categories)
            {
                list.Add(new SingleParamTree()
                {
                    Title = category,
                    Children = GetSubCategory(category, infos)
                });
            }
            return list;
        }

        private List<SingleParamTree> GetSubCategory(string category, List<RegisterAddrInfo> infos)
        {
            var list = new List<SingleParamTree>();

            var subCategories = infos.Where(t => t.Category == category).Select(t => t.SubCategory).Distinct().ToList();
            foreach (var subCategory in subCategories)
            {
                list.Add(new SingleParamTree()
                {
                    Title = subCategory,
                    Children = GetRegister(subCategory, infos)
                });
            }

            return list;
        }

        private List<SingleParamTree> GetRegister(string subCategory, List<RegisterAddrInfo> infos)
        {
            var list = new List<SingleParamTree>();

            var registers = infos.Where(t => t.SubCategory == subCategory).Select(t => t.Name).Distinct().ToList();
            foreach (var register in registers)
            {
                list.Add(new SingleParamTree()
                {
                    Title = register,
                });
            }

            return list;
        }

    }
}
