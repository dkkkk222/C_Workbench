using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Models
{
    public abstract class AvaDocument : BindableBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private string _contentId;
        public string ContentId
        {
            get => _contentId;
            set => SetProperty(ref _contentId, value);
        }

        private PPEC_Project _project;
        public PPEC_Project Project
        {
            get => _project;
            set => SetProperty(ref _project, value);
        }

        /// <summary>
        /// Tab对象被添加到栏位上时会调用这个方法
        /// 触发这个方法的时机在ContentViewModel中
        /// </summary>
        public abstract void LoadData();
    }
}
