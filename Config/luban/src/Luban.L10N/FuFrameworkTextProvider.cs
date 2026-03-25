using Luban.DataLoader;
using Luban.Datas;
using Luban.Defs;
using Luban.RawDefs;
using Luban.Types;
using Luban.Utils;

namespace Luban.L10N;

/// <summary>
/// FuFramework 文本提供器
/// 自定义功能：
/// 1. 支持多文件批量加载
/// 2. 显式检查 Excel 扩展名是否正确
/// </summary>
[TextProvider("fuframework")]
public class FuFrameworkTextProvider : ITextProvider
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 文本键字段名
    /// </summary>
    private string _keyFieldName;

    /// <summary>
    /// 文本值字段名
    /// </summary>
    private string _ValueFieldName;

    /// <summary>
    /// 是否将文本键转换为值
    /// </summary>
    private bool _convertTextKeyToValue;

    /// <summary>
    /// 文本字典，存储键值对
    /// </summary>
    private readonly Dictionary<string, string> _texts = new();

    /// <summary>
    /// 未知的文本键集合
    /// </summary>
    private readonly HashSet<string> _unknownTextKeys = new();

    /// <summary>
    /// 加载文本配置
    /// </summary>
    public void Load()
    {
        // 获取环境变量管理器
        var env = EnvManager.Current;

        // 获取文本键字段名，一般为 "Key"
        _keyFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileKeyFieldName, false, "");

        // 检查键字段名是否为空
        if (string.IsNullOrWhiteSpace(_keyFieldName))
        {
            throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileKeyFieldName}=xxx' missing");
        }

        // 获取是否转换文本键为值的配置
        _convertTextKeyToValue = DataUtil.ParseBool(env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NConvertTextKeyToValue, false, "false"));

        // 如果需要转换，获取值字段名
        if (_convertTextKeyToValue)
        {
            _ValueFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileLanguageFieldName, false, "");

            // 检查值字段名称是否为空
            if (string.IsNullOrWhiteSpace(_ValueFieldName))
            {
                throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileLanguageFieldName}=xxx' missing");
            }
        }

        // 获取文本提供器文件路径
        var textProviderFile = env.GetOption(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFilePath, false);

        // 从文件加载文本列表
        LoadTextListFromFile(textProviderFile);
    }

    /// <summary>
    /// 是否将文本键转换为值
    /// </summary>
    public bool ConvertTextKeyToValue => _convertTextKeyToValue;

    /// <summary>
    /// 检查键是否有效
    /// </summary>
    /// <param name="key">文本键</param>
    /// <returns>如果键存在返回 true，否则返回 false</returns>
    public bool IsValidKey(string key)
    {
        return _texts.ContainsKey(key);
    }

    /// <summary>
    /// 尝试获取文本
    /// </summary>
    /// <param name="key">文本键</param>
    /// <param name="text">输出文本值</param>
    /// <returns>如果获取成功返回 true，否则返回 false</returns>
    public bool TryGetText(string key, out string text)
    {
        return _texts.TryGetValue(key, out text);
    }

    /// <summary>
    /// 从文件路径下加载文本列表
    /// </summary>
    /// <param name="path">文件路径</param>
    private void LoadTextListFromFile(string path)
    {
        // 创建定义程序集
        var ass = new DefAssembly(new RawAssembly { Targets = [new RawTarget { Name = "default", Manager = "Tables" }], }, "default", [], null, null);

        // 创建原始字段列表
        var rawFields = new List<RawField> { new() { Name = _keyFieldName, Type = "string" }, };

        // 如果需要转换文本键为值，添加值字段
        if (_convertTextKeyToValue)
        {
            rawFields.Add(new RawField { Name = _ValueFieldName, Type = "string" });
        }

        // 创建文本记录类型定义
        var defTableRecordType = new DefBean(new RawBean
        {
            Namespace   = "__intern__",   // 内部命名空间
            Name        = "__TextInfo__", // 类型名称
            Parent      = "",             // 父类型
            Alias       = "",             // 别名
            IsValueType = false,          // 不是值类型
            Sep         = "",             // 分隔符
            Fields      = rawFields,      // 字段列表
        }) { Assembly = ass, };


        ass.AddType(defTableRecordType);  // 将类型添加到程序集
        defTableRecordType.PreCompile();  // 预编译
        defTableRecordType.Compile();     // 编译
        defTableRecordType.PostCompile(); // 编译后处理

        // 创建表格记录类型
        var tableRecordType = TBean.Create(false, defTableRecordType, null);

        // 获取目录信息
        var directoryInfo = new DirectoryInfo(path);

        // 检查目录是否存在
        if (!directoryInfo.Exists)
        {
            s_logger.Error($"path:{path} is not a directory. ignore it! return");
            return;
        }

        // 定义支持的 Excel 文件扩展名集合
        var excelExts = new HashSet<string> { "xlsx", "xls", "xlsm", "csv" };

        // 获取目录下的所有文件
        var fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

        // 遍历所有文件
        foreach (var fileInfo in fileInfos)
        {
            // 检查是否为需要忽略的文件
            if (FileUtil.IsIgnoreFile(path, fileInfo.Name))
            {
                continue;
            }


            var fileName = Path.GetFileName(fileInfo.Name);            // 获取文件名
            var ext      = Path.GetExtension(fileName).TrimStart('.'); // 获取文件扩展名

            // 检查是否为支持的 Excel 文件类型
            if (!excelExts.Contains(ext))
            {
                continue;
            }

            // 分割文件名和工作表名称
            var (actualFile, sheetName) = FileUtil.SplitFileAndSheetName(FileUtil.Standardize(fileInfo.FullName));

            // 加载表格文件数据
            var records = DataLoaderManager.Ins.LoadTableFile(tableRecordType, actualFile, sheetName, new Dictionary<string, string>());

            // 遍历所有记录
            foreach (var r in records)
            {
                // 获取数据 Bean
                DBean data = r.Data;

                // 获取键值
                var key = ((DString)data.GetField(_keyFieldName)).Value;

                // 获取值（如果需要转换则获取值字段，否则使用键作为值）
                var value = _convertTextKeyToValue ? ((DString)data.GetField(_ValueFieldName)).Value : key;

                // 检查键是否为空
                if (string.IsNullOrEmpty(key))
                {
                    s_logger.Error("textFile:{} key:{} is empty. ignore it!", fileName, key);
                    continue;
                }

                // 尝试添加到文本字典
                if (!_texts.TryAdd(key, value))
                {
                    s_logger.Error("textFile:{} key:{} is duplicated", fileName, key);
                }
            }
        }
    }

    /// <summary>
    /// 添加未知键
    /// </summary>
    /// <param name="key">未知的文本键</param>
    public void AddUnknownKey(string key)
    {
        _unknownTextKeys.Add(key);
    }

    /// <summary>
    /// 处理数据
    /// </summary>
    public void ProcessDatas()
    {
        // 如果需要转换文本键为值
        if (_convertTextKeyToValue)
        {
            // 创建文本键到值的转换器
            var trans = new TextKeyToValueTransformer(this);

            // 遍历所有表格
            foreach (var table in GenerationContext.Current.Tables)
            {
                // 遍历表格中的所有数据
                foreach (var record in GenerationContext.Current.GetTableAllDataList(table))
                {
                    // 应用转换器转换数据
                    record.Data = (DBean)record.Data.Apply(trans, table.ValueTType);
                }
            }
        }
    }
}
