using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Shapes;
using Workbench.Models;
using Path = System.IO.Path;

namespace Workbench.Utils
{
    public class FileHandler
    {
        private readonly string _recentFileName = "recent.json";
        private string _recentFilePath = string.Empty;
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileHandler));

        public FileHandler()
        {
            _recentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _recentFileName);
        }

        /// <summary>
        /// 读取本地文件
        /// </summary>
        /// <param name="relativeFilePath"></param>
        /// <returns></returns>
        public string ReadLocalFile(string relativeFilePath)
        {
            var result = string.Empty;
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFilePath);
            if (File.Exists(path))
            {
                result = File.ReadAllText(path);
            }
            return result;
        }

        /// <summary>
        /// 读取本地文件返回指定类型集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relativeFilePath"></param>
        /// <returns></returns>
        public List<T> ReadLocalFile<T>(string relativeFilePath)
        {
            var result = new List<T>();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFilePath);
            if (File.Exists(path))
            {
                result = JsonHelper.DeserializeObject<List<T>>(File.ReadAllText(path));
            }
            return result;
        }

        public List<T> ReadResourceConfig<T>(string resourcePath, Assembly assembly = null)
        {
            var result = new List<T>();
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                    return result;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string text = reader.ReadToEnd();
                    if (text != null)
                    {
                        result = JsonHelper.DeserializeObject<List<T>>(text);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 读取本地文件返回指定类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relativeFilePath"></param>
        /// <returns></returns>
        public T ReadLocalFileObject<T>(string relativeFilePath)
        {
            var result = default(T);
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFilePath);
            if (File.Exists(path))
            {
                result = JsonHelper.DeserializeObject<T>(File.ReadAllText(path));
            }
            return result;
        }

        /// <summary>
        /// 添加到最近文件列表
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dirPath"></param>
        public void AddToRecentFile(string fileName, string dirPath, string uid)
        {
            var files = GetRecentFiles();
            files.Add(new RecentFile { FileName = fileName, DirPath = dirPath, DateTime = DateTime.Now, UID = uid });
            // 保存最近文件列表
            SaveRecentFiles(files);
        }

        /// <summary>
        /// 追加到最近文件列表
        /// </summary>
        /// <param name="project"></param>
        /// <param name="coverUid">被覆盖文件uid</param>
        internal void AppendToRecentFile(PPEC_Project project, string coverUid = "")
        {
            var recentFiles = GetRecentFiles();
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
            SaveRecentFiles(list);
        }

        /// <summary>
        /// 保存最近文件列表
        /// </summary>
        /// <param name="files"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SaveRecentFiles(List<RecentFile> files)
        {
            var content = JsonHelper.SerializeObject(files);
            using (StreamWriter writer = new StreamWriter(_recentFilePath, false))
            {
                writer.WriteLine(content);
            }
        }

        /// <summary>
        /// 获取最近文件列表
        /// </summary>
        /// <returns></returns>
        public List<RecentFile> GetRecentFiles()
        {
            var recentFileName = "recent.json";
            var recentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, recentFileName);
            if (File.Exists(recentFilePath))
            {
                try
                {
                    var content = File.ReadAllText(recentFilePath);
                    var files = JsonHelper.DeserializeObject<List<RecentFile>>(content);
                    files.ForEach(t => t.DateTimeStr = t.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    return files;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    return new List<RecentFile>();
                }
            }
            else
            {
                return new List<RecentFile>();
            }
        }

        internal void DeleteRecentFile(RecentFile recentFile)
        {
            var files = GetRecentFiles();
            var list = files.Where(x => x.UID != recentFile.UID).ToList();
            SaveRecentFiles(list);
        }

        /// <summary>
        /// 更新最近文件列表中项目的最近打开时间
        /// </summary>
        /// <param name="projectStr"></param>
        internal void UpdateRecentFileDatetime(string projectStr)
        {
            var recentFiles = GetRecentFiles();
            var project = JsonHelper.DeserializeObject<PPEC_Project>(projectStr);
            var recentFile = recentFiles.FirstOrDefault(t => t.UID == project.UID);
            recentFile.DateTime = DateTime.Now;
            SaveRecentFiles(recentFiles);
        }

        internal string GetDefaultFilePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Project";
        }
    }
}
