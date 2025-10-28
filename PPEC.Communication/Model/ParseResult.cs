using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPEC.Communication.Model
{

    public enum FormulaKind
    {
        YEqX,          // y = x
        YEqAX,         // y = a*x
        YEqABminusAX,  // y = a*(b ± x) —— 你关注的是 b - x，对应 sign1='-'
        YEqAXpmB,      // y = a*x ± b
        Unknown
    }
    public sealed class  ParseResult
    {
        public FormulaKind Kind { get; set; } = FormulaKind.Unknown;
        public double? A { get; set; }
        public double? B { get; set; }   // 括号内/常数项
        public char? Sign { get; set; }  // 若 Kind==YEqABminusAX，表示 (b ± x) 的 ±；
                                         // 若 Kind==YEqAXpmB，表示 ax 与 b 之间的 ±。
        public override string ToString()
            => $"Kind={Kind}, A={A}, B={B}, Sign={(Sign.HasValue ? Sign.ToString() : "null")}";
    }
}
