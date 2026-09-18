using Luban.CodeTarget;
using Luban.Utils;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 仅生成枚举类型的代码目标。
///     用于只产出枚举（不产出 bean/table/manager）的场景，如 AOT 侧语言枚举。
///     枚举集 = 当前 target 分组独立导出的枚举（如 group=aot 的 ELanguage）。
/// </summary>
[CodeTarget("cs-enums")]
public class CsharpEnumsCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 处理代码生成：仅遍历导出枚举，逐枚举渲染 enum 模板。
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        foreach (var @enum in ctx.ExportEnums)
        {
            var writer = new CodeWriter();
            GenerateEnum(ctx, @enum, writer);
            manifest.AddFile(CreateOutputFile($"{GetFileNameWithoutExtByTypeName(@enum.FullName)}.{FileSuffixName}", writer.ToResult(FileHeader)));
        }
    }
}
