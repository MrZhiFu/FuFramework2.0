using Luban.CodeTarget;
using Luban.CSharp.TemplateExtensions;
using Luban.Utils;
using Luban.Defs;
using Luban.Datas;
using Scriban;
using Scriban.Runtime;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 自定义多语言 Key 代码生成目标
/// 生成包含所有多语言 Key 的静态类 : L10nKey.cs
/// </summary>
[CodeTarget("cs-l10n-key")]
public class CsharpL10NKeyCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 多语言 Key 信息
    /// </summary>
    public class L10NKeyInfo
    {
        /// <summary>
        /// 多语言 Key
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// 多语言 Key 注释
        /// </summary>  
        public string Comment { get; set; } = "";
    }

    /// <summary>
    /// 默认类名与输出文件名（可用选项 cs-l10n-key.className 覆盖）
    /// </summary>
    protected const string DefaultClassName = "L10nKey";

    /// <summary>
    /// 日志记录器
    /// </summary>
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 创建模板上下文时的回调
    /// </summary>
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        base.OnCreateTemplateContext(ctx);
        ctx.PushGlobal(new CsharpL10NKeyTemplateExtension());
    }

    /// <summary>
    /// 处理代码生成
    /// 重写以生成L10nKey.cs静态类
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var className = EnvManager.Current.GetOptionOrDefault(Name, "className", true, DefaultClassName);

        // 生成{className}.cs
        var writer = new CodeWriter();
        GenerateL10NKeys(ctx, writer, className);
        manifest.AddFile(CreateOutputFile($"{className}.{FileSuffixName}", writer.ToResult(FileHeader)));
    }

    /// <summary>
    /// 生成多语言静态类（类名与文件名由 className 选项决定）
    /// </summary>
    /// <param name="ctx">生成时的上下文</param>
    /// <param name="writer">代码写入器</param>
    /// <param name="className">静态类名</param>
    private void GenerateL10NKeys(GenerationContext ctx, CodeWriter writer, string className)
    {
        var template = GetTemplate("l10n-keys");
        var tplCtx   = CreateTemplateContext(template);

        // 从多语言表中收集Key信息
        var keys = CollectKeys(ctx.ExportTables);

        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx },
            { "__name", ctx.Target.Manager },
            { "__namespace", ctx.Target.TopModule },
            { "__full_name", TypeUtil.MakeFullName(ctx.Target.TopModule, ctx.Target.Manager) },
            { "__class_name", className },
            { "__keys", keys },
            { "__code_style", CodeStyle },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }

    /// <summary>
    /// 从多语言表中收集Key信息
    /// </summary>
    /// <param name="tables">所有被导出的表</param>
    /// <returns>多语言表key结果列表</returns>
    private List<L10NKeyInfo> CollectKeys(List<DefTable> tables)
    {
        var keys   = new List<L10NKeyInfo>();
        var keySet = new HashSet<string>();

        // 筛选出多语言表（含 AOT 前置本地化表）
        var l10NTables = tables.Where(t => t.Name is "TbLocalization" or "TbLocalizationAOT").ToList();

        foreach (var table in l10NTables)
        {
            CollectKeysFromTable(table, keys, keySet);
        }

        s_logger.Info("收集导出到代码中的多语言key: 总共收集到 {Count} 个多语言Key", keys.Count);

        // 按 Key 按照字母进行 排序
        return keys.OrderBy(k => k.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 从单个表中收集Key
    /// </summary>
    /// <param name="table">多语言表</param>
    /// <param name="resultKeys">多语言表key结果列表</param>
    /// <param name="keySet">用于去重的集合</param>
    private void CollectKeysFromTable(DefTable table, List<L10NKeyInfo> resultKeys, HashSet<string> keySet)
    {
        // 获取 key 字段（通常是第一个字段或名为 key 的字段）
        var keyField = table.ValueTType.DefBean.ExportFields.FirstOrDefault(f => f.Name == "key") ?? table.ValueTType.DefBean.ExportFields.FirstOrDefault();
        if (keyField == null) return;

        // 获取 ChineseSimplified(简体中文) 字段作为注释
        var commentField = table.ValueTType.DefBean.ExportFields.FirstOrDefault(f => f.Name is "ChineseSimplified" or "chineseSimplified" or "chinese_simplified");

        // 获取 is_code 字段（标记该 key 是否导出到代码），所有本地化表必须声明该列
        var isCodeField = table.ValueTType.DefBean.ExportFields.FirstOrDefault(f => f.Name == "is_code");
        if (isCodeField == null)
        {
            throw new Exception($"本地化表:'{table.Name}' 缺少 'is_code' 列！所有本地化表必须声明 is_code(bool) 列，用于标记该 key 是否导出到 L10nKey 类");
        }

        // 获取表数据
        var tableDataInfo = GenerationContext.Current.GetTableDataInfo(table);
        if (tableDataInfo == null) return;

        // 遍历表记录，收集 Key 信息
        foreach (var record in tableDataInfo.FinalRecords)
        {
            ProcessRecord(record, keyField, commentField, isCodeField, resultKeys, keySet);
        }
    }

    /// <summary>
    /// 处理单条数据记录
    /// </summary>
    /// <param name="record">数据记录</param>
    /// <param name="keyField">key字段</param>
    /// <param name="commentField">注释字段(即简体中文字段)</param>
    /// <param name="isCodeField">is_code字段(标记该key是否导出到代码)</param>
    /// <param name="resultKeys">多语言表key结果列表</param>
    /// <param name="keySet">用于去重的集合</param>
    private void ProcessRecord(Record record, DefField keyField, DefField commentField, DefField isCodeField, List<L10NKeyInfo> resultKeys, HashSet<string> keySet)
    {
        if (record.Data is not DBean bean) return;

        // 过滤未标记导出到代码的 key
        if (bean.GetField(isCodeField.Name) is not DBool { Value: true }) return;

        var keyValue = bean.GetField(keyField.Name);
        if (keyValue is not DString keyStr || string.IsNullOrWhiteSpace(keyStr.Value)) return;

        // 去重
        var key = keyStr.Value.Trim();
        if (!keySet.Add(key)) return;

        // 提取注释，并添加到结果key列表中
        var comment = ExtractComment(bean, commentField);
        resultKeys.Add(new L10NKeyInfo { Key = key, Comment = comment });
    }

    /// <summary>
    /// 提取注释
    /// </summary>
    /// <param name="bean">目标类</param>
    /// <param name="commentField">目标类的注释字段</param>
    /// <returns>注释</returns>
    private string ExtractComment(DBean bean, DefField commentField)
    {
        if (commentField == null) return "";

        var commentValue = bean.GetField(commentField.Name);
        if (commentValue is DString commentStr)
        {
            return commentStr.Value?.Trim() ?? "";
        }

        return "";
    }
}
