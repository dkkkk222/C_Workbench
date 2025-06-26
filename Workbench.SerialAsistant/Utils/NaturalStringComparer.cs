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
}
