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
using System.Web.UI.WebControls;
using System.Windows.Forms;
using Unity;
using Workbench.Events;
using Workbench.Models;
using Workbench.Models.dw;
using Workbench.Utils.Common;
using Workbench.ViewModels.dw;

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
            //if (CurrentProject == null)
            //    return null;
            //var project = OpenedProjectList.FirstOrDefault(t => t.UID == CurrentProject.UID);
            //if (project != null)
            //{
            //    return project.Children.FirstOrDefault(t => t.UID == CurrentPPEC?.UID);
            //}
            //return null;
            return CurrentProject;
        }

        /// <summary>
        /// 移除工程
        /// </summary>
        /// <param name="project"></param>
        public async Task RemoveProject(PpecProject project)
        {
            if(CurrentProject.UID== project.UID)//移除当前工程，先要断开连接
            {
                await AsyncDisConnect();
                RemoveTabAndProject(project);
            }
            else
            {
                RemoveTabAndProject(project);
            }
            
        }
        public void RemoveTabAndProject(PpecProject project)
        {
            _eventAggregator.GetEvent<RemovePpecEvent>().Publish(project.Children[0].PPEC_Id);
            _eventAggregator.GetEvent<RemoveProjectFromSiderEvent>().Publish(project.UID);
            var removProject = OpenedProjectList.FirstOrDefault(x => x.UID == project.UID);
            OpenedProjectList.Remove(removProject);
        }
        private async Task AsyncDisConnect()
        {
            _eventAggregator.GetEvent<CloseConnectEvent>().Publish();
            await Task.Delay(200);
            CurrentProject.Disconnect();
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
            project.WatchChartGroups = new ObservableCollection<WatchChartModel>(project.WatchChartGroups.Where(m => m.Id!= "placeholder" && m.Header!="未选中"));
            var dirPath = project.Path;
            var fileName = project.Name;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var filePath = Path.Combine(dirPath, fileName + ".sdpc");
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
            openFileDialog.Filter = "Files (*.sdpc)|*.sdpc|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = _fileHandler.GetDefaultFilePath();
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return false;

            var filePath = openFileDialog.FileName;
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);
                var openProject = JsonHelper.DeserializeObject<PpecProject>(content);
                
                string directoryPath = Path.GetDirectoryName(filePath);
                openProject.Path= directoryPath;               
                var newPathProject=JsonHelper.SerializeObject(openProject, new JsonSerializerSettings()
                {
                    ContractResolver = new IgnorePropertyContractResolver(new[] { "_isSelected", "IsSelected" })
                });

                //var isHaveChipType = InitDataStaticService.Instance.ChipTypeSource.FirstOrDefault(x => x.Value.ToString() == openProject.Chip.ChipId);
                //验证ID改为验证名称
                var isHaveChipType = InitDataStaticService.Instance.ChipTypeSource.FirstOrDefault(x => x.Name.ToString() == openProject.Chip.ChipName);
                if (isHaveChipType == null)
                {
                    var result = MessageBox.Show("该芯片类型不存在，无法打开！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                   
                    return true;
                }
                //更新最近文件列表中的时间
                _fileHandler.UpdateRecentFileDatetime(newPathProject);
         
                _eventAggregator.GetEvent<AddedProjectEvent>().Publish(openProject);
            }
            return true;
        }

        internal bool OpenRecentFile(RecentFile recentFile)
        {
            var filePath = Path.Combine(recentFile.DirPath, recentFile.FileName + ".sdpc");
            if (File.Exists(filePath))
            {
                var projectStr = File.ReadAllText(filePath);
                //更新最近文件列表中的时间
                _fileHandler.UpdateRecentFileDatetime(projectStr);
                var project = JsonHelper.DeserializeObject<PpecProject>(projectStr);
                //var isHaveChipType = InitDataStaticService.Instance.ChipTypeSource.FirstOrDefault(x=>x.Value.ToString()== project.Chip.ChipId);
                var isHaveChipType = InitDataStaticService.Instance.ChipTypeSource.FirstOrDefault(x => x.Name.ToString() == project.Chip.ChipName);
                if (isHaveChipType==null)
                {
                    var result = MessageBox.Show("该芯片类型不存在，从列表中删除！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _fileHandler.DeleteRecentFile(recentFile);
                    //刷新页面最近列表
                    _eventAggregator.GetEvent<RefreshRecentFileEvent>().Publish();
                    return true;
                }
                _eventAggregator.GetEvent<AddedProjectEvent>().Publish(project);
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
                Filter = "Files (*.sdpc)|*.sdpc|All Files (*.*)|*.*"
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
                 
                UpdateAllProjectIds(project, project.UID);
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

        public void UpdateAllProjectIds(PpecProject root, string newProjectId)
        {
            Stack<PpecProject> stack = new Stack<PpecProject>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                current.ProjectId = newProjectId;

                if (current.Children != null)
                {
                    foreach (var child in current.Children)
                    {
                        stack.Push(child);
                    }
                }
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
                case Constants.OldSERIAL_PORT:
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

        public List<CategoryTree> GetChipCategoryTreeOnlyW(string ctg = null, string address = null, bool isOrderByAddress = true)
        {
            var list = new List<CategoryTree>();
            var infos = CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).Where(x=>x.RW.Contains('W')).ToList();
            if (!string.IsNullOrEmpty(ctg))
            {
                infos = infos.Where(t => t.Category == ctg).ToList();
            }
            if (!string.IsNullOrEmpty(address))
            {
                infos = infos.Where(t => t.AddressHex == address).ToList();
            }
            var categories = infos.Select(t => t.Category).Distinct().ToList();

            foreach (var category in categories)
            {
                list.Add(new CategoryTree()
                {
                    Title = category,
                    Type = CategoryTreeType.Category,
                    Children = GetSubCategory(category, infos, isOrderByAddress)
                });
            }
            // ★ 在这里给每棵根树补父引用
            foreach (var root in list)
                AttachParentRecursive(root, null);
            return list;
        }

        /// <summary>
        /// 获取分类树
        /// </summary>
        /// <param name="ctg">指定分类</param>
        /// <returns></returns>
        internal List<CategoryTree> GetChipCategoryTree(string ctg = null, string address = null, bool isOrderByAddress = true)
        {
            var list = new List<CategoryTree>();
            var infos = CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).ToList();
            if (!string.IsNullOrEmpty(ctg))
            {
                infos = infos.Where(t => t.Category == ctg).ToList();
            }
            if (!string.IsNullOrEmpty(address))
            {
                infos = infos.Where(t => t.AddressHex == address).ToList();
            }
            var categories = infos.Select(t => t.Category).Distinct().ToList();

            foreach (var category in categories)
            {
                list.Add(new CategoryTree()
                {
                    Title = category,
                    Type = CategoryTreeType.Category,
                    Children = GetSubCategory(category, infos, isOrderByAddress)
                });
            }
            // ★ 在这里给每棵根树补父引用
            foreach (var root in list)
                AttachParentRecursive(root, null);
            return list;
        }
        private void AttachParentRecursive(CategoryTree node, CategoryTree parent)
        {
            node.Parent = parent;
            if (node.Children == null) return;

            foreach (var child in node.Children)
                AttachParentRecursive(child, node);
        }
        private List<CategoryTree> GetSubCategory(string category, List<RegisterAddrInfo> infos, bool isOrderByAddress)
        {
            var list = new List<CategoryTree>();

            var subCategories = infos.Where(t => t.Category == category).Select(t => t.SubCategory).Distinct().ToList();
            foreach (var subCategory in subCategories)
            {
                list.Add(new CategoryTree()
                {
                    Title = subCategory,
                    Type = CategoryTreeType.SubCategory,
                    Children = GetRegister(category, subCategory, infos, isOrderByAddress)
                });
            }

            return list;
        }

        private List<CategoryTree> GetRegister(string category, string subCategory, List<RegisterAddrInfo> infos, bool isOrderByAddress)
        {
            var list = new List<CategoryTree>();

            var registers = infos.Where(t => t.Category == category && t.SubCategory == subCategory).ToList();
            if (isOrderByAddress)
            {
                registers = registers.OrderBy(t => t.AddressDec).ToList();
            }

            foreach (var register in registers)
            {
                list.Add(new CategoryTree()
                {
                    Title = register.Name,
                    Type = CategoryTreeType.Register,
                    AddressDec = register.AddressDec.ToString(),
                    AddressHex = register.AddressHex
                });
            }

            return list;
        }

        /// <summary>
        /// 获取分类
        /// </summary>
        /// <returns></returns>
        internal List<string> GetCategories()
        {
            return CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).Select(d => d.Category).Distinct().ToList();
        }

        /// <summary>
        /// 根据分类获取寄存器信息
        /// </summary>
        /// <param name="Categorie"></param>
        /// <returns></returns>
        internal List<RegisterAddrInfo> GetRegisterForCategories(string Categorie)
        {
            var returnRegists= CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).Where(x=>x.Category== Categorie).ToList();
            if (returnRegists == null)
                return null;
            returnRegists=returnRegists.OrderBy(x => x.AddressDec).ToList();
            foreach(var addressStr in returnRegists)
            {
                addressStr.ShowAddressStr = addressStr.AddressHex + " : " + addressStr.Name;
            }
            return returnRegists;
        }


        public RegisterAddrInfo GetRegisterValue(string registerName)
        {
            var register=CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.AddressHex == registerName);
            return register;
        }

        internal void SetRegisterValue(string registerName, uint value)
        {
            var register = CurrentProject.Chip.ChipRegisterInfo.Select(t => t.AddrInfo).FirstOrDefault(t => t.Name == registerName);
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                register.DecValue = value;
                register.HexValue = Utility.DecToHex(value);
                var tpl = Utility.ParseDecToBinary(value);
                register.BinaryStr = tpl.binaryString;
                var list = tpl.binaryArray.Select(t => new ObservableCollection<BitOption>(t));
                register.BinaryArray.Clear();
                register.BinaryArray.AddRange(list);
                var mirror = CurrentProject?.CategoryRegisters?
                        .FirstOrDefault(r => r.AddressDec == register.AddressDec);
                if (mirror != null && !ReferenceEquals(mirror, register))
                {
                    mirror.DecValue = value; // 若 HexValue 为派生属性，这一条就够了
                    mirror.HexValue = Utility.DecToHex(value);
                }
                ResolveBitFields(register);
            });
            
        }

        internal void SetWriteRegisterValue(RegisterAddrInfo writeRegister,string registerName, uint value)
        {
            writeRegister.DecValue = value;
            //writeRegister.HexValue = Utility.DecToHex(value);
            var tpl = Utility.ParseDecToBinary(value);
            writeRegister.BinaryStr = tpl.binaryString;
            var list = tpl.binaryArray.Select(t => new ObservableCollection<BitOption>(t));
            writeRegister.BinaryArray.Clear();
            writeRegister.BinaryArray.AddRange(list);

            ResolveBitFields(writeRegister);
        }

        private void ResolveBitFields(RegisterAddrInfo register)
        {
            foreach (var bf in register.BitFields)
            {
                if (bf.FieldType == FieldType.Option)
                {
                    var hex = Utility.BinaryToHex(bf.ReadBinary);
                    var option = bf.Options.FirstOrDefault(t => t.Key == hex);
                    if (option != null)
                    {
                        bf.ResolveStr = option.Label;
                    }
                    else
                    {
                        bf.ResolveStr = null;
                    }
                }
                else if (bf.FieldType == FieldType.Range)
                {
                    //将二进制转成十进制
                    var dec = Utility.BinaryToDec(bf.ReadBinary);
                    bf.ResolveStr = dec.ToString();
                }
            }
        }
    }
}
