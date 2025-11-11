using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Workbench.SerialAsistant.Utils
{
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            // 使用正则表达式匹配数字
            Regex regex = new Regex(@"\d+");

            // 提取x和y中的数字
            MatchCollection matchesX = regex.Matches(x);
            MatchCollection matchesY = regex.Matches(y);

            // 比较每对数字
            for (int i = 0; i < Math.Min(matchesX.Count, matchesY.Count); i++)
            {
                int numX = int.Parse(matchesX[i].Value);
                int numY = int.Parse(matchesY[i].Value);

                if (numX != numY)
                {
                    return numX.CompareTo(numY);
                }
            }

            // 如果数字完全一样，那么按照原字符串比较
            return x.CompareTo(y);
        }
    }

    public class ChineseNaturalSortComparerWithRegex : IComparer<string>
    {
        private static readonly Regex regex = new Regex(@"^([^\d]*)(\d+).*$", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var xMatch = regex.Match(x);
            var yMatch = regex.Match(y);

            string xPrefix = xMatch.Success ? xMatch.Groups[1].Value : x;
            string yPrefix = yMatch.Success ? yMatch.Groups[1].Value : y;

            // 先比较前缀
            int prefixCompare = string.Compare(xPrefix, yPrefix, StringComparison.Ordinal);
            if (prefixCompare != 0)
                return prefixCompare;

            // 如果前缀相同，比较数字
            if (xMatch.Success && yMatch.Success)
            {
                long xNum = long.Parse(xMatch.Groups[2].Value);
                long yNum = long.Parse(yMatch.Groups[2].Value);
                return xNum.CompareTo(yNum);
            }

            // 如果有一个没有数字，按字符串比较
            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }
}
