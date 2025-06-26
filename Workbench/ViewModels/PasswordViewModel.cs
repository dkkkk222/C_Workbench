using PPEC.Communication;
using PPEC.Communication.Enum;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Windows.Forms;
using Workbench.Models.Enums;
using Workbench.Utils;
using DialogResult = Prism.Services.Dialogs.DialogResult;

namespace Workbench.ViewModels
{
    public class PasswordViewModel : BindableBase, IDialogAware
    {
        private readonly ProjectManager _projectManager;
        public PasswordViewModel(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }
        private int? _password;
        public int? Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

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

        private DelegateCommand _closeCommand;
        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, new DialogParameters()));
            }));

        private DelegateCommand _okCommand;
        public DelegateCommand OkCommand =>
            _okCommand ?? (_okCommand = new DelegateCommand(() =>
            {
                if (!Password.HasValue)
                {
                    MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var ppec = _projectManager.GetCachePPEC();
                //校验密码
                var checkResult = UtilsFunc.CheckPassword(Password.Value, ppec.Master);
                if (!checkResult)
                {
                    MessageBox.Show("密码不正确", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //记录密码
                ppec.Password = Password;
                ppec.LastPortName = ppec.PortName;
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }));
    }
}
