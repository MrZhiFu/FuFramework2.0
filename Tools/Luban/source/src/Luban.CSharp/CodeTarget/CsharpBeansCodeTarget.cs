using Luban.CodeTarget;
using Luban.Utils;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 仅生成 bean 数据类的代码目标。
///     用于只产出数据 bean（不产出 table/manager）的场景，如 AOT 侧本地化表 bean。
///     bean 集 = 当前 target 分组导出的 bean，可用 cs-bean.tables 选项（逗号分隔表名）过滤，
///     仅生成指定表的值 bean（如 cs-bean.tables=TbLocalizationAOT）。
/// </summary>
[CodeTarget("cs-bean")]
public class CsharpBeansCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 处理代码生成：遍历导出 bean（按 tables 选项过滤），逐 bean 渲染 bean 模板。
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var tableFilter = EnvManager.Current.GetOptionOrDefault(Name, "tables", true, "");
        var allowedTables = tableFilter.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        foreach (var bean in ctx.ExportBeans)
        {
            if (allowedTables.Count > 0 &&
                !ctx.ExportTables.Any(t => t.ValueTType.DefBean == bean && allowedTables.Contains(t.Name)))
            {
                continue;
            }

            var writer = new CodeWriter();
            GenerateBean(ctx, bean, writer);
            manifest.AddFile(CreateOutputFile($"{bean.Name}.{FileSuffixName}", writer.ToResult(FileHeader)));
        }
    }
}
