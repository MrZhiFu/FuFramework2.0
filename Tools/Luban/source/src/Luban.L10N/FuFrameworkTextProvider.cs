using Luban.DataLoader;
using Luban.Datas;
using Luban.Defs;
using Luban.RawDefs;
using Luban.Types;
using Luban.Utils;

namespace Luban.L10N;

/// <summary>
/// FuFramework 多语言文本提供器
///
/// 主要功能：
/// 将所有多语言表的key都加载缓存到texts字典中，方便在需要将多语言键转换值时，直接使用texts字典的值替换key
///
/// 自定义功能：
/// 1. 支持多个多语言表批量加载，默认的DefaultTextProvider只支持一个。支持多个后方便进行多个模块的多语言表分别工作
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
    /// 多语言键字段名
    /// </summary>
    private string _keyFieldName;

    /// <summary>
    /// 多语言值字段名
    /// </summary>
    private string _valueFieldName;

    /// <summary>
    /// 多语言字典，存储键值对，key：多语言key，value：多语言内容
    /// </summary>
    private readonly Dictionary<string, string> _texts = new();

    /// <summary>
    /// 未知的多语言键集合
    /// </summary>
    private readonly HashSet<string> _unknownTextKeys = [];

    /// <summary>
    /// 是否将多语言键转换为值
    /// </summary>
    public bool ConvertTextKeyToValue { get; private set; }

    /// <summary>
    /// 加载多语言配置
    /// </summary>
    public void Load()
    {
        // 获取环境变量管理器
        var env = EnvManager.Current;

        // 从命令行中获取多语言键字段名，一般为 "key"
        _keyFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileKeyFieldName, false, "");

        // 检查键字段名是否为空
        if (string.IsNullOrWhiteSpace(_keyFieldName))
        {
            throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileKeyFieldName}=xxx' missing");
        }

        // 获取是否转换多语言键为值的配置
        ConvertTextKeyToValue = DataUtil.ParseBool(env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NConvertTextKeyToValue, false, "false"));

        // 如果需要转换，获取值字段名
        if (ConvertTextKeyToValue)
        {
            _valueFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileLanguageFieldName, false, "");

            // 检查值字段名称是否为空
            if (string.IsNullOrWhiteSpace(_valueFieldName))
            {
                throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileLanguageFieldName}=xxx' missing");
            }
        }

        // 从命令行中获取多语言提供器文件路径
        var textProviderFile = env.GetOption(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFilePath, false);

        // 从文件加载多语言列表
        LoadTextListFromFile(textProviderFile);
    }

    /// <summary>
    /// 检查多语言键是否有效
    /// </summary>
    /// <param name="key">多语言键</param>
    /// <returns>如果键存在返回 true，否则返回 false</returns>
    public bool IsValidKey(string key) => _texts.ContainsKey(key);

    /// <summary>
    /// 尝试获取多语言
    /// </summary>
    /// <param name="key">多语言键</param>
    /// <param name="text">输出多语言值</param>
    /// <returns>如果获取成功返回 true，否则返回 false</returns>
    public bool TryGetText(string key, out string text) => _texts.TryGetValue(key, out text);

    /// <summary>
    /// 从文件路径下加载多语言列表，并添加到
    /// </summary>
    /// <param name="path">文件路径</param>
    private void LoadTextListFromFile(string path)
    {
        // 创建定义程序集
        var ass = new DefAssembly(new RawAssembly { Targets = [new RawTarget { Name = "default", Manager = "Tables" }], }, "default", [], null, null);

        // 创建原始字段列表
        var rawFields = new List<RawField> { new() { Name = _keyFieldName, Type = "string" }, };

        // 如果需要转换多语言键为值，添加值字段
        if (ConvertTextKeyToValue)
        {
            rawFields.Add(new RawField { Name = _valueFieldName, Type = "string" });
        }

        // 创建多语言记录类型定义
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
            s_logger.Error($"路径:{path} 不存在！");
            return;
        }

        // 定义支持的 Excel 文件扩展名集合
        var excelExts = new HashSet<string> { "xlsx", "xls", "xlsm", "csv" };

        // 获取目录下的所有配置表文件
        var fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

        // 遍历目录下的所有多语言表文件，将各个多语言表的key与value添加到多语言字典中
        foreach (var fileInfo in fileInfos)
        {
            string fileName = Path.GetFileName(fileInfo.Name);            // 获取文件名
            string ext      = Path.GetExtension(fileName).TrimStart('.'); // 获取文件扩展名

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
                var value = ConvertTextKeyToValue ? ((DString)data.GetField(_valueFieldName)).Value : key;

                // 检查键是否为空
                if (string.IsNullOrEmpty(key))
                {
                    s_logger.Error("多语言表:{} key:{} 为空,已被丢弃.", fileName, key);
                    continue;
                }

                // 尝试添加到多语言字典
                if (!_texts.TryAdd(key, value))
                {
                    s_logger.Error("多语言表:{} key:{} 重复，请检查多语言表.", fileName, key);
                }
            }
        }
    }

    /// <summary>
    /// 添加未知键
    /// </summary>
    /// <param name="key">未知的多语言键</param>
    public void AddUnknownKey(string key) => _unknownTextKeys.Add(key);

    /// <summary>
    /// 处理数据
    /// </summary>
    public void ProcessDatas()
    {
        // 如果不需要转换多语言键为值，直接返回
        if (!ConvertTextKeyToValue) return;

        // 创建多语言键到值的转换器
        var trans = new TextKeyToValueTransformer(this);

        // 遍历所有表格
        foreach (var table in GenerationContext.Current.Tables)
        {
            // 遍历表格中的所有数据
            foreach (var record in GenerationContext.Current.GetTableAllDataList(table))
            {
                // 转换多语言值到数据中
                record.Data = (DBean)record.Data.Apply(trans, table.ValueTType);
            }
        }
    }
}
