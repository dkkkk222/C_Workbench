using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;

namespace Workbench.Utils
{
    public class Pinia
    {
        /// <summary>
        /// 当前工程
        /// </summary>
        public PPEC_Project CurrentProject { get; set; }

        /// <summary>
        /// 当前PPEC
        /// </summary>
        public PPEC_Project CurrentPPEC { get; set; }

        /// <summary>
        /// 已打开工程列表
        /// </summary>
        public List<string> OpenedProjectUidList { get; set; } = new List<string>();

        public void ClearCurrent()
        {
            CurrentProject = null;
            CurrentPPEC = null;
        }

        public void SetCurrentProject(PPEC_Project project)
        {
            CurrentProject = project;
        }

        internal void SetCurrentPPEC(PPEC_Project currentPPEC)
        {
            CurrentPPEC = currentPPEC;
        }
    }
}
