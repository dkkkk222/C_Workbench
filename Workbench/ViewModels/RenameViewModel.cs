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
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
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

        private DelegateCommand _createCommand;
        public DelegateCommand CreateCommand =>
            _createCommand ?? (_createCommand = new DelegateCommand(() =>
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters { { "Name", Name } }));
                }
            }));

        private DelegateCommand _closeCommand;
        public DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }));
    }
}
