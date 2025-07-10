using log4net;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Workbench.Models;

namespace Workbench.Utils
{
    public class MessageManager : IDisposable
    {
        private readonly Pinia _cache;
        private readonly FileHandler _fileHandler;
        private static readonly ILog _log = LogManager.GetLogger(typeof(MessageManager));
        public MessageManager(FileHandler fileHandler, Pinia cache)
        {
            _cache = cache;
            _fileHandler = fileHandler;
        }

        /// <summary>
        /// 获取当前运行程序路径
        /// </summary>
        /// <returns></returns>
        internal string GetDefaultFilePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Project";
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        /// <returns></returns>
        internal string ChooseDirectory()
        {
            var path = string.Empty;
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                path = folderBrowserDialog.SelectedPath;
            }
            return path;
        }

        /// <summary>
        /// 创建工程
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        internal string CreateProject(PpecProject project)
        {
            var dirPath = project.Path;
            var fileName = project.Name;
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("请输入工程名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "false";
            }
            if (string.IsNullOrEmpty(dirPath))
            {
                MessageBox.Show("请选择工程路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "false";
            }
            try
            {
                if (!string.IsNullOrEmpty(dirPath) && !string.IsNullOrEmpty(fileName))
                {
                    SaveProject(project);
                    _fileHandler.AddToRecentFile(fileName, dirPath, project.UID);
                }
                return "true";
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return "false";
            }
        }

        public void Dispose()
        {
            Dispose();
        }

        /// <summary>
        /// 获取最近文件列表
        /// </summary>
        /// <returns></returns>
        internal string GetRecentFiles()
        {
            var files = _fileHandler.GetRecentFiles();
            return JsonHelper.SerializeObject(files.OrderByDescending(t => t.DateTime));
        }

        internal string OpenFile()
        {
            var content = string.Empty;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Files (*.sdpc)|*.sdpc|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = GetDefaultFilePath();
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return content;

            var filePath = openFileDialog.FileName;
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);

                //更新最近文件列表中的时间
                _fileHandler.UpdateRecentFileDatetime(content);
            }
            return content;
        }

        internal string OpenRecentFile(RecentFile recentFile)
        {
            var content = string.Empty;
            var filePath = Path.Combine(recentFile.DirPath, recentFile.FileName + ".adpc");
            if (File.Exists(filePath))
            {
                var projectStr = File.ReadAllText(filePath);
                content = JsonHelper.SerializeObject(new { HasFile = true, Content = projectStr });
                _fileHandler.UpdateRecentFileDatetime(projectStr);
                return content;
            }
            else
            {
                var result = MessageBox.Show("文件路径已变更，是否从列表中删除？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                var isDeleted = result == DialogResult.Yes;
                content = JsonHelper.SerializeObject(new { HasFile = false, IsDeleted = isDeleted });
                if (isDeleted)
                    _fileHandler.DeleteRecentFile(recentFile);

            }
            return content;
        }

        /// <summary>
        /// 保存工程文件
        /// </summary>
        /// <param name="currentProject">工程对象</param>
        internal void SaveProject(PpecProject currentProject)
        {
            if (currentProject == null)
            {
                MessageBox.Show("请先打开或新建项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dirPath = currentProject.Path;
            var fileName = currentProject.Name;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var filePath = Path.Combine(dirPath, fileName + ".sdpc");
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                // 序列化为json字符串，属性首字母小写，驼峰式命名
                writer.WriteLine(JsonHelper.SerializeObject(currentProject));
            }
        }

        /// <summary>
        /// 追加到最近文件列表
        /// </summary>
        /// <param name="project"></param>
        /// <param name="coverUid">被覆盖文件uid</param>
        internal void AppendToRecentFile(PpecProject project, string coverUid = "")
        {
            var recentFiles = _fileHandler.GetRecentFiles();
            var list = recentFiles;
            if (!string.IsNullOrEmpty(coverUid))
                //删除被覆盖文件记录
                list = recentFiles.Where(t => t.UID != coverUid).ToList();
            //重新添加新的记录
            list.Add(new RecentFile
            {
                FileName = project.Name,
                DirPath = project.Path,
                DateTime = DateTime.Now,
                UID = project.UID
            });
            _fileHandler.SaveRecentFiles(list);
        }

        internal void SaveAsProject(Action postMessageAction, PpecProject saveAsProject)
        {
            if (saveAsProject == null)
            {
                MessageBox.Show("请先打开或新建项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    if (_cache.OpenedProjectUidList.Contains(saveAsFileProject.UID))
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
                AppendToRecentFile(project, saveAsFileProject?.UID);
                //保存文件
                SaveProject(project);
                //刷新界面最近文件列表
                postMessageAction();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        internal void RenameProject(PpecProject project, string oldName)
        {
            //保存工程文件
            SaveProject(project);

            //更新最近文件列表中的文件名
            var recentFiles = _fileHandler.GetRecentFiles();
            var recentFile = recentFiles.FirstOrDefault(t => t.UID == project.UID);
            recentFile.FileName = project.Name ?? project.Label;
            recentFile.DateTime = DateTime.Now;
            _fileHandler.SaveRecentFiles(recentFiles);

            //删除原来的工程文件
            var filePath = Path.Combine(project.Path, oldName + ".sdpc");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
