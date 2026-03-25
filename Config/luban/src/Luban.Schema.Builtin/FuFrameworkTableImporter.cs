using Luban.Defs;
using Luban.RawDefs;
using Luban.Utils;
using System.Text.RegularExpressions;

namespace Luban.Schema.Builtin;

/// <summary>
/// FuFramework 自定义表格导入器
/// 
/// 自定义功能：
/// 1.支持按目标分组过滤
/// 2.支持排除路径配置
/// 3.支持文件名前缀排序
/// 
/// 文件名格式："前缀-表名-描述-*"  或  "前缀-表名-分组-描述-*"
/// 类注释从"描述"部分提取
/// </summary>
[TableImporter("fuframework")]
public class FuFrameworkTableImporter : ITableImporter
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 加载导入的表格
    /// </summary>
    /// <returns>原始表格列表</returns>
    public List<RawTable> LoadImportTables()
    {
        string dataDir = GenerationContext.GlobalConf.InputDataDir;// 获取数据输入目录
        var groups = GenerationContext.GlobalConf.Groups;          // 获取所有分组配置
        var targets = GenerationContext.GlobalConf.Targets;        // 获取所有目标配置

        // 获取排除路径列表，从环境变量中读取，多个路径用逗号分隔
        var excludePaths = EnvManager.Current.GetOptionOrDefault("tableImporter", "excludePaths", false, "").Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        s_logger.Info("exclude paths: " + string.Join(",", excludePaths));

        // 获取目标名称
        string targetName = EnvManager.Current.GetOptionOrDefault("tableImporter", "target", false, "");
        // 查找对应的导出目标
        var exportTarget = targets.Find(m => m.Name == targetName);

        // 获取文件名匹配模式，默认为匹配字母数字开头的字符串，方便自定义表格的排序
        string fileNamePatternStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "filePattern", false, "([a-zA-Z0-9]-.+)");
        // 获取表格命名空间格式
        string tableNamespaceFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNamespaceFormat", false, "{0}");
        // 获取表格名称格式，默认为 Tb 前缀
        string tableNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNameFormat", false, "Tb{0}");
        // 获取值类型名称格式
        string valueTypeNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "valueTypeNameFormat", false, "{0}");
        // 编译文件名匹配正则表达式
        var fileNamePattern = new Regex(fileNamePatternStr);
        // 定义支持的 Excel 文件扩展名集合
        var excelExts = new HashSet<string> { "xlsx", "xls", "xlsm", "csv" };

        // 创建表格列表
        var tables = new List<RawTable>();
        
        // 遍历数据目录下的所有文件
        foreach (string file in Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories))
        {
            // 检查是否为需要忽略的文件，路径中的任何部分是否以 . （隐藏文件）、 _ （私有文件）或 ~ （临时文件）开头
            if (FileUtil.IsIgnoreFile(dataDir, file))
            {
                continue;
            }

            // 计算相对路径
            string relativePath = file.Substring(dataDir.Length + 1).TrimStart('\\').TrimStart('/');
            // 检查文件是否在排除路径中
            if (excludePaths?.Any(excludePath => relativePath.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase)) == true)
            {
                continue;
            }

            // 获取文件名
            string fileName = Path.GetFileName(file);
            // 获取文件扩展名
            string ext = Path.GetExtension(fileName).TrimStart('.');
            // 检查是否为支持的 Excel 文件类型
            if (!excelExts.Contains(ext))
            {
                continue;
            }

            // 获取不带扩展名的文件名
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // 使用正则表达式匹配文件名
            var match = fileNamePattern.Match(fileNameWithoutExt);
            // 检查匹配是否成功
            if (!match.Success || match.Groups.Count <= 1)
            {
                continue;
            }

            // 从相对路径生成命名空间
            string namespaceFromRelativePath = Path.GetDirectoryName(relativePath).Replace('/', '.').Replace('\\', '.');

            // 获取原始表格全名
            string rawTableFullName = match.Groups[1].Value;
            // 按 - 或 _ 分割文件名
            var split = rawTableFullName.Split(['-', '_',], StringSplitOptions.RemoveEmptyEntries);
            // 如果有多个部分，取第二部分作为表名
            if (split.Length > 1)
            {
                // 代表有首字母的排序, 不管后面有多少都只要第二个切片
                // 获取中间的值
                rawTableFullName = split[1];
            }

            // 用于存储提取到的描述作为注释
            string comment = "";

            // 检查是否有分组信息或描述
            if (split.Length > 2)
            {
                // 获取第3部分
                string part3 = split[2].Trim();
                // 判断第3部分是否是预定义的分组名称
                bool isPart3Group = groups.Any(group => group.Names.Contains(part3, StringComparer.OrdinalIgnoreCase));

                if (isPart3Group)
                {
                    // 第3部分是分组，需要检查导出权限
                    string groupName = part3.ToLower();
                    // 判断是否导出
                    bool isExport = exportTarget.Groups.Any(targetGroupName => targetGroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase));

                    // 如果不导出则跳过
                    if (!isExport)
                    {
                        continue;
                    }

                    // 有分组时，使用第4部分作为描述（如果存在）
                    if (split.Length > 3)
                    {
                        comment = split[3].Trim();
                    }
                }
                else
                {
                    // 第3部分不是分组，则作为描述使用
                    comment = part3;
                }
            }

            // 检查表名是否包含中文
            if (IsContainsZhCn(rawTableFullName))
            {
                throw new Exception($"不支持中文表名:[{rawTableFullName}] 文件名:[{fileName}] 表名称定义规范为: 排序编号-导出表名-中文标识名称");
            }

            // 从全名中提取命名空间
            string rawTableNamespace = TypeUtil.GetNamespace(rawTableFullName);
            // 从全名中提取表名
            string rawTableName = TypeUtil.GetName(rawTableFullName);
            // 组合最终的表格命名空间
            string tableNamespace = TypeUtil.MakeFullName(namespaceFromRelativePath, string.Format(tableNamespaceFormatStr, rawTableNamespace));
            // 格式化表格名称
            string tableName = string.Format(tableNameFormatStr, rawTableName);
            // 组合值类型的完整名称
            string valueTypeFullName = TypeUtil.MakeFullName(tableNamespace, string.Format(valueTypeNameFormatStr, rawTableName));

            // 检查表格是否已存在
            bool isExist = false;
            foreach (var rawTable in tables)
            {
                // 如果命名空间和名称都相同，则认为是同一个表
                if (rawTable.Namespace == tableNamespace && rawTable.Name == tableName)
                {
                    // 添加输入文件到现有表
                    rawTable.InputFiles.Add(relativePath);
                    isExist = true;
                    break;
                }
            }

            // 如果表格已存在则跳过
            if (isExist)
            {
                continue;
            }

            // 创建新的原始表格对象
            var table = new RawTable()
            {
                Namespace = tableNamespace,          // 命名空间
                Name = tableName,                    // 表格名称
                Index = "",                          // 索引
                ValueType = valueTypeFullName,       // 值类型
                ReadSchemaFromFile = true,           // 从文件读取 Schema
                Mode = TableMode.MAP,                // 表格模式为 Map
                Comment = comment,                   // 注释（从文件名中提取的描述）
                Groups = new List<string> { },       // 分组列表
                InputFiles = new List<string> { relativePath }, // 输入文件列表
                OutputFile = "",                     // 输出文件
            };
            // 记录调试日志
            s_logger.Debug("import table file:{@}", table);
            // 添加到表格列表
            tables.Add(table);
        }

        // 返回所有表格
        return tables;
    }

    /// <summary>
    /// 匹配中文正则表达式
    /// </summary>
    private static readonly Regex CnReg = new Regex(@"[\u4e00-\u9fa5]");

    /// <summary>
    /// 判断是否有中文
    /// </summary>
    /// <param name="self">原始字符串</param>
    /// <returns>如果包含中文返回 true，否则返回 false</returns>
    private static bool IsContainsZhCn(string self)
    {
        return CnReg.IsMatch(self);
    }
}