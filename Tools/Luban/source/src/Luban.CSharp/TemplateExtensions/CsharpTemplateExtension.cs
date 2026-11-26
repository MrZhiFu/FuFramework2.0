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
using Luban.TemplateExtensions;
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

    // ========== 条件生成判定：TranslateText / ResolveRef 仅在有实际工作时生成 ==========
    //
    // 目的：没有 text 字段 / 跨表引用的 bean 不生成空方法，表级与 TableManager 的调用行同步剔除，
    // 消除切语言、加载解析时的全链空转（如 TbLocalization 每次切语言白跑数千行空方法）。
    //
    // 一致性铁律：bean.sbn / table.sbn / tables.sbn 三层条件必须使用同一判定函数，
    // 否则上层调用不存在的下层方法将直接编译失败。

    /// <summary>
    /// bean 是否需要生成 TranslateText：本 bean 或任一后代 bean 存在 text 字段。
    /// 后代 override 调 base，故父链需一并生成；表级经根 bean 虚分派，根覆盖整棵子树。
    /// 嵌套 bean 字段不经 TranslateText 递归（沿用既有模板语义），故无需沿字段边传播。
    /// </summary>
    public static bool BeanNeedsTranslateText(DefBean bean)
    {
        foreach (var f in bean.ExportFields)
        {
            if (f.Type == "text")
            {
                return true;
            }
        }
        if (bean.Children == null)
        {
            return false;
        }
        foreach (var child in bean.Children)
        {
            if (BeanNeedsTranslateText(child))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// bean 是否需要生成 ResolveRef（最小不动点判定）：
    /// 自身有产生 _Ref 赋值的引用字段，或嵌套 bean 字段的目标需要生成（调用行存在），
    /// 或任一后代需要生成（override 调 base 要求父链提供 virtual）。
    /// DFS 环路按 false 处理：无种子的引用环不产生任何方法，符合最小不动点语义。
    /// </summary>
    public static bool BeanNeedsResolveRef(DefBean bean)
    {
        var visited = new HashSet<DefBean>();
        return BeanNeedsResolveRefCore(bean, visited);
    }

    private static bool BeanNeedsResolveRefCore(DefBean bean, HashSet<DefBean> visited)
    {
        if (!visited.Add(bean))
        {
            return false;
        }
        try
        {
            foreach (var f in bean.ExportFields)
            {
                if (TypeTemplateExtension.CanGenerateRef(f) || TypeTemplateExtension.CanGenerateCollectionRef(f))
                {
                    return true;
                }
            }
            if (bean.Children != null)
            {
                foreach (var child in bean.Children)
                {
                    if (BeanNeedsResolveRefCore(child, visited))
                    {
                        return true;
                    }
                }
            }
            foreach (var f in bean.ExportFields)
            {
                var target = GetNestedResolveTargetBean(f);
                if (target != null && BeanNeedsResolveRefCore(target, visited))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            visited.Remove(bean);
        }
    }

    /// <summary>
    /// 字段是否生成对嵌套 bean 的 ResolveRef 调用行：字段为需要生成 ResolveRef 的
    /// 非值类型 bean（直接/数组元素/Map 值三种形态），且目标 bean 经判定需要生成方法。
    /// 与模板既有三分支的形态判定保持一致，叠加目标 needs 条件。
    /// </summary>
    public static bool FieldGeneratesResolveRefCall(DefField field)
    {
        var target = GetNestedResolveTargetBean(field);
        return target != null && BeanNeedsResolveRef(target);
    }

    /// <summary>
    /// ref 字段目标类型的 C# 声明名（模板函数 ref_target_type_name）。
    /// Luban 逻辑 FullName（如 Tables.SoundGroup）与本项目生成 bean 的 C# 命名空间
    /// （Hotfix.Game.Config，由自定义导入器改写）不一致，全名无法解析导致 CS0234；
    /// 本项目全部 bean 生成在同一命名空间，用短类名即可编译期解析。
    /// 注意：不能与 TType 参数重载共存——scriban 对重载解析歧义（"Unable to convert
    /// DefField to TType"），模板函数只保留 DefField 单一签名。
    /// </summary>
    public static string RefTargetTypeName(DefField field)
    {
        var refTable = TypeTemplateExtension.GetRefTable(field);
        return refTable?.ValueTType.DefBean?.Name ?? string.Empty;
    }

    private static DefBean GetNestedResolveTargetBean(DefField field)
    {
        if (field.CType is TBean b && TypeTemplateExtension.IsBeanNeedResolveRef(b))
        {
            return b.DefBean;
        }
        if (TypeTemplateExtension.IsFieldArrayLikeNeedResolveRef(field))
        {
            return ((TBean)field.CType.ElementType).DefBean;
        }
        if (TypeTemplateExtension.IsFieldMapNeedResolveRef(field))
        {
            return ((TBean)((TMap)field.CType).ValueType).DefBean;
        }
        return null;
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
    /// 服务端模板（cs-dotnet-bin）专用：值转字符串。
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
