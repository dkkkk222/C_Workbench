using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models.dw;

namespace Workbench.Utils
{
    public class TreeSearcher
    {
        public List<CategoryTree> SearchInForest(List<CategoryTree> trees, string keyword)
        {
            if (trees == null || string.IsNullOrEmpty(keyword))
            {
                return new List<CategoryTree>();
            }

            var results = new List<CategoryTree>();
            foreach (var tree in trees)
            {
                var filteredTree = FilterNode(tree, keyword);
                if (filteredTree != null)
                {
                    results.Add(filteredTree);
                }
            }
            return results;
        }

        private CategoryTree FilterNode(CategoryTree node, string keyword)
        {
            if (node == null)
            {
                return null;
            }

            // 检查当前节点是否匹配。
            bool selfMatches = node.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || node.AddressDec.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || node.AddressHex.Contains(keyword, StringComparison.OrdinalIgnoreCase);

            // 如果当前节点匹配，我们就不需要再对子节点进行过滤了。
            // 直接返回一个包含所有原始子树的节点副本。
            if (selfMatches)
            {
                // 创建一个新节点副本，并直接附加其所有的原始子节点。
                // 这是一个深拷贝的简化，返回一个包含原始子树引用（未过滤）的新父节点。
                // 这确保了如果父节点匹配，其整个原始分支都会被保留。
                var newNode = new CategoryTree()
                {
                    Title = node.Title,
                    Type = node.Type,
                    AddressDec = node.AddressDec,
                    AddressHex = node.AddressHex,
                    Children = node.Children
                };
                return newNode;
            }

            // 如果当前节点不匹配，我们需要检查其后代是否匹配。
            // 所以，我们递归地过滤子节点。
            var filteredChildren = new List<CategoryTree>();
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var filteredChild = FilterNode(child, keyword);
                    if (filteredChild != null)
                    {
                        filteredChildren.Add(filteredChild);
                    }
                }
            }

            // 如果有任何一个子路径是有效的，则保留当前节点（作为路径的一部分），
            // 并将过滤后的子节点附加给它。
            if (filteredChildren.Any())
            {
                var newNode = new CategoryTree()
                {
                    Title = node.Title,
                    Type = node.Type,
                    AddressDec = node.AddressDec,
                    AddressHex = node.AddressHex,
                    Children = new ObservableCollection<CategoryTree>(filteredChildren)
                };
                return newNode;
            }

            // 如果自己不匹配，后代也不匹配，则彻底丢弃该节点。
            return null;
        }
    }
}
