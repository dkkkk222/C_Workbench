using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.ViewModels
{
    public class RenameViewModel : BindableBase, IDialogAware
    {
        #region Properties
        public string _NameType;
        public string NameType
        {
            get => _NameType;
            set => SetProperty(ref _NameType, value);
        }

        public string _ShowName;
        public string ShowName
        {
            get => _ShowName;
            set => SetProperty(ref _ShowName, value);
        }
        #endregion
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
            NameType = parameters.GetValue<string>("NameType");
        }

        #region Command
        public DelegateCommand ComfirCommand => new DelegateCommand(() =>
        {
            var parameters = new DialogParameters();
            parameters.Add("NameType", NameType);
            parameters.Add("ShowName", ShowName);
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        });

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null));
        });
        #endregion
    }
}
