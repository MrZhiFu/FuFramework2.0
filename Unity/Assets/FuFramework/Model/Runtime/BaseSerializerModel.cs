using Newtonsoft.Json;
using FuFramework.Core.Runtime;
using FuFramework.SaveData.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Model.Runtime
{
    /// <summary>
    /// 可序列化的Model基类(数据字段存储在本地的Model)。
    /// 功能：
    ///     1. 配合数据存储模块，提供Model数据的序列化，反序列化功能。
    /// 
    /// 序列化规则说明：
    /// 1.默认情况下，以下元素会被JSON序列化保存：
    ///     ✅ 公共属性 (public properties with getter and setter)
    ///     ✅ 公共字段 (public fields)
    /// 
    /// 2.以下元素默认不会被保存：
    ///     ❌ 私有/受保护成员 (private/protected members)
    ///     ❌ 只读属性 (read-only properties)
    ///     ❌ 计算方法/表达式体属性 (computed properties)
    ///     ❌ 方法、事件、委托 (methods, events, delegates)
    ///     ❌ Unity组件引用 (Unity object references)
    /// 
    /// 使用特性精确控制序列化：
    ///     1. 使用 [JsonIgnore] 忽略公共属性：
    /// <code>
    /// [JsonIgnore]
    /// public string TemporaryData { get; set; }  // 不会被保存
    /// </code>
    /// 
    ///     2. 使用 [JsonProperty] 强制序列化私有成员：
    /// <code>
    /// [JsonProperty]
    /// private string secretCode; // 会被保存
    /// </code>
    /// </summary>
    public abstract class BaseSerializerModel : BaseModel
    {
        /// <summary>
        /// 本地存储的文件名(默认为类名)。
        /// </summary>
        private string m_FileName;

        /// <summary>
        /// 本地存储管理器。
        /// </summary>
        private StorageModule _storageModule;

        /// <summary>
        /// 获取存储的文件名（可重写以自定义）
        /// </summary>
        protected virtual string GetFileName() => GetType().Name;

        /// <summary>
        /// 初始化
        /// </summary>
        protected sealed override void OnInitData()
        {
            base.OnInitData();
            m_FileName       = GetFileName();
            _storageModule = ModuleManager.GetModule<StorageModule>();

            if (!_storageModule)
            {
                FuLogger.LogError($"初始化Model-{m_FileName}时，数据保存管理器未找到!");
                return;
            }

            Load();
        }

        /// <summary>
        /// 释放(自动保存数据)
        /// </summary>
        protected override void OnDispose()
        {
            Save();
            base.OnDispose();
        }

        /// <summary>
        /// 加载数据到自身对象
        /// </summary>
        private void Load()
        {
            try
            {
                var dataJson = _storageModule.GetString(m_FileName, m_FileName);
                if (string.IsNullOrEmpty(dataJson))
                {
                    OnFirstInitDate();
                    return;
                }

                // JSON 字符串中的数据填充到自身对象中
                JsonConvert.PopulateObject(dataJson, this);
                FuLogger.LogInfo($"Model数据加载成功: {m_FileName}");
            }
            catch (System.Exception ex)
            {
                FuLogger.LogError($"读取Model数据{m_FileName}出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存自身数据到本地
        /// </summary>
        private void Save()
        {
            if (!_storageModule)
            {
                FuLogger.LogWarning($"无法保存{m_FileName}，数据保存管理器未找到!");
                return;
            }

            try
            {
                var dataJson = JsonConvert.SerializeObject(this, Formatting.None);
                _storageModule.SetString(m_FileName, dataJson, m_FileName);
                _storageModule.Save(m_FileName);
                FuLogger.LogInfo($"Model数据保存成功: {m_FileName}");
            }
            catch (System.Exception ex)
            {
                FuLogger.LogError($"存储Model数据{m_FileName}出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 首次初始化(在数据文件不存在时调用）
        /// </summary>
        protected virtual void OnFirstInitDate()
        {
            FuLogger.LogInfo($"首次初始化Model: {m_FileName}");
        }
    }
}