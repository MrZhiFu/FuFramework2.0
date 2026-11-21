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
using Luban.Types;
using Scriban.Runtime;

namespace Luban.CSharp.TemplateExtensions;

public class CsharpSimpleJsonTemplateExtension : ScriptObject
{
    public static string Deserialize(string fieldName, string jsonVar, TType type)
    {
        if (type.IsNullable)
        {
            return $"{{var _j = {jsonVar}; if (_j.Tag != JSONNodeType.None && _j.Tag != JSONNodeType.NullValue) {{ {type.Apply(SimpleJsonDeserializeVisitor.Ins, "_j", fieldName, 0)} }} else {{ {fieldName} = null; }} }}";
        }
        else
        {
            return type.Apply(SimpleJsonDeserializeVisitor.Ins, jsonVar, fieldName, 0);
        }
    }

    public static string DeserializeField(string fieldName, string jsonVar, string jsonFieldName, TType type)
    {
        if (type.IsNullable)
        {
            return $"{{ var _j = {jsonVar}[\"{jsonFieldName}\"]; if (_j.Tag != JSONNodeType.None && _j.Tag != JSONNodeType.NullValue) {{ {type.Apply(SimpleJsonDeserializeVisitor.Ins, "_j", fieldName, 0)} }} else {{ {fieldName} = null; }} }}";
        }
        else
        {
            return type.Apply(SimpleJsonDeserializeVisitor.Ins, $"{jsonVar}[\"{jsonFieldName}\"]", fieldName, 0);
        }
    }

    /// <summary>
    /// 本地化拆分产物专用宽容反序列化：缺键（或 null 值）时字段置 null，不抛 SerializationException。
    /// 仅限 Localization bean 的语种字段（模板分支控制）；key 字段不经过此函数（保持严格）。
    /// 依赖产物形态：按语种拆分后每行只有 key + 目标语种两键，缺失语种键 = 该语种在当前语言下无译文（null），
    /// 空值兜底由运行时英语兜底表二段查询接管。
    /// </summary>
    public static string DeserializeLenient(string fieldName, string jsonVar, string jsonFieldName, TType type)
    {
        // type 恒为 TString（模板分支保证）；缺键时 SimpleJSON indexer 返回 JSONNode.Null 实例，IsString=false → null
        return $"{{ var _j = {jsonVar}[\"{jsonFieldName}\"]; {fieldName} = _j.IsString ? _j.Value : null; }}";
    }
}
