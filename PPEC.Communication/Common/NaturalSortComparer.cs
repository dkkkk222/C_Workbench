using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PPEC.Communication.Model;

namespace PPEC.Communication.Common
{
    public class NaturalSortComparer : IComparer<string>
    {
        private readonly Regex _regex = new Regex(@"(\d+)|(\D+)", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var partsX = _regex.Matches(x);
            var partsY = _regex.Matches(y);

            int minLength = Math.Min(partsX.Count, partsY.Count);

            for (int i = 0; i < minLength; i++)
            {
                string partX = partsX[i].Value;
                string partY = partsY[i].Value;

                if (partX != partY)
                {
                    // 如果两部分都是数字，按数值比较
                    if (long.TryParse(partX, out long numX) && long.TryParse(partY, out long numY))
                    {
                        return numX.CompareTo(numY);
                    }
                    // 否则按字符串比较
                    return string.Compare(partX, partY, StringComparison.Ordinal);
                }
            }

            return partsX.Count.CompareTo(partsY.Count);
        }
    }

    public static class TelemetryCodeExtensions
    {
        public static IEnumerable<TelemetryCode> OrderByNatural(
            this IEnumerable<TelemetryCode> source,
            Func<TelemetryCode, string> keySelector)
        {
            return source.OrderBy(keySelector, new NaturalSortComparer());
        }

        public static IEnumerable<TelemetryCode> OrderByDescendingNatural(
            this IEnumerable<TelemetryCode> source,
            Func<TelemetryCode, string> keySelector)
        {
            return source.OrderByDescending(keySelector, new NaturalSortComparer());
        }
    }
}
