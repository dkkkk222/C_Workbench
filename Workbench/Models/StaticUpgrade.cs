using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public class StaticUpgrade
    {
        /// <summary>
        /// 远程文件名称
        /// </summary>
        public string UpdateProNameZip { get; set; }
        /// <summary>
        /// 启动的文件名称
        /// </summary>
        public string UpdateProName { get; set; }
        /// <summary>
        /// 所打开EXE的文件夹，一般在当前文件夹内
        /// </summary>
        public string StartFolderName { get; set; }
        /// <summary>
        /// 比较的版本，0=更新程序，1=Workbench
        /// </summary>
        public string CompareInfo { get; set; }
        /// <summary>
        /// 是否检测更新
        /// </summary>
        public string IsCheckUpdate { get; set; }
        /// <summary>
        /// 不提示，直接更新
        /// </summary>
        public string IsDirectUpdate { get; set; }
    }

    public class Upgrade
    {
        /// <summary>
        /// 远程服务器地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 更新器版本信息
        /// </summary>
        public UpdateVersionInfo Updater { get; set; }

        /// <summary>
        /// workbench版本信息
        /// </summary>
        public UpdateVersionInfo Workbench { get; set; }
    }

    public class UpdateVersionInfo
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }
    }
}
