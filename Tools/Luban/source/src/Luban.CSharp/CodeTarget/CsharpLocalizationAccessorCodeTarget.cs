using Luban.CodeTarget;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;
using Scriban;
using Scriban.Runtime;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 本地化取词访问器代码生成目标（cs-l10n-accessor）。
///     依据本地化表（TbLocalization）的语言列 + LanguageDef 生成静态取词访问器（默认类名 LocalizationTextAccessor）：
///     GetText(Localization row, ELanguage language) 的 switch 由语言列生成，删/增语种（Excel 删列）时 case 自动增减，
///     default 回落兜底语种列——运行时取词不再手写语言分支。
///     校验：本地化表语言列集合 ↔ LanguageDef 行集合一致、兜底列存在。
///     仅在 client 段使用（需要 TbLocalization 与 TbLanguageDef 同时可见）。
/// </summary>
[CodeTarget("cs-l10n-accessor")]
public class CsharpLocalizationAccessorCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 热更本地化表名
    /// </summary>
    private const string LocalizationTableName = "TbLocalization";

    /// <summary>
    /// 语言定义表名
    /// </summary>
    private const string LanguageDefTableName = "TbLanguageDef";

    /// <summary>
    /// 默认类名（可用选项 cs-l10n-accessor.className 覆盖）
    /// </summary>
    protected const string DefaultClassName = "LocalizationTextAccessor";

    /// <summary>
    /// 处理代码生成：收集语言列 + 兜底列，校验后交给模板渲染
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var className = EnvManager.Current.GetOptionOrDefault(Name, "className", true, DefaultClassName);

        var localizationTable = ctx.ExportTables.FirstOrDefault(t => t.Name == LocalizationTableName)
            ?? throw new Exception($"cs-l10n-accessor: 未找到表 '{LocalizationTableName}'（client target 必须导出本地化表）");
        var languageDefTable = ctx.ExportTables.FirstOrDefault(t => t.Name == LanguageDefTableName)
            ?? throw new Exception($"cs-l10n-accessor: 未找到表 '{LanguageDefTableName}'");

        // 语言列 = 本地化表除 key/is_code 外的字符串字段（与拆分后处理的语种识别口径一致）
        var bean = localizationTable.ValueTType.DefBean;
        var languageFields = bean.ExportFields
            .Where(f => f.CType is TString && f.Name != "key" && f.Name != "is_code")
            .ToList();
        if (languageFields.Count == 0)
        {
            throw new Exception($"cs-l10n-accessor: 表 '{LocalizationTableName}' 无语言列");
        }

        // 兜底语种是业务不变量（英语兜底）：兜底列固定为 English 语言列
        const string FallbackLanguageName = "English";
        var fallbackField = languageFields.FirstOrDefault(f => f.Name == FallbackLanguageName)
            ?? throw new Exception($"cs-l10n-accessor: 兜底语种 '{FallbackLanguageName}' 在本地化表中没有对应语言列（英语兜底体系要求该列必须存在）");

        var defDataInfo = GenerationContext.Current.GetTableDataInfo(languageDefTable)
            ?? throw new Exception($"cs-l10n-accessor: 表 '{LanguageDefTableName}' 无数据（缺 -f）");

        // 校验语言列集合 == LanguageDef 行集合
        var defNames = new HashSet<string>();
        foreach (var record in defDataInfo.FinalRecords)
        {
            if (record.Data is DBean row && (row.GetField("language") as DEnum) is { } lang)
            {
                defNames.Add(lang.StrValue);
            }
        }
        var columnNames = new HashSet<string>(languageFields.Select(f => f.Name));
        foreach (var name in defNames)
        {
            if (!columnNames.Contains(name))
            {
                throw new Exception($"cs-l10n-accessor: LanguageDef 语言 '{name}' 在本地化表中缺少语言列");
            }
        }
        foreach (var field in languageFields)
        {
            if (!defNames.Contains(field.Name))
            {
                throw new Exception($"cs-l10n-accessor: 本地化表语言列 '{field.Name}' 在 LanguageDef 中缺少对应行");
            }
        }

        var writer = new CodeWriter();
        GenerateAccessor(ctx, writer, className, languageFields, fallbackField);
        manifest.AddFile(CreateOutputFile($"{className}.{FileSuffixName}", writer.ToResult(FileHeader)));
    }

    /// <summary>
    /// 渲染访问器模板
    /// </summary>
    private void GenerateAccessor(GenerationContext ctx, CodeWriter writer, string className, List<DefField> languageFields, DefField fallbackField)
    {
        var template = GetTemplate("accessor");
        var tplCtx = CreateTemplateContext(template);

        var fields = languageFields.Select(f => new
        {
            Name = f.Name,
            Property = FormatPropertyName(f),
        }).ToList();

        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx },
            { "__namespace", ctx.Target.TopModule },
            { "__class_name", className },
            { "__row_type", "Localization" },
            { "__fields", fields },
            { "__fallback_property", FormatPropertyName(fallbackField) },
            { "__fallback_name", fallbackField.Name },
            { "__code_style", CodeStyle },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }

    /// <summary>
    /// 字段名转 C# 属性名（与生成 bean 的 format_property_name 口径一致）
    /// </summary>
    private string FormatPropertyName(DefField field)
    {
        return Luban.TemplateExtensions.TypeTemplateExtension.FormatPropertyName(CodeStyle, field.Name);
    }
}
