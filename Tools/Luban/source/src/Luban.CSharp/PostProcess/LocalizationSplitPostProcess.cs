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
/// 本地化表按语种拆分后处理（dataPostprocess=split-localization，仅挂 client target）。
///     把 tblocalization 整表在 manifest 内拆为 15 个单语种文件（整表不再落盘）：
///     - json 数据变体：每行 2 键（key + 目标语种），其余语种键缺失（运行时宽容反序列化 + 英语兜底表二段查询接管）；
///     - bin 数据变体：16 字段顺序列序（WriteSize 行数 + 逐行 key + 15 语种字符串），目标语种有值其余空串
///       （列序与生成模板读取序天然对齐，模板零改动）。
///     运行时消费见 Unity 工程 LocalizationModule.SetLanguageAsync / LocalizationProvider（英语兜底懒加载）。
///     aot/server target 不挂本处理器（bat 参数隔离）+ TargetName 守卫双保险。
///     自校验内置：行数/首行值断言失败即抛异常中断导出。
/// </summary>
[PostProcess("split-localization", TargetFileType.DataExport)]
public class LocalizationSplitPostProcess : PostProcessBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 单文件级后处理（基类默认实现按文件循环调本方法，本处理器重载批量入口，不需要单文件路径）
    /// </summary>
    public override void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest, OutputFile outputFile)
    {
        // 批量入口已处理全部文件，此实现为空（不会被调用——基类 PostProcess(old, new) 被本类重载遮蔽）
    }

    /// <summary>
    /// 本地化表名（client target 的热更本地化表）
    /// </summary>
    private const string LocalizationTableName = "tblocalization";

    /// <summary>
    /// 产物文件扩展名（两个数据变体统一 .bytes，与 YooAsset 收集器路径解耦——变体只决定内容格式）
    /// </summary>
    private const string OutputExtension = ".bytes";

    /// <summary>
    /// 产物子目录（相对 outputDataDir）：Unity 侧独立 YooAsset 收集组目录 Bundles/Localization，
    /// 是 outputDataDir（Bundles/Config）的兄弟目录——File 经 LocalFileSaver 拼接后由 OS 路径规范化跨出 outputDataDir。
    /// 注意：与"产物目录在 outputDataDir 兄弟位置"的工程结构耦合，挪目录须同步本常量与 YooAsset 收集器 CollectPath。
    /// </summary>
    private const string OutputSubDir = "../Localization";

    /// <summary>
    /// json 序列化选项：非 ASCII 保留 UTF-8 原文不转义（产物可肉眼核对/diff）
    /// </summary>
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public override void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest)
    {
        // 守卫：只处理 client gen target（manifest.TargetName 是 data target 名 json/bytes，
        // gen target 须从 GenerationContext.Current.Target 取——aot/server target 即使误挂参数也不拆分）
        if (GenerationContext.Current.Target.Name != "client")
        {
            CopyAll(oldOutputFileManifest, newOutputFileManifest);
            return;
        }

        // 定位本地化表的整表文件（数据段产物名 = DefTable.OutputDataFile + 扩展名）
        var table = GenerationContext.Current.ExportTables.FirstOrDefault(t => t.OutputDataFile == LocalizationTableName);
        if (table == null)
        {
            // client target 无本地化表（理论不发生）：原样拷贝，不拆分
            s_logger.Warn("client target 未找到表 {Table}，跳过拆分", LocalizationTableName);
            CopyAll(oldOutputFileManifest, newOutputFileManifest);
            return;
        }

        var fullTableName = $"{LocalizationTableName}{GetManifestFileExtension(oldOutputFileManifest)}";

        // 其余文件照拷（业务表等），本地化整表文件跳过不拷贝（不落盘）
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
        var languageFields = table.ValueTType.DefBean.ExportFields
            .Where(f => f.CType is TString && f.Name != "key")
            .ToList();
        var keyField = table.ValueTType.DefBean.ExportFields.FirstOrDefault(f => f.Name == "key")
            ?? throw new Exception($"本地化表 '{table.Name}' 缺少 key 字段，无法拆分");

        foreach (var languageField in languageFields)
        {
            var outputFile = BuildLanguageFile(records, keyField, languageField, oldOutputFileManifest);
            newOutputFileManifest.AddFile(outputFile);
        }

        s_logger.Info("本地化表拆分完成: {Count} 个语种文件 (源记录 {Rows} 行)", languageFields.Count, records.Count);
    }

    /// <summary>
    /// 构建单语种产物（双变体内容分派 + 内存自校验）
    /// </summary>
    private OutputFile BuildLanguageFile(List<Record> records, DefField keyField, DefField languageField, OutputFileManifest manifest)
    {
        var variant = GetManifestFileExtension(manifest); // json 数据变体 manifest 文件扩展名 .json / bin 变体 .bytes
        var fileName = $"{OutputSubDir}/{LocalizationTableName}_{languageField.Name}{OutputExtension}";

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
    /// 按数据 manifest 扩展名识别数据变体（json 变体整表 .json / bin 变体整表 .bytes）
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
        throw new Exception($"manifest 中未找到本地化整表文件，无法识别数据变体");
    }

    /// <summary>
    /// 取 Record 的字符串字段值（key/语种字段均为 string，缺失/空按空串——兜底语义由运行时接管）
    /// </summary>
    private static string GetStringValue(Record record, string fieldName)
    {
        return record.Data.GetField(fieldName) is DString value ? value.Value ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// json 变体内容：2 键数组（key + 目标语种），中文保留 UTF-8 原文
    /// </summary>
    private static string BuildJsonContent(List<Record> records, string keyFieldName, string languageFieldName)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        foreach (var record in records)
        {
            var key        = GetStringValue(record, keyFieldName);
            var targetText = GetStringValue(record, languageFieldName);
            sb.Append("\n  {");
            sb.Append($"\n    \"{keyFieldName}\": {JsonSerializer.Serialize(key, JsonWriteOptions)},");
            sb.Append($"\n    \"{languageFieldName}\": {JsonSerializer.Serialize(targetText, JsonWriteOptions)}");
            sb.Append("\n  },");
        }

        // 去掉最后一行的尾逗号，闭合数组
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
    /// bin 变体内容：16 字段顺序列序（WriteSize 行数 + 逐行 key + 15 语种字符串），Luban ByteBuf 同源编码
    /// </summary>
    private static byte[] BuildBinContent(List<Record> records, string keyFieldName, DefField languageField)
    {
        var buf = new ByteBuf();
        buf.WriteSize(records.Count);
        foreach (var record in records)
        {
            buf.WriteString(GetStringValue(record, keyFieldName));
            foreach (var field in languageField.HostType.ExportFields)
            {
                if (field == languageField)
                {
                    buf.WriteString(GetStringValue(record, field.Name));
                }
                else if (field.CType is TString && field.Name != keyFieldName)
                {
                    buf.WriteString(string.Empty);
                }
            }
        }
        return buf.CopyData();
    }

    /// <summary>
    /// json 产物自校验：可解析 + 行数一致 + 首行 key/目标值与 Record[0] 一致（失败中断导出）
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
            throw new Exception($"本地化拆分自校验失败(json): 行数 {count} != {records.Count}");
        }

        if (records.Count == 0)
        {
            return;
        }

        var first = doc.RootElement.EnumerateArray().First();
        var expectKey = GetStringValue(records[0], keyFieldName);
        var expectText = GetStringValue(records[0], languageFieldName);
        if (first.GetProperty(keyFieldName).GetString() != expectKey
            || first.GetProperty(languageFieldName).GetString() != expectText)
        {
            throw new Exception($"本地化拆分自校验失败(json): 首行值与源数据不一致");
        }
    }

    /// <summary>
    /// bin 产物自校验：行数前缀一致 + 首行 key/目标值与 Record[0] 一致（Luban ByteBuf 读回）
    /// </summary>
    private static void VerifyBin(byte[] bytes, List<Record> records, string keyFieldName, DefField languageField)
    {
        var buf = new ByteBuf(bytes);
        if (buf.ReadSize() != records.Count)
        {
            throw new Exception($"本地化拆分自校验失败(bin): 行数前缀与源数据不一致");
        }

        if (records.Count == 0)
        {
            return;
        }

        if (buf.ReadString() != GetStringValue(records[0], keyFieldName))
        {
            throw new Exception($"本地化拆分自校验失败(bin): 首行 key 与源数据不一致");
        }

        // 读回首行 15 语种，断言目标语种位与源一致
        foreach (var field in languageField.HostType.ExportFields)
        {
            if (field.CType is not TString || field.Name == keyFieldName)
            {
                continue;
            }
            var readBack = buf.ReadString();
            if (field == languageField && readBack != GetStringValue(records[0], field.Name))
            {
                throw new Exception($"本地化拆分自校验失败(bin): 首行目标语种值与源数据不一致");
            }
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
