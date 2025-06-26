using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Utils;

namespace Workbench.ViewModels
{
    public class NodeRedViewModel : BindableBase, IDialogAware
    {
        private readonly CommandHandler _cmd;
        public NodeRedViewModel(CommandHandler cmd)
        {
            _cmd = cmd;
        }

        public string Title => string.Empty;

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            //_cmd.CloseCommandProcess();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}
