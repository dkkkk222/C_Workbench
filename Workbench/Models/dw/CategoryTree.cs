using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Force.DeepCloner;
using Prism.Mvvm;
using Workbench.Utils;

namespace Workbench.Models.dw
{
    public class CategoryTree:BindableBase
    {
        public CategoryTree()
        {
            Children = new ObservableCollection<CategoryTree>();
            Children.CollectionChanged += Children_CollectionChanged;
        }
        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CategoryTree item in e.NewItems)
                {
                    item.Parent = this;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CategoryTree item in e.OldItems)
                {
                    if (item.Parent == this)
                        item.Parent = null;
                }
            }
        }
        public string Title { get; set; }
        public string Type { get; set; }
        public string AddressHex { get; set; }
        public string AddressDec { get; set; }
        public CategoryTree Parent { get; set; }
        public ObservableCollection<CategoryTree> Children
        { 
            get;
            set;
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isUpdating;
        private bool _isCheck=false;
        public bool IsCheck
        {
            get => _isCheck;
            set
            {
                if (!SetProperty(ref _isCheck, value))
                    return;

                // 内部更新时不再重复联动，防止递归
                if (_isUpdating)
                    return;

                try
                {
                    _isUpdating = true;

                    // ① 下传：父节点勾选 / 取消 → 所有子节点保持一致
                    foreach (var child in Children)
                    {
                        child.IsCheck = value;
                    }
                }
                finally
                {
                    _isUpdating = false;
                }

                // ② 上推：让父节点根据所有子节点的状态重新计算自己的 IsCheck
                Parent?.RefreshCheckFromChildren();
            }
        }
        public void RefreshCheckFromChildren()
        {
            if (Children == null || Children.Count == 0)
            {
                // 没有子节点就不计算
                return;
            }

            bool allChecked = Children.All(c => c.IsCheck);
            bool newValue = allChecked;  // 不用三态：不是全部选就是 false

            if (_isCheck == newValue)
            {
                // 状态没变就不用往上冒泡了
                return;
            }

            try
            {
                _isUpdating = true;
                IsCheck = newValue;
                //SetProperty(ref _isCheck, newValue, nameof(IsCheck));
                //SetProperty(ref _isCheck, newValue);
            }
            finally
            {
                _isUpdating = false;
            }

            // 自己变了以后，继续往上通知
            Parent?.RefreshCheckFromChildren();
        }
    }

    public class CategoryTreeType
    {
        public const string Type = "Type";
        public const string Category = "Category";
        public const string SubCategory = "SubCategory";
        public const string Register = "Register";

    }
}
