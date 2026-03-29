using System.Text.RegularExpressions;

namespace Luban.CSharp.TemplateExtensions;

/// <summary>
/// 自定义多语言Key模板扩展
/// 提供从多语言表中收集Key信息的方法
/// </summary>
public class CsharpL10NKeyTemplateExtension : CsharpTemplateExtension
{
    /// <summary>
    /// 格式化Key为合法的 C# 标识符(l10n-keys.sbn模板中使用"format_key_name")
    /// </summary>
    public static string FormatKeyName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "_Empty";
        }

        // 替换非法字符为下划线
        var result = Regex.Replace(key, @"[^a-zA-Z0-9_]", "_");

        // 确保以字母或下划线开头
        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        // 处理 C# 关键字
        string[] keywords =
        [
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
            "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        ];

        if (keywords.Contains(result))
        {
            result = "@" + result;
        }

        return result;
    }
}
