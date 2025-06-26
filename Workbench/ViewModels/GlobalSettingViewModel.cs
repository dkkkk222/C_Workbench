using Newtonsoft.Json;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models;
using Workbench.Utils;

namespace Workbench.ViewModels
{
    public class GlobalSettingViewModel : BindableBase, IDialogAware
    {
        public GlobalSettingViewModel()
        {
            GetLocalSettingInfo();
        }

        #region Properties

        public StaticUpgrade currentProInfo;
        string parentDirectoryFileName = "";

        private bool _isCheckUpdate;
        public bool IsCheckUpdate
        {
            get { return _isCheckUpdate; }
            set
            {
                SetProperty(ref _isCheckUpdate, value);
                UpdateLocalJson();
            }
        }

        #endregion
        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        #region Methods

        public void UpdateLocalJson()
        {
            var tempState = IsCheckUpdate ? "true" : "false";
            if (currentProInfo == null) return;
            if (currentProInfo.IsCheckUpdate == tempState)
                return;
            currentProInfo.IsCheckUpdate = tempState;
            string updatedJsonContent = JsonConvert.SerializeObject(currentProInfo, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if (File.Exists(parentDirectoryFileName))
                File.WriteAllText(parentDirectoryFileName, updatedJsonContent);
        }

        public void GetLocalSettingInfo()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;//当前程序目录
            DirectoryInfo specificDir = new DirectoryInfo(currentDirectory);
            DirectoryInfo specificParentDir = specificDir.Parent;

            parentDirectoryFileName = Path.Combine(specificParentDir.FullName, "procedure.json");
            if (File.Exists(parentDirectoryFileName))
            {
                currentProInfo = UtilsFunc.GetLocalInfo<StaticUpgrade>(parentDirectoryFileName);

                if (currentProInfo.IsCheckUpdate == "true")
                {
                    IsCheckUpdate = true;
                }
                else
                {
                    IsCheckUpdate = false;
                }

            }

        }

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

        #endregion


    }
}
