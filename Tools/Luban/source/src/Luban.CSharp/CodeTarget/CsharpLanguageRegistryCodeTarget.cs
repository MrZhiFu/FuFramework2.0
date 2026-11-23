using Luban.CodeTarget;
using System.Linq;
using Luban.CSharp.TemplateExtensions;
using Luban.Datas;
using Luban.Defs;
using Luban.Utils;
using Scriban;
using Scriban.Runtime;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 语言注册表代码生成目标（cs-language-registry）。
///     依据 TbLanguageDef 表生成语言注册表静态类（默认类名 LaunchLanguageRegistry）：
///     条目（语言/显示名/排序/分隔符/系统语言映射）、按名查找、SystemLanguage → 语言映射（switch 由数据生成）、
///     兜底语言常量（兜底语种固定 English，属业务不变量，不做成配置）。
///     用途：删除/新增语种只改配置数据（LanguageDef 行 + 本地化表列 + ELanguage 枚举成员），运行时零代码改动。
///     必须与 -f（forceLoadTableDatas）联用——本目标读取表数据；管线已在 P0 保证先 LoadDatas 再跑 code target。
///     生成期校验：ELanguage 成员 ↔ LanguageDef 行一一对应、English 兜底语种必须存在、语言名唯一。
/// </summary>
[CodeTarget("cs-language-registry")]
public class CsharpLanguageRegistryCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 语言定义表名
    /// </summary>
    private const string LanguageDefTableName = "TbLanguageDef";

    /// <summary>
    /// 默认类名（可用选项 cs-language-registry.className 覆盖）
    /// </summary>
    protected const string DefaultClassName = "LaunchLanguageRegistry";

    /// <summary>
    /// 必需字段清单：LanguageDef 删列/改名时生成期报错而非静默退化；加列零改动（新列自动进入 Fields）。
    /// language = 结构主键（Name/ELanguage/文件名/tag/偏好全链路）；name = 显示名（UI 直接可感）；
    /// system_language = 系统语言映射数据源（缺失会使 FromSystemLanguage 无 case，新用户静默全回落 English）。
    /// sort/separator 为普通数据：缺失无害退化（sort 失败按 0 走表行序），不锁入清单。
    /// </summary>
    private static readonly string[] RequiredFieldNames = { "language", "name", "system_language" };

    /// <summary>
    /// 字段名在生成产物中的展示名（Excel 列名 → Entry.Fields 键），未列出的字段按 Excel 原列名入包
    /// </summary>
    private static readonly Dictionary<string, string> FieldDisplayNames = new(StringComparer.Ordinal)
    {
        { "name", "display_name" },
        { "system_language", "system_languages" }, // 语义修正：该列是 SystemLanguage 名列表（分号分隔）
    };

    /// <summary>
    /// 主键列名（language）：转为 Entry.Name + ELanguage，不进 Fields 字段包
    /// </summary>
    private const string KeyFieldName = "language";

    /// <summary>
    /// 注册表条目（数据驱动字段包）：主键 Name + 其余全部字段的字符串键值对。
    /// LanguageDef 加列零改动（新列自动出现在 Fields）；取值用 GetField，拼错键名返回 null 不抛异常。
    /// bool/int 值以原字符串存储（"True"/"1"/"3"），由消费端解析。
    /// </summary>
    public class LanguageEntry
    {
        /// <summary>
        /// 语言枚举名（同时是拆分文件后缀与 YooAsset 标签 / 偏好存储名）
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 全部非主键字段（键 = 字段展示名，值 = Excel 单元格原值字符串；空单元格为空串）
        /// </summary>
        public Dictionary<string, string> Fields { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Fields 的有序键值对视图（scriban 模板遍历用；按 C# 端遍历顺序）
        /// </summary>
        public List<KeyValuePair<string, string>> FieldsList => Fields.ToList();

        /// <summary>
        /// Unity SystemLanguage 成员名列表（由 system_languages 字段解析，生成 FromSystemLanguage switch 的 case）
        /// </summary>
        public List<string> SystemLanguages { get; set; } = new();
    }

    /// <summary>
    /// 处理代码生成：读取 TbLanguageDef 全部行，校验后交给模板渲染
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var className = EnvManager.Current.GetOptionOrDefault(Name, "className", true, DefaultClassName);
        var entries = CollectEntries(ctx);
        Validate(entries);

        var writer = new CodeWriter();
        GenerateRegistry(ctx, writer, className, entries);
        manifest.AddFile(CreateOutputFile($"{className}.{FileSuffixName}", writer.ToResult(FileHeader)));
    }

    /// <summary>
    /// 从 TbLanguageDef 收集全部行（不依赖分组过滤：调用方用 -o 显式指定本表）
    /// </summary>
    /// <param name="ctx">生成上下文</param>
    /// <returns>条目列表（按表行序）</returns>
    private static List<LanguageEntry> CollectEntries(GenerationContext ctx)
    {
        var table = ctx.ExportTables.FirstOrDefault(t => t.Name == LanguageDefTableName)
            ?? throw new Exception($"cs-language-registry: 未找到表 '{LanguageDefTableName}'，请确认 -o 已包含该表且 tableImporter.target 允许导出");
        var dataInfo = GenerationContext.Current.GetTableDataInfo(table)
            ?? throw new Exception($"cs-language-registry: 表 '{LanguageDefTableName}' 无数据（缺 -f 或数据未加载）");

        var entries = new List<LanguageEntry>();
        foreach (var record in dataInfo.FinalRecords)
        {
            if (record.Data is not DBean bean) continue;

            // 必需字段存在性校验：LanguageDef 删列/改名时生成期报错，而非静默退化
            foreach (var required in RequiredFieldNames)
            {
                if (bean.GetField(required) == null)
                {
                    throw new Exception($"cs-language-registry: 表 '{LanguageDefTableName}' 缺少必需字段 '{required}'（LanguageDef 表结构变更须同步 cs-language-registry 生成器）");
                }
            }

            var language = bean.GetField(KeyFieldName) as DEnum
                ?? throw new Exception($"cs-language-registry: 表 '{LanguageDefTableName}' 缺少 language(ELanguage) 字段");
            if (language.Type.DefEnum.Name != "ELanguage")
            {
                throw new Exception($"cs-language-registry: language 字段类型须为 ELanguage，实际 {language.Type.DefEnum.Name}");
            }

            // 数据驱动字段包：按 DefBean 导出字段遍历（除主键），名值对入 Fields——加列零改动
            var entry = new LanguageEntry { Name = language.StrValue };
            foreach (var field in bean.ImplType.ExportFields)
            {
                if (field.Name == KeyFieldName) continue;

                var key = FieldDisplayNames.TryGetValue(field.Name, out var display) ? display : field.Name;
                entry.Fields[key] = GetCellValue(bean, field.Name);
            }

            entry.SystemLanguages = ParseSystemLanguages(entry.Fields.GetValueOrDefault("system_languages"));
            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// 取字段单元格原值字符串（string 直取；其余类型转字符串表示——空/null 统一空串）
    /// </summary>
    private static string GetCellValue(DBean bean, string fieldName)
    {
        var value = bean.GetField(fieldName);
        return value switch
        {
            DString s  => s.Value ?? "",
            DBool b    => b.Value ? "True" : "False",
            DInt i     => i.Value.ToString(),
            DLong l    => l.Value.ToString(),
            DFloat f   => f.Value.ToString(),
            DDouble d  => d.Value.ToString(),
            DEnum en   => en.StrValue,
            _          => value?.ToString() ?? "",
        };
    }

    /// <summary>
    /// 解析 system_language 多值列（分号分隔的 Unity SystemLanguage 成员名）
    /// </summary>
    /// <param name="raw">原始字符串，空/空白返回空列表</param>
    /// <returns>成员名列表</returns>
    private static List<string> ParseSystemLanguages(string raw)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return result;
        foreach (var part in raw.Split(';'))
        {
            var name = part.Trim();
            if (name.Length > 0) result.Add(name);
        }
        return result;
    }

    /// <summary>
    /// 生成期校验：枚举成员 ↔ 表行一一对应、fallback 唯一、legacy_id 唯一非零、名字唯一。
    /// 校验失败抛异常中断整个生成。
    /// </summary>
    /// <param name="entries">待校验条目</param>
    private static void Validate(List<LanguageEntry> entries)
    {
        if (entries.Count == 0)
        {
            throw new Exception("cs-language-registry: LanguageDef 表无数据行");
        }

        var seenNames = new HashSet<string>();
        foreach (var e in entries)
        {
            if (!seenNames.Add(e.Name))
            {
                throw new Exception($"cs-language-registry: 语言名重复 '{e.Name}'");
            }
        }

        // 兜底语种是业务不变量（英语兜底），不是配置项：English 语种必须存在
        if (entries.All(e => e.Name != "English"))
        {
            throw new Exception("cs-language-registry: 兜底语种 English 缺失（兜底体系要求 English 语种必须存在）");
        }

        // ELanguage 成员 ↔ 表行 一一对应（Unspecified 为保留零值，不要求有行）
        var enumType = GenerationContext.Current.ExportEnums.FirstOrDefault(e => e.Name == "ELanguage")
            ?? throw new Exception("cs-language-registry: 当前 target 未导出 ELanguage 枚举");
        foreach (var item in enumType.Items)
        {
            if (item.Name == "Unspecified") continue;
            if (entries.All(e => e.Name != item.Name))
            {
                throw new Exception($"cs-language-registry: ELanguage 成员 '{item.Name}' 在 LanguageDef 表中缺少对应行");
            }
        }
        foreach (var e in entries)
        {
            if (enumType.Items.All(i => i.Name != e.Name))
            {
                throw new Exception($"cs-language-registry: LanguageDef 行 '{e.Name}' 在 ELanguage 枚举中不存在对应成员");
            }
        }
    }

    /// <summary>
    /// 渲染注册表模板
    /// </summary>
    private void GenerateRegistry(GenerationContext ctx, CodeWriter writer, string className, List<LanguageEntry> entries)
    {
        var template = GetTemplate("registry");
        var tplCtx = CreateTemplateContext(template);

        // 排序键从字段包读取（sort 列），缺失按 0
        var sorted = entries
            .OrderBy(e => int.TryParse(e.Fields.GetValueOrDefault("sort"), out var sort) ? sort : 0)
            .ToList();

        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx },
            { "__namespace", ctx.Target.TopModule },
            { "__class_name", className },
            { "__entries", sorted },
            { "__fallback_index", sorted.FindIndex(e => e.Name == "English") },
            { "__code_style", CodeStyle },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }
}
