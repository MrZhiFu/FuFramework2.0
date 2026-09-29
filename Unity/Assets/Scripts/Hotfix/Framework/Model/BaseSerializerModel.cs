using Newtonsoft.Json;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Storage;

namespace Hotfix.Framework.Model
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
		private string m_fileName;

		/// <summary>
		/// 本地存储管理器。
		/// </summary>
		private StorageModule m_storageModule;

		/// <summary>
		/// 加载是否失败。失败（存档损坏/反序列化异常）后禁止回写：
		/// 此时对象内为「半残值」，OnDispose 无条件的全量保存会覆写掉磁盘上尚可修复的原始存档。
		/// </summary>
		private bool m_loadFailed;

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
			m_fileName     = GetFileName();
			m_storageModule = StorageModule.Instance;

			if (m_storageModule == null)
			{
				FuLogger.LogError($"初始化Model-{m_fileName}时，数据保存管理器未找到!");
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
				var dataJson = m_storageModule.GetString(m_fileName, m_fileName);
				if (string.IsNullOrEmpty(dataJson))
				{
					OnFirstInitDate();
					return;
				}

				// JSON 字符串中的数据填充到自身对象中
				JsonConvert.PopulateObject(dataJson, this);
				FuLogger.LogInfo($"Model数据加载成功: {m_fileName}");
			}
			catch (System.Exception ex)
			{
				// 置失败标记：本次对象内容不可信（可能只填充了部分字段），禁止后续保存覆写磁盘原始存档
				m_loadFailed = true;
				FuLogger.LogError($"读取Model数据{m_fileName}出错：{ex.Message}（已跳过回写，避免覆盖磁盘原始存档）");
			}
		}

		/// <summary>
		/// 保存自身数据到本地
		/// </summary>
		private void Save()
		{
			if (m_storageModule == null)
			{
				FuLogger.LogWarning($"无法保存{m_fileName}，数据保存管理器未找到!");
				return;
			}

			// 加载失败时对象内为半残值：回写会用残缺数据覆盖磁盘原始存档，导致坏档不可恢复，故整体跳过保存
			if (m_loadFailed)
			{
				FuLogger.LogWarning($"无法保存{m_fileName}，数据加载失败（存档可能损坏），为避免覆盖原始存档已跳过保存!");
				return;
			}

			try
			{
				var dataJson = JsonConvert.SerializeObject(this, Formatting.None);
				m_storageModule.SetString(m_fileName, dataJson, m_fileName);
				m_storageModule.Save(m_fileName);
				FuLogger.LogInfo($"Model数据保存成功: {m_fileName}");
			}
			catch (System.Exception ex)
			{
				FuLogger.LogError($"存储Model数据{m_fileName}出错：{ex.Message}");
			}
		}

		/// <summary>
		/// 首次初始化(在数据文件不存在时调用）
		/// </summary>
		protected virtual void OnFirstInitDate()
		{
			FuLogger.LogInfo($"首次初始化Model: {m_fileName}");
		}
	}
}