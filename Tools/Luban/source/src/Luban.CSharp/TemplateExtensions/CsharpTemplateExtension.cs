// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Luban.CSharp.TypeVisitors;
using Luban.Defs;
using Luban.Types;
using Scriban.Runtime;

namespace Luban.CSharp.TemplateExtensions;

public class CsharpTemplateExtension : ScriptObject
{
    public static string DeclaringTypeName(TType type)
    {
        return type.Apply(DeclaringTypeNameVisitor.Ins);
    }

    public static string DeclaringCollectionRefName(TType type)
    {
        return type.Apply(DeclaringCollectionRefNameVisitor.Ins);
    }

    public static string ClassOrStruct(DefBean bean)
    {
        return bean.IsValueType ? "struct" : "class";
    }

    public static string ClassModifier(DefBean bean)
    {
        return bean.IsAbstractType ? "abstract" : "sealed";
    }

    public static string MethodModifier(DefBean bean)
    {
        return bean.ParentDefType != null ? "override" : (bean.IsAbstractType ? "virtual" : "");
    }

    /// <summary>
    /// 字符串字面量转义（嵌入生成的 C# 字符串字面量：反斜杠/引号/控制字符），模板函数名 escape_string
    /// </summary>
    public static string EscapeString(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append($"\\u{(int)c:x4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    public static string NamespaceWithGraceBegin(string ns)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return string.Empty;
        }
        return $"namespace {ns}\n{{";
    }

    public static string NamespaceWithGraceEnd(string ns)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return string.Empty;
        }
        return "}";
    }

    public static string ToPrettyString(string name, TType type)
    {
        return type.Apply(DataToStringVisitor.Ins, name);
    }

    /// <summary>
    /// 服务端模板（cs-dotnet-json / cs-dotnet-bin）专用：值转字符串。
    /// 服务端工程不引用 Luban 运行时，集合转字符串使用项目自带的
    /// FuFramework.Core.Config.StringUtil（生成文件头已 using 该命名空间），
    /// 因此这里把 DataToStringVisitor 输出的 Luban.StringUtil 限定名改为短名。
    /// </summary>
    public static string ToPrettyStringServer(string name, TType type)
    {
        return type.Apply(DataToStringVisitor.Ins, name).Replace("Luban.StringUtil.", "StringUtil.");
    }

    public static string PadRight(string s, int totalWidth)
    {
        return s.PadRight(totalWidth);
    }

    public static string LowerCaseFirst(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
    }

    public static string GetValueOfNullableType(TType type, string varName)
    {
        return type.Apply(IsRawNullableTypeVisitor.Ins) ? varName : $"{varName}.Value";
    }

    // public static string RefTypeName(DefField field)
    // {
    //     if (field.CType.GetTag("ref") is { } value && GenerationContext.Current.Assembly.GetCfgTable(value) is { } cfgTable)
    //     {
    //         return cfgTable.ValueTType.Apply(DeclaringTypeNameVisitor.Ins);
    //     }
    //     return string.Empty;
    // }
}
