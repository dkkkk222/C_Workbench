using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Workbench.Events;
using Workbench.Utils;

namespace Workbench.ViewModels
{
    public class SplashWindowViewModel : BindableBase, IDialogAware
    {
        private int dotCount = 0;
        public SplashWindowViewModel()
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (s, e) =>
            {
                dotCount = dotCount >= 3 ? 1 : dotCount + 1;
                Content = TextManager.InitializeText + new string('.', dotCount);
            };
            timer.Start();
        }

        private string _content = string.Empty;

        public event Action<IDialogResult> RequestClose;

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string Title => throw new NotImplementedException();

        public bool CanCloseDialog()
        {
            return false;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}
