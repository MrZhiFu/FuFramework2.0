#nullable enable
using Luban.Defs;
using Luban.RawDefs;
using Luban.Utils;
using System.Text.RegularExpressions;

namespace Luban.Schema.Builtin;

/// <summary>
/// FuFramework 自定义表格导入器
///
/// 主要功能：
/// 收集整理配置表的相关信息，包括命名空间，名称，配置表路径等，然后整理到一个列表中，方便后续加载读取配置表
/// 
/// 自定义功能：
/// 1.支持按目标分组过滤
/// 2.支持排除路径配置
/// 3.支持文件名前缀编号排序
/// 
/// 文件名格式："排序编号-导出表名-分组(可选)-中文标识名称
/// 类注释从"描述"部分提取
/// </summary>
[TableImporter("fuframework")]
public class FuFrameworkTableImporter : ITableImporter
{
    /// <summary>
    /// 配置信息记录
    /// </summary>
    private class ImportSetting
    {
        /// <summary>
        /// 数据输入目录，如：Excels
        /// </summary>
        public string DataDir { get; set; } = "";

        /// <summary>
        /// 分组配置列表，如：c(客户端), s(服务器)
        /// </summary>
        public List<RawGroup> Groups { get; set; } = new();

        /// <summary>
        /// 导出目标配置，如：{ "name": "client", "groups": ["c"] }
        /// </summary>
        public RawTarget? ExportTarget { get; set; }

        /// <summary>
        /// 排除路径列表，如：["Test", "Backup"]
        /// </summary>
        public List<string> ExcludePaths { get; set; } = new();

        /// <summary>
        /// 文件名匹配正则表达式，如：([a-zA-Z0-9]-.+)，表示匹配所有以字母或数字开头的文件名
        /// 匹配格式：排序编号-导出表名-分组(可选)-中文标识名称
        /// 例如：D-Item-Client-道具表.xlsx
        /// </summary>
        public Regex FileNamePattern { get; set; } = null!;

        /// <summary>
        /// 表格命名空间格式化字符串，如：{0}
        /// </summary>
        public string TableNamespaceFormat { get; set; } = "{0}";

        /// <summary>
        /// 表格类名格式化字符串，如：Tb{0}
        /// </summary>
        public string TableNameFormat { get; set; } = "Tb{0}";

        /// <summary>
        /// 值类型名称格式化字符串，如：{0} 或 {0}Bean
        /// </summary>
        public string ValueTypeNameFormat { get; set; } = "{0}";
    }

    /// <summary>
    /// 表格信息
    /// </summary>
    private class TableInfo
    {
        /// <summary>
        /// 原始表名（从文件名解析），如：Item
        /// </summary>
        public string RawName { get; set; } = "";

        /// <summary>
        /// 原始命名空间，如：Sub.Item 中的 Sub
        /// </summary>
        public string RawNamespace { get; set; } = "";

        /// <summary>
        /// 最终命名空间，如：Tables 或 Tables.Sub
        /// </summary>
        public string Namespace { get; set; } = "";

        /// <summary>
        /// 最终表名，如：TbItem
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 值类型完整名称，如：Tables.Item
        /// </summary>
        public string ValueType { get; set; } = "";

        /// <summary>
        /// 表注释，如：道具表
        /// </summary>
        public string Comment { get; set; } = "";
    }

    /// <summary>
    /// 日志记录器
    /// </summary>
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 支持的 Excel 文件扩展名
    /// </summary>
    private static readonly HashSet<string> s_excelExtensions = new(StringComparer.OrdinalIgnoreCase) { "xlsx", "xls", "xlsm", "csv" };

    /// <summary>
    /// 匹配中文正则表达式
    /// </summary>
    private static readonly Regex s_cnRegex = new(@"[\u4e00-\u9fa5]");

    /// <summary>
    /// 从配置表根目录中，收集整理配置表相关信息
    /// </summary>
    /// <returns>所有配置表的原始相关信息列表</returns>
    public List<RawTable> LoadImportTables()
    {
        // 将conf配置和命令行中的相关参数，将其整理成一个配置导入设置信息，包括数据目录，分组，导出目标，排除路径，文件名模式，导出表名格式，导出表值类型名格式
        var config = FormatImportSetting();

        // 遍历配置表数据目录, 处理所有配置表文件
        var targetTables = new List<RawTable>();
        foreach (var file in Directory.GetFiles(config.DataDir, "*", SearchOption.AllDirectories))
        {
            if (TryProcessFile(file, config, out var rawTable))
            {
                // 合并或添加表格到目标列表中
                AddOrMergeTable(targetTables, rawTable);
            }
        }

        return targetTables;
    }

    /// <summary>
    /// 整理导入设置。
    /// 将conf配置和命令行中的相关参数，将其整理成一个配置导入设置信息，
    /// 包括数据目录，分组，导出目标，排除路径，文件名模式，导出表名格式，导出表值类型名格式 
    /// </summary>
    /// <returns>配置导入设置信息</returns>
    private ImportSetting FormatImportSetting()
    {
        var targetName = EnvManager.Current.GetOptionOrDefault("tableImporter", "target", false, "");

        // 解析排除路径
        var excludePaths = ParseExcludePaths();
        if (excludePaths.Count > 0)
        {
            s_logger.Info("exclude paths: " + string.Join(",", excludePaths));
        }

        // 整理成导入设置信息
        return new ImportSetting
        {
            DataDir              = GenerationContext.GlobalConf.InputDataDir,
            Groups               = GenerationContext.GlobalConf.Groups,
            ExportTarget         = GenerationContext.GlobalConf.Targets.Find(m => m.Name == targetName),
            ExcludePaths         = excludePaths,
            FileNamePattern      = new Regex(EnvManager.Current.GetOptionOrDefault("tableImporter", "filePattern", false, "([a-zA-Z0-9]-.+)")),
            TableNamespaceFormat = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNamespaceFormat", false, "{0}"),
            TableNameFormat      = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNameFormat",      false, "Tb{0}"),
            ValueTypeNameFormat  = EnvManager.Current.GetOptionOrDefault("tableImporter", "valueTypeNameFormat",  false, "{0}")
        };
    }

    /// <summary>
    /// 解析排除路径。
    /// 支持逗号分隔的路径列表，每个路径可以是绝对路径或相对路径，也可以是通配符（如：*.xlsx）
    /// </summary>
    /// <returns>排除的路径列表</returns>
    private static List<string> ParseExcludePaths()
    {
        var paths = EnvManager.Current.GetOptionOrDefault("tableImporter", "excludePaths", false, "");
        return paths.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
    }

    /// <summary>
    /// 尝试处理单个文件。
    /// 支持 Excel 文件扩展名，如：xlsx,xls,xlsm,csv
    /// 支持文件名前缀编号排序
    /// </summary>
    /// <param name="file">要处理的文件路径</param>
    /// <param name="importSetting">导入设置信息</param>
    /// <param name="rawTable">处理后的原始配置表信息</param>
    /// <returns>是否成功处理文件</returns>
    private bool TryProcessFile(string file, ImportSetting importSetting, out RawTable rawTable)
    {
        rawTable = null!;

        // 忽略隐藏/私有/临时文件。这里主要忽略定义文件，如：__beans__.xlsx, __enums__.xlsx,__tables__.xlsx
        if (FileUtil.IsIgnoreFile(importSetting.DataDir, file))
            return false;

        // 获取相对路径(如：Tables/D-Item—道具表.xlsx)，检查是否需要排除路径
        var relativePath = GetRelativePath(file, importSetting.DataDir);
        if (IsExcluded(relativePath, importSetting.ExcludePaths))
            return false;

        // 获取文件名和文件扩展名，如：D-Item—道具表.xlsx => D-Item—道具表.xlsx, xlsx
        var fileName = Path.GetFileName(file);
        var ext      = Path.GetExtension(fileName).TrimStart('.');

        // 检查文件类型
        if (!s_excelExtensions.Contains(ext))
            return false;

        // 获取不带文件扩展名的文件名，如：D-Item—道具表.xlsx => D-Item—道具表
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var match              = importSetting.FileNamePattern.Match(fileNameWithoutExt);

        // 检查正则匹配
        if (!match.Success || match.Groups.Count <= 1)
            return false;

        // 解析表格信息
        var tableInfo = ParseTableInfo(match.Groups[1].Value, relativePath, importSetting);
        if (tableInfo == null)
            return false;

        // 创建原始配置表信息
        rawTable = CreateRawTable(tableInfo, relativePath);
        return true;
    }

    /// <summary>
    /// 获取相对路径，如：Excels/Tables/D-Item—道具表.xlsx => Tables/D-Item—道具表.xlsx
    /// </summary>
    /// <param name="file"></param>
    /// <param name="dataDir"></param>
    /// <returns></returns>
    private static string GetRelativePath(string file, string dataDir)
    {
        return file.Substring(dataDir.Length + 1).TrimStart('\\', '/');
    }

    /// <summary>
    /// 检查是否被排除
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="excludePaths"></param>
    /// <returns></returns>
    private static bool IsExcluded(string relativePath, List<string> excludePaths)
    {
        return excludePaths.Any(excludePath => relativePath.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 解析表格信息
    /// </summary>
    /// <param name="rawFullName">原始表名，如：D-Item—道具表</param>
    /// <param name="relativePath">相对路径，如：Tables/D-Item—道具表.xlsx</param>
    /// <param name="importSetting">导入设置信息</param>
    /// <returns>解析后的表格信息</returns>
    private TableInfo? ParseTableInfo(string rawFullName, string relativePath, ImportSetting importSetting)
    {
        // 分割表名，如：D-Item—道具表 => D, Item, 道具表
        var parts = rawFullName.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        // 提取表名，如：Item
        var rawName = parts[1];

        // 检查表名是否包含中文
        if (s_cnRegex.IsMatch(rawName))
        {
            throw new Exception($"不支持中文表名:[{rawName}] 文件名:[{relativePath}] 表名称定义规范为: 排序编号-导出表名-分组(可选)-中文标识名称");
        }

        // 解析注释和导出状态
        var (comment, isExport) = ParseCommentAndExport(parts, importSetting);
        if (!isExport)
            return null;

        // 提取命名空间，如：Tables
        var namespaceFromPath = Path.GetDirectoryName(relativePath)?.Replace('/', '.').Replace('\\', '.') ?? "";
        var rawNamespace      = TypeUtil.GetNamespace(rawName);

        // 创建表格信息
        return new TableInfo
        {
            RawName      = rawName,
            RawNamespace = rawNamespace,
            Namespace    = TypeUtil.MakeFullName(namespaceFromPath, string.Format(importSetting.TableNamespaceFormat, rawNamespace)),
            Name         = string.Format(importSetting.TableNameFormat, TypeUtil.GetName(rawName)),
            ValueType    = TypeUtil.MakeFullName(namespaceFromPath, string.Format(importSetting.ValueTypeNameFormat, TypeUtil.GetName(rawName))),
            Comment      = rawName.Equals("Localization") ? "本地化多语言表" : comment
        };
    }

    /// <summary>
    /// 解析注释和导出状态
    /// </summary>
    /// <param name="rawTableName">表名，如：D-Item—道具表</param>
    /// <param name="importSetting">导入设置信息</param>
    /// <returns>注释和导出状态</returns>
    private (string comment, bool isExport) ParseCommentAndExport(string[] rawTableName, ImportSetting importSetting)
    {
        // 检查表名是否符合
        if (rawTableName.Length <= 2)
            return ("", true);

        // 检查是否有分组，分组在第3部分
        var part3   = rawTableName[2].Trim();
        var isGroup = importSetting.Groups.Any(g => g.Names.Contains(part3, StringComparer.OrdinalIgnoreCase));
        if (!isGroup)
            return (part3, true);

        // 是分组，检查导出分组是否是导出的目标分组
        var canExport = importSetting.ExportTarget?.Groups.Any(g => g.Equals(part3, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (!canExport)
            return ("", false);

        // 有分组时，使用第4部分作为描述
        var comment = rawTableName.Length > 3 ? rawTableName[3].Trim() : "";
        return (comment, true);
    }

    /// <summary>
    /// 创建 RawTable
    /// <param name="info">表格信息</param>
    /// <param name="relativePath">相对路径</param>
    /// <returns>RawTable</returns>
    /// </summary>
    private static RawTable CreateRawTable(TableInfo info, string relativePath)
    {
        return new RawTable
        {
            Namespace          = info.Namespace,
            Name               = info.Name,
            Index              = "",
            ValueType          = info.ValueType,
            ReadSchemaFromFile = true,
            Mode               = TableMode.MAP,
            Comment            = info.Comment,
            Groups             = new List<string>(),
            InputFiles         = new List<string> { relativePath },
            OutputFile         = ""
        };
    }

    /// <summary>
    /// 添加或合并表格。
    /// 如果表格已存在，合并输入文件路径；如果不存在，添加新表格。
    /// <param name="tables">表格列表</param>
    /// <param name="newTable">新表格</param>
    /// </summary>
    private static void AddOrMergeTable(List<RawTable> tables, RawTable newTable)
    {
        var existing = tables.FirstOrDefault(t => t.Namespace == newTable.Namespace && t.Name == newTable.Name);

        if (existing != null)
        {
            // 如果已存在，合并输入文件路径，如：多个多语言配置表会将schema文件路径合并到一个InputFiles列表中
            existing.InputFiles.AddRange(newTable.InputFiles);
        }
        else
        {
            tables.Add(newTable);
        }
    }
}
