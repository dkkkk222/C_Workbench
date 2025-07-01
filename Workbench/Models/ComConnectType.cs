using Prism.Mvvm;

namespace Workbench.Models
{
    public class ComConnectType : BindableBase
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}
