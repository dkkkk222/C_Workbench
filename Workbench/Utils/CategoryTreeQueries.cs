using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Models.dw;

namespace Workbench.Utils
{
    public static class CategoryTreeQueries
    {
        /* ========== A. 所有叶子（不看 IsCheck） ========== */
        public static IEnumerable<CategoryTree> GetAllLeaves(this IEnumerable<CategoryTree> roots)
        => roots == null ? Enumerable.Empty<CategoryTree>() : roots.SelectMany(GetAllLeaves);

        public static IEnumerable<CategoryTree> GetAllLeaves(this CategoryTree root)
        {
            if (root == null) yield break;

            var stack = new Stack<CategoryTree>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var children = node.Children;

                if (children == null || children.Count == 0)
                {
                    yield return node; // 叶子
                    continue;
                }

                // 压栈子节点（容错 null）
                for (int i = 0; i < children.Count; i++)
                {
                    var ch = children[i];
                    if (ch != null) stack.Push(ch);
                }
            }
        }

        /* ========== B. 叶子且勾选（IsCheck==true） ========== */
        public static IEnumerable<CategoryTree> GetCheckedLeaves(this IEnumerable<CategoryTree> roots)
            => roots?.SelectMany(GetCheckedLeaves) ?? Enumerable.Empty<CategoryTree>();

        public static IEnumerable<CategoryTree> GetCheckedLeaves(this CategoryTree node)
        {
            if (node == null) yield break;

            var children = node.Children;
            if (children == null || children.Count == 0)
            {
                if (node.IsCheck) yield return node;
                yield break;
            }

            foreach (var ch in children)
            {
                foreach (var n in GetCheckedLeaves(ch))
                    yield return n;
            }
        }

        /* ========== C. 最下级的已选节点（父子都勾选只取子辈） ========== */
        public static IEnumerable<CategoryTree> GetDeepestChecked(this IEnumerable<CategoryTree> roots)
        {
            if (roots == null) yield break;
            foreach (var r in roots)
                foreach (var n in GetDeepestChecked(r))
                    yield return n;
        }

        public static IEnumerable<CategoryTree> GetDeepestChecked(this CategoryTree root)
        {
            if (root == null) yield break;
            var result = new List<CategoryTree>();
            CollectDeepestChecked(root, result);
            foreach (var n in result) yield return n;
        }

        /// <summary>
        /// 返回该子树是否含勾选；同时把“最下级的已选节点”加入 result。
        /// 规则：当前 IsCheck==true 且后代中无任何勾选 => 当前是“最下级已选”。
        /// </summary>
        private static bool CollectDeepestChecked(CategoryTree node, List<CategoryTree> result)
        {
            if (node == null) return false;

            bool descendantHasChecked = false;
            var children = node.Children;
            if (children != null && children.Count > 0)
            {
                foreach (var ch in children)
                    descendantHasChecked |= CollectDeepestChecked(ch, result);
            }

            bool thisChecked = node.IsCheck;
            if (thisChecked && !descendantHasChecked)
                result.Add(node);

            return thisChecked || descendantHasChecked;
        }
        /// <summary>
        /// 获取整片森林中的所有“最下级叶子”（Children 为 null 或 Count==0），不考虑 IsCheck。
        /// </summary>
        public static IEnumerable<CategoryTree> GetDeepestLeaves(this IEnumerable<CategoryTree> roots)
            => roots == null ? Enumerable.Empty<CategoryTree>() : roots.SelectMany(GetDeepestLeaves);

        /// <summary>
        /// 获取单棵树中的所有“最下级叶子”（Children 为 null 或 Count==0），不考虑 IsCheck。
        /// 非递归 DFS，避免深树的栈溢出。
        /// </summary>
        public static IEnumerable<CategoryTree> GetDeepestLeaves(this CategoryTree root)
        {
            if (root == null) yield break;

            var stack = new Stack<CategoryTree>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var children = node.Children;

                if (children == null || children.Count == 0)
                {
                    // 叶子
                    yield return node;
                }
                else
                {
                    // 压栈子节点（允许 null 容错）
                    for (int i = 0; i < children.Count; i++)
                    {
                        var ch = children[i];
                        if (ch != null) stack.Push(ch);
                    }
                }
            }
        }

        public static IEnumerable<CategoryTree> GetMaxDepthLeaves(this IEnumerable<CategoryTree> roots)
        {
            if (roots == null) return Enumerable.Empty<CategoryTree>();

            int maxDepth = -1;
            var result = new List<CategoryTree>();
            var stack = new Stack<(CategoryTree node, int depth)>();

            foreach (var r in roots)
                if (r != null) stack.Push((r, 0));

            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();
                var children = node.Children;
                bool isLeaf = children == null || children.Count == 0;

                if (isLeaf)
                {
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                        result.Clear();
                        result.Add(node);
                    }
                    else if (depth == maxDepth)
                    {
                        result.Add(node);
                    }
                }
                else
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        var ch = children[i];
                        if (ch != null) stack.Push((ch, depth + 1));
                    }
                }
            }

            return result;
        }

        public static void SetAllLeavesChecked(this IEnumerable<CategoryTree> roots, bool isChecked)
        {
            if (roots == null) return;
            foreach (var leaf in roots.GetDeepestLeaves()) // 你已实现的方法：返回所有叶子
            {
                if (leaf != null) leaf.IsCheck = isChecked;
            }
        }
    }
}
