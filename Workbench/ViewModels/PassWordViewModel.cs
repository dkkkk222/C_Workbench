using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Workbench.ViewModels
{
    public class PassWordViewModel : BindableBase, IDialogAware
    {
        #region Properties
        public string DefaultPass = "SPDC";

        public string _PassWord;
        public string PassWord
        {
            get => _PassWord;
            set => SetProperty(ref _PassWord, value);
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
           
        }

        #region Command
        public void Confirm(bool isResult)
        {
            if(isResult)
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }
            else
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            }
        }
        public DelegateCommand ComfirCommand => new DelegateCommand(() =>
        {
            //var parameters = new DialogParameters();
            //parameters.Add("NameType", NameType);
            //parameters.Add("ShowName", ShowName);
            //RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        });

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null));
        });
        #endregion
    }
}
