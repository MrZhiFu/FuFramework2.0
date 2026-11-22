using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Luban.Datas;
using Luban.Defs;
using Luban.PostProcess;
using Luban.Serialization;
using Luban.Types;
using Luban.Utils;

namespace Luban.CSharp.PostProcess;

/// <summary>
/// AOT 前置本地化表按语种拆分后处理（dataPostprocess=split-aot-localization，仅挂 aot target）。
///     把 tblocalizationaot 整表在 manifest 内拆为每语种一文件（整表不再落盘，语种删除后重导自动清理）：
///     - 文件名 tblocalizationaot_<语言>.<json|bytes>，写回 outputDataDir 内（FileCleaner 自动清旧变体与删语种残留）；
///     - json 数据变体：每行 2 键（key + value）；
///     - bin 数据变体：WriteSize 行数 + 逐行 key + value（2 字段顺序列序）。
///     拆分后行内只有目标语种译文，运行时按注册表选当前语种文件 + 英语兜底文件。
///     自校验内置：行数/首行 key/value 断言失败即抛异常中断导出。
/// </summary>
[PostProcess("split-aot-localization", TargetFileType.DataExport)]
public class LocalizationAotSplitPostProcess : PostProcessBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// AOT 前置本地化表名
    /// </summary>
    private const string LocalizationTableName = "tblocalizationaot";

    /// <summary>
    /// json 序列化选项：非 ASCII 保留 UTF-8 原文不转义（产物可肉眼核对/diff）
    /// </summary>
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// 单文件级后处理（基类默认实现按文件循环调本方法，本处理器重载批量入口，不需要单文件路径）
    /// </summary>
    public override void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest, OutputFile outputFile)
    {
        // 批量入口已处理全部文件，此实现为空（不会被调用——基类 PostProcess(old, new) 被本类重载遮蔽）
    }

    public override void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest)
    {
        // 守卫：只处理 aot gen target（aot/server 即使误挂参数也不拆分）
        if (GenerationContext.Current.Target.Name != "aot")
        {
            CopyAll(oldOutputFileManifest, newOutputFileManifest);
            return;
        }

        var table = GenerationContext.Current.ExportTables.FirstOrDefault(t => t.OutputDataFile == LocalizationTableName);
        if (table == null)
        {
            s_logger.Warn("aot target 未找到表 {Table}，跳过拆分", LocalizationTableName);
            CopyAll(oldOutputFileManifest, newOutputFileManifest);
            return;
        }

        var fullTableName = $"{LocalizationTableName}{GetManifestFileExtension(oldOutputFileManifest)}";

        // 其余文件照拷（理论为空），本地化整表文件跳过不拷贝（不落盘）
        foreach (var outputFile in oldOutputFileManifest.DataFiles)
        {
            if (outputFile.File == fullTableName)
            {
                continue;
            }
            newOutputFileManifest.AddFile(outputFile);
        }

        // 从原始 Record 按语种拆分产出
        var records = GenerationContext.Current.GetTableDataInfo(table).FinalRecords;
        var bean = table.ValueTType.DefBean;
        var languageFields = bean.ExportFields
            .Where(f => f.CType is TString && f.Name != "key")
            .ToList();
        var keyField = bean.ExportFields.FirstOrDefault(f => f.Name == "key")
            ?? throw new Exception($"AOT 本地化表 '{table.Name}' 缺少 key 字段，无法拆分");

        foreach (var languageField in languageFields)
        {
            newOutputFileManifest.AddFile(BuildLanguageFile(records, keyField, languageField, oldOutputFileManifest));
        }

        s_logger.Info("AOT 本地化表拆分完成: {Count} 个语种文件 (源记录 {Rows} 行)", languageFields.Count, records.Count);
    }

    /// <summary>
    /// 构建单语种产物（双变体内容分派 + 内存自校验）
    /// </summary>
    private OutputFile BuildLanguageFile(List<Record> records, DefField keyField, DefField languageField, OutputFileManifest manifest)
    {
        var variant = GetManifestFileExtension(manifest);
        var fileName = $"{LocalizationTableName}_{languageField.Name}{variant}";

        if (variant == ".json")
        {
            var content = BuildJsonContent(records, keyField.Name, languageField.Name);
            VerifyJson(content, records, keyField.Name, languageField.Name);
            return new OutputFile { File = fileName, Content = content };
        }
        else
        {
            var bytes = BuildBinContent(records, keyField.Name, languageField);
            VerifyBin(bytes, records, keyField.Name, languageField);
            return new OutputFile { File = fileName, Content = bytes };
        }
    }

    /// <summary>
    /// 按数据 manifest 扩展名识别数据变体
    /// </summary>
    private static string GetManifestFileExtension(OutputFileManifest manifest)
    {
        foreach (var f in manifest.DataFiles)
        {
            if (f.File.StartsWith(LocalizationTableName + ".", StringComparison.Ordinal))
            {
                return Path.GetExtension(f.File);
            }
        }
        throw new Exception("manifest 中未找到 AOT 本地化整表文件，无法识别数据变体");
    }

    /// <summary>
    /// 取 Record 的字符串字段值（key/语种字段均为 string，缺失/空按空串——兜底语义由运行时接管）
    /// </summary>
    private static string GetStringValue(Record record, string fieldName)
    {
        return record.Data.GetField(fieldName) is DString value ? value.Value ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// json 变体内容：2 键数组（key + value），中文保留 UTF-8 原文
    /// </summary>
    private static string BuildJsonContent(List<Record> records, string keyFieldName, string languageFieldName)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        foreach (var record in records)
        {
            var key = GetStringValue(record, keyFieldName);
            var targetText = GetStringValue(record, languageFieldName);
            sb.Append("\n  {");
            sb.Append($"\n    \"{keyFieldName}\": {JsonSerializer.Serialize(key, JsonWriteOptions)},");
            sb.Append($"\n    \"value\": {JsonSerializer.Serialize(targetText, JsonWriteOptions)}");
            sb.Append("\n  },");
        }

        if (records.Count > 0)
        {
            sb.Remove(sb.Length - 1, 1);
            sb.Append("\n]");
        }
        else
        {
            sb.Append(']');
        }

        return sb.ToString();
    }

    /// <summary>
    /// bin 变体内容：WriteSize 行数 + 逐行 key + value（2 字段顺序列序）
    /// </summary>
    private static byte[] BuildBinContent(List<Record> records, string keyFieldName, DefField languageField)
    {
        var buf = new ByteBuf();
        buf.WriteSize(records.Count);
        foreach (var record in records)
        {
            buf.WriteString(GetStringValue(record, keyFieldName));
            buf.WriteString(GetStringValue(record, languageField.Name));
        }
        return buf.CopyData();
    }

    /// <summary>
    /// json 产物自校验：可解析 + 行数一致 + 首行 key/value 与 Record[0] 一致（失败中断导出）
    /// </summary>
    private static void VerifyJson(string content, List<Record> records, string keyFieldName, string languageFieldName)
    {
        using var doc = JsonDocument.Parse(content);
        var count = 0;
        foreach (var _ in doc.RootElement.EnumerateArray())
        {
            count++;
        }
        if (count != records.Count)
        {
            throw new Exception($"AOT 拆分自校验失败(json): 行数 {count} != {records.Count}");
        }

        if (records.Count == 0)
        {
            return;
        }

        var first = doc.RootElement.EnumerateArray().First();
        if (first.GetProperty(keyFieldName).GetString() != GetStringValue(records[0], keyFieldName)
            || first.GetProperty("value").GetString() != GetStringValue(records[0], languageFieldName))
        {
            throw new Exception("AOT 拆分自校验失败(json): 首行值与源数据不一致");
        }
    }

    /// <summary>
    /// bin 产物自校验：行数前缀一致 + 首行 key/value 与 Record[0] 一致（Luban ByteBuf 读回）
    /// </summary>
    private static void VerifyBin(byte[] bytes, List<Record> records, string keyFieldName, DefField languageField)
    {
        var buf = new ByteBuf(bytes);
        if (buf.ReadSize() != records.Count)
        {
            throw new Exception("AOT 拆分自校验失败(bin): 行数前缀与源数据不一致");
        }

        if (records.Count == 0)
        {
            return;
        }

        if (buf.ReadString() != GetStringValue(records[0], keyFieldName)
            || buf.ReadString() != GetStringValue(records[0], languageField.Name))
        {
            throw new Exception("AOT 拆分自校验失败(bin): 首行值与源数据不一致");
        }
    }

    /// <summary>
    /// 非拆分路径：manifest 全量拷贝
    /// </summary>
    private static void CopyAll(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest)
    {
        foreach (var outputFile in oldOutputFileManifest.DataFiles)
        {
            newOutputFileManifest.AddFile(outputFile);
        }
    }
}
