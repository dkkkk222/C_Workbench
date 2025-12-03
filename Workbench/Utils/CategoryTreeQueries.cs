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
        /* ========== D. 最下级已选节点 + 其所有“已选”的上级节点 ========== */

        /// <summary>
        /// 整片森林中：获取所有“最下级已选节点”及其所有【IsCheck==true】的祖先节点。
        /// （未勾选的上级节点不会返回）
        /// </summary>
        public static IEnumerable<CategoryTree> GetDeepestCheckedWithCheckedAncestors(this IEnumerable<CategoryTree> roots)
        {
            if (roots == null)
                return Enumerable.Empty<CategoryTree>();

            var result = new HashSet<CategoryTree>();   // 用来去重
            foreach (var r in roots)
            {
                if (r != null)
                    CollectDeepestCheckedWithCheckedAncestors(r, new List<CategoryTree>(), result);
            }

            return result;
        }

        /// <summary>
        /// 单棵树版本
        /// </summary>
        public static IEnumerable<CategoryTree> GetDeepestCheckedWithCheckedAncestors(this CategoryTree root)
        {
            if (root == null)
                return Enumerable.Empty<CategoryTree>();

            var result = new HashSet<CategoryTree>();
            CollectDeepestCheckedWithCheckedAncestors(root, new List<CategoryTree>(), result);
            return result;
        }

        /// <summary>
        /// DFS：收集“最下级已选节点” + 其所有【已勾选】的祖先到 result 中。
        /// 返回：该子树中是否存在勾选的节点（包括自己和后代）。
        /// 规则：
        ///   当前 IsCheck == true 且后代无勾选 => 当前是“最下级已选”；
        ///   然后沿 path 把 IsCheck==true 的节点加入 result。
        /// </summary>
        private static bool CollectDeepestCheckedWithCheckedAncestors(
            CategoryTree node,
            List<CategoryTree> path,
            HashSet<CategoryTree> result)
        {
            if (node == null)
                return false;

            // 进入当前节点：放进路径
            path.Add(node);

            bool descendantHasChecked = false;
            var children = node.Children;
            if (children != null && children.Count > 0)
            {
                foreach (var ch in children)
                {
                    if (CollectDeepestCheckedWithCheckedAncestors(ch, path, result))
                        descendantHasChecked = true;
                }
            }

            bool thisChecked = node.IsCheck;
            bool hasCheckedInSubtree = thisChecked || descendantHasChecked;

            // 当前节点是“最下级已选”：自己勾选，且后代里没人勾选
            if (thisChecked && !descendantHasChecked)
            {
                // 沿着整条路径，把【已勾选】的节点加入结果（祖先 + 当前）
                foreach (var n in path)
                {
                    if (n != null && n.IsCheck)   // ⭐ 这里过滤掉没选中的节点
                        result.Add(n);            // HashSet 去重
                }
            }

            // 退出当前节点：从路径中移除
            path.RemoveAt(path.Count - 1);

            return hasCheckedInSubtree;
        }
    }
}
