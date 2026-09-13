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
/// 生成包含所有多语言 Key 的静态类 : LanguageKey.cs
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
    /// 默认输出文件名
    /// </summary>
    protected const string DefaultOutputFileName = "LanguageKey";

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
    /// 重写以生成LanguageKey.cs静态类
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var outputFileName = EnvManager.Current.GetOptionOrDefault(Name, "outputFile", true, DefaultOutputFileName);

        // 生成LanguageKey.cs
        var writer = new CodeWriter();
        GenerateL10NKeys(ctx, writer);
        manifest.AddFile(CreateOutputFile($"{outputFileName}.{FileSuffixName}", writer.ToResult(FileHeader)));
    }

    /// <summary>
    /// 生成多语言LanguageKey.cs静态类
    /// </summary>
    /// <param name="ctx">生成时的上下文</param>
    /// <param name="writer">代码写入器</param>
    private void GenerateL10NKeys(GenerationContext ctx, CodeWriter writer)
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
            { "__class_name", "LanguageKey" },
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

        // 筛选出多语言表
        var l10NTables = tables.Where(t => t.Name == "TbLocalization").ToList();

        foreach (var table in l10NTables)
        {
            CollectKeysFromTable(table, keys, keySet);
        }

        s_logger.Info("多语言key收集: 总共收集到 {Count} 个多语言Key", keys.Count);

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

        // 获取表数据
        var tableDataInfo = GenerationContext.Current.GetTableDataInfo(table);
        if (tableDataInfo == null) return;

        // 遍历表记录，收集 Key 信息
        foreach (var record in tableDataInfo.FinalRecords)
        {
            ProcessRecord(record, keyField, commentField, resultKeys, keySet);
        }
    }

    /// <summary>
    /// 处理单条数据记录
    /// </summary>
    /// <param name="record">数据记录</param>
    /// <param name="keyField">key字段</param>
    /// <param name="commentField">注释字段(即简体中文字段)</param>
    /// <param name="resultKeys">多语言表key结果列表</param>
    /// <param name="keySet">用于去重的集合</param>
    private void ProcessRecord(Record record, DefField keyField, DefField commentField, List<L10NKeyInfo> resultKeys, HashSet<string> keySet)
    {
        if (record.Data is not DBean bean) return;

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
