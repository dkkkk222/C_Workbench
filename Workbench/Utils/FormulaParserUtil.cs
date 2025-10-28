using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PPEC.Communication.Model;

namespace Workbench.Utils
{
    public static class FormulaParser
    {
        // 数字（整数/小数/科学计数法）
        private const string NUM = @"[+-]?(?:\d+(?:\.\d+)?|\.\d+)(?:[eE][+-]?\d+)?";

        private static readonly Regex RxYEqX =
            new(@"^\s*y\s*=\s*x\s*$", RegexOptions.IgnoreCase);

        private static readonly Regex RxYEqAX =
            new($@"^\s*y\s*=\s*(?<a>{NUM})\s*\*?\s*x\s*$", RegexOptions.IgnoreCase);

        private static readonly Regex RxYEqABmAX =
            new($@"^\s*y\s*=\s*(?<a>{NUM})\s*\(\s*(?<b>{NUM})\s*(?<sign1>[+-])\s*x\s*\)\s*$",
                RegexOptions.IgnoreCase);

        private static readonly Regex RxYEqAXpmB =
            new($@"^\s*y\s*=\s*(?<a>{NUM})\s*\*?\s*x\s*(?<sign2>[+-])\s*(?<b>{NUM})\s*$",
                RegexOptions.IgnoreCase);

        public static ParseResult Parse(string input)
        {
            // 兼容中文标点结尾，例如 "y=0.0006x-1.2000。"
            var s = input.Trim().TrimEnd('。', '.', ';', '；');

            var r = new ParseResult();

            if (RxYEqX.IsMatch(s))
            {
                r.Kind = FormulaKind.YEqX;
                return r;
            }

            var m = RxYEqAX.Match(s);
            if (m.Success)
            {
                r.Kind = FormulaKind.YEqAX;
                r.A = double.Parse(m.Groups["a"].Value);
                return r;
            }

            m = RxYEqABmAX.Match(s);
            if (m.Success)
            {
                r.Kind = FormulaKind.YEqABminusAX;
                r.A = double.Parse(m.Groups["a"].Value);
                r.B = double.Parse(m.Groups["b"].Value);
                r.Sign = m.Groups["sign1"].Value[0]; // 括号里 b 与 x 之间的 +/-
                return r;
            }

            m = RxYEqAXpmB.Match(s);
            if (m.Success)
            {
                r.Kind = FormulaKind.YEqAXpmB;
                r.A = double.Parse(m.Groups["a"].Value);
                r.B = double.Parse(m.Groups["b"].Value);
                r.Sign = m.Groups["sign2"].Value[0]; // ax 与 b 之间的 +/-
                return r;
            }

            return r; // Unknown
        }
    }

    public class FormulaParser2
    {
        // 主正则表达式，匹配各种公式模式
        private static readonly Regex FormulaRegex = new Regex(
            @"^y\s*=\s*" +                       // y= 开头
            @"(" +                               // 开始分组1：系数A
                @"(?<a>-?\d*\.?\d+)" +          // 系数A（可带负号和小数）
            @")?" +                              // 系数A可选（y=x时没有系数）
            @"\s*" +                             // 可选空格
            @"(?<x>x)" +                         // x变量
            @"\s*" +                             // 可选空格
            @"(" +                               // 开始分组2：运算符和常数项
                @"(?<op>[+-])" +                 // 运算符 + 或 -
                @"\s*" +                         // 可选空格
                @"(?<b>\d*\.?\d+)" +            // 常数项B
            @")?" +                              // 运算符和常数项可选
            @"$",
            RegexOptions.IgnoreCase);

        // 匹配 y=A(B-x) 模式的特殊正则
        private static readonly Regex ParenthesisRegex = new Regex(
            @"^y\s*=\s*" +                       // y= 开头
            @"(?<a>\d*\.?\d+)" +                 // 系数A
            @"\s*\(\s*" +                        // 左括号
            @"(?<b>\d*\.?\d+)" +                 // 系数B
            @"\s*-\s*x\s*\)" +                   // -x 和右括号
            @"$",
            RegexOptions.IgnoreCase);

        public class FormulaResult
        {
            public string FormulaType { get; set; }      // 公式类型
            public double? CoefficientA { get; set; }    // 系数A
            public double? CoefficientB { get; set; }    // 系数B（如果有）
            public string Operator { get; set; }         // 运算符 + 或 -
            public double? Constant { get; set; }        // 常数项
        }

        public static FormulaResult ParseFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return null;

            // 清理输入字符串
            string cleanFormula = formula.Replace(" ", "").ToLower();

            // 先尝试匹配 y=A(B-x) 模式
            var parenthesisMatch = ParenthesisRegex.Match(cleanFormula);
            if (parenthesisMatch.Success)
            {
                return new FormulaResult
                {
                    FormulaType = "Y=A*(B-X)",
                    CoefficientA = double.Parse(parenthesisMatch.Groups["a"].Value),
                    CoefficientB = double.Parse(parenthesisMatch.Groups["b"].Value),
                    Operator = "-", // 这种模式固定为减号
                    Constant = null
                };
            }

            // 匹配其他模式
            var match = FormulaRegex.Match(cleanFormula);
            if (match.Success)
            {
                var result = new FormulaResult();

                // 处理系数A
                if (match.Groups["a"].Success && !string.IsNullOrEmpty(match.Groups["a"].Value))
                {
                    result.CoefficientA = double.Parse(match.Groups["a"].Value);
                }
                else
                {
                    result.CoefficientA = 1.0; // y=x 等价于 y=1x
                }

                // 处理运算符和常数项
                if (match.Groups["op"].Success && match.Groups["b"].Success)
                {
                    result.Operator = match.Groups["op"].Value;
                    result.Constant = double.Parse(match.Groups["b"].Value);
                    result.FormulaType = $"Y=A*X{result.Operator}B";
                }
                else
                {
                    // 没有运算符和常数项
                    result.Operator = null;
                    result.Constant = null;
                    if (result.CoefficientA == 1.0)
                        result.FormulaType = "Y=X";
                    else
                        result.FormulaType = "Y=A*X";
                }

                return result;
            }

            return null; // 无法解析的公式
        }

        // 批量解析方法
        public static List<FormulaResult> ParseFormulas(List<string> formulas)
        {
            var results = new List<FormulaResult>();
            foreach (var formula in formulas)
            {
                var result = ParseFormula(formula);
                if (result != null)
                    results.Add(result);
            }
            return results;
        }
    }
}
