#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Editor
{
	/// <summary>
	/// 对象池调试面板
	/// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 ObjectPoolModule。
	/// 功能：
	///     1. 展示所有对象池及其对象信息（名称、锁定、使用中、可销毁标记、优先级、闲置时长等）。
	///     2. 支持按池名/对象名过滤搜索、自动刷新、全部展开/折叠。
	///     3. 支持模块级/池级一键释放（释放全部未使用、释放超容量对象）。
	/// </summary>
	public sealed class ObjectPoolModuleWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/对象池调试面板", false, FuMenuPriority.DEBUG_PANEL_OBJECT_POOL)]
		public static void ShowWindow()
		{
			var window = GetWindow<ObjectPoolModuleWindow>("对象池调试");
			window.minSize = new Vector2(800, 800);

			// 初始位置居中显示
			const float width  = 1000f;
			const float height = 600f;
			var x = (Screen.currentResolution.width  - width)  / 2f;
			var y = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "ObjectPoolModule";

		#endregion

		#region 私有字段

		/// <summary>
		/// 对象池折叠状态缓存
		/// </summary>
		private readonly Dictionary<object, bool> m_foldoutStates = new();

		#endregion

		#region 反射缓存

		/// <summary>
		/// ObjectPoolModule 类型
		/// </summary>
		private Type m_objectPoolModuleType;

		/// <summary>
		/// ObjectPoolBase 类型
		/// </summary>
		private Type m_objectPoolBaseType;

		/// <summary>
		/// ObjectInfo 类型
		/// </summary>
		private Type m_objectInfoType;

		/// <summary>
		/// ObjectPoolModule 实例
		/// </summary>
		private object m_moduleInstance;

		/// <summary>
		/// ObjectPoolModule.Count 属性
		/// </summary>
		private PropertyInfo m_moduleCountProperty;

		/// <summary>
		/// ObjectPoolModule.GetAllObjectPools(bool) 方法
		/// </summary>
		private MethodInfo m_getAllObjectPoolsMethod;

		/// <summary>
		/// ObjectPoolModule.DisposeAllUnused 方法
		/// </summary>
		private MethodInfo m_moduleDisposeAllUnusedMethod;

		/// <summary>
		/// ObjectPoolModule.DisposeOverCapacity 方法
		/// </summary>
		private MethodInfo m_moduleDisposeOverCapacityMethod;

		/// <summary>
		/// 对象池名称属性
		/// </summary>
		private PropertyInfo m_poolNameProperty;

		/// <summary>
		/// 对象池对象类型属性
		/// </summary>
		private PropertyInfo m_poolObjectTypeProperty;

		/// <summary>
		/// 对象池数量属性
		/// </summary>
		private PropertyInfo m_poolCountProperty;

		/// <summary>
		/// 对象池可释放数量属性
		/// </summary>
		private PropertyInfo m_poolCanDisposeCountProperty;

		/// <summary>
		/// 对象池是否允许获取使用中对象属性
		/// </summary>
		private PropertyInfo m_poolAllowSpawnInUseProperty;

		/// <summary>
		/// 对象池自动销毁检查间隔属性
		/// </summary>
		private PropertyInfo m_poolAutoDisposeCheckIntervalProperty;

		/// <summary>
		/// 对象池容量属性
		/// </summary>
		private PropertyInfo m_poolCapacityProperty;

		/// <summary>
		/// 对象池过期时间属性
		/// </summary>
		private PropertyInfo m_poolExpireTimeAfterIdleProperty;

		/// <summary>
		/// 对象池优先级属性
		/// </summary>
		private PropertyInfo m_poolPriorityProperty;

		/// <summary>
		/// 对象池释放全部未使用对象方法
		/// </summary>
		private MethodInfo m_poolDisposeAllUnusedMethod;

		/// <summary>
		/// 对象池释放超容量对象方法
		/// </summary>
		private MethodInfo m_poolDisposeOverCapacityMethod;

		/// <summary>
		/// 对象池获取所有对象信息方法
		/// </summary>
		private MethodInfo m_poolGetAllObjectInfosMethod;

		/// <summary>
		/// 对象名称属性
		/// </summary>
		private PropertyInfo m_infoNameProperty;

		/// <summary>
		/// 对象目标真实对象属性
		/// </summary>
		private PropertyInfo m_infoTargetProperty;

		/// <summary>
		/// 对象是否锁定属性
		/// </summary>
		private PropertyInfo m_infoLockedProperty;

		/// <summary>
		/// 对象自定义可销毁标记属性
		/// </summary>
		private PropertyInfo m_infoCustomCanDisposeFlagProperty;

		/// <summary>
		/// 对象优先级属性
		/// </summary>
		private PropertyInfo m_infoPriorityProperty;

		/// <summary>
		/// 对象上次使用时间（单调时钟秒数）属性
		/// </summary>
		private PropertyInfo m_infoLastUseTimeProperty;

		/// <summary>
		/// 对象获取计数属性
		/// </summary>
		private PropertyInfo m_infoSpawnCountProperty;

		/// <summary>
		/// 对象是否使用中属性
		/// </summary>
		private PropertyInfo m_infoIsInUseProperty;

		#endregion

		#region 概览与主体绘制

		/// <summary>
		/// 绘制模块级概览与一键释放操作
		/// </summary>
		protected override void DrawOverview()
		{
			var count = m_moduleCountProperty?.GetValue(m_moduleInstance) ?? 0;
			EditorGUILayout.LabelField($"对象池总个数：{count}");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("释放所有池中的未使用对象", GUILayout.Width(200)))
			{
				try
				{
					m_moduleDisposeAllUnusedMethod?.Invoke(m_moduleInstance, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"释放所有池中的未使用对象失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			if (GUILayout.Button("释放所有池中超容量对象", GUILayout.Width(200)))
			{
				try
				{
					m_moduleDisposeOverCapacityMethod?.Invoke(m_moduleInstance, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"释放所有池中超容量对象失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制对象池列表主体（基类滚动容器内调用）
		/// </summary>
		protected override void DrawContent()
		{
			var pools = GetAllPools();
			if (pools == null || pools.Length == 0)
			{
				EditorGUILayout.HelpBox("对象池为空", MessageType.Info);
				return;
			}

			foreach (var pool in pools)
			{
				DrawObjectPool(pool);
			}
		}

		#endregion

		#region 对象池绘制

		/// <summary>
		/// 绘制单个对象池信息
		/// </summary>
		/// <param name="pool">对象池实例</param>
		private void DrawObjectPool(object pool)
		{
			if (pool == null) return;

			var poolName        = m_poolNameProperty?.GetValue(pool) as string ?? "<Unknown>";
			var poolCount       = (int)(m_poolCountProperty?.GetValue(pool) ?? 0);
			var poolUsingCount  = CountInUseObjects(pool);

			// 搜索过滤：池名或池内对象名匹配才展示
			if (!string.IsNullOrEmpty(m_searchFilter))
			{
				if (!poolName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase))
				{
					var objectInfos = GetPoolInfos(pool);
					var objectMatch = false;
					if (objectInfos != null)
					{
						foreach (var info in objectInfos)
						{
							if (info == null) continue;

							var infoName = m_infoNameProperty?.GetValue(info) as string;
							if (!string.IsNullOrEmpty(infoName) && infoName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase))
							{
								objectMatch = true;
								break;
							}
						}
					}

					if (!objectMatch) return;
				}
			}

			if (!m_foldoutStates.TryGetValue(pool, out var isOpen))
			{
				isOpen                = true;
				m_foldoutStates[pool] = true;
			}

			// 对象池名称（Foldout 标题）用青色高亮
			var foldoutOldColor = GUI.color;
			GUI.color             = Color.cyan;
			m_foldoutStates[pool] = EditorGUILayout.Foldout(isOpen, $"{poolName} ({poolUsingCount}/{poolCount})", true);
			GUI.color             = foldoutOldColor;
			if (!m_foldoutStates[pool]) return;

			EditorGUILayout.BeginVertical("box");
			{
				DrawPoolProperties(pool);
				EditorGUILayout.Separator();

				// 一次性收集，避免多次枚举 GetPoolInfos
				var objectInfos = GetPoolInfos(pool);
				var infoList    = new List<object>();
				if (objectInfos != null)
				{
					foreach (var info in objectInfos)
					{
						if (info == null) continue;

						infoList.Add(info);
					}
				}

				if (infoList.Count == 0)
				{
					var emptyOldColor = GUI.color;
					GUI.color = new Color(0.6f, 0.6f, 0.6f);
					GUILayout.Label("对象池中没有对象...", EditorStyles.miniLabel);
					GUI.color = emptyOldColor;
				}
				else
				{
					DrawObjectInfoHeader(pool);
					foreach (var info in infoList)
					{
						DrawObjectInfo(pool, info);
					}

					EditorGUILayout.Separator();
					DrawPoolActions(pool);
				}
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();
		}

		/// <summary>
		/// 绘制对象池配置属性（单行展示）
		/// </summary>
		/// <param name="pool">对象池实例</param>
		private void DrawPoolProperties(object pool)
		{
			var typeName      = (m_poolObjectTypeProperty?.GetValue(pool) as Type)?.Name ?? "Unknown";
			var allowInUse    = (bool)(m_poolAllowSpawnInUseProperty?.GetValue(pool)           ?? false);
			var autoDispose   = (float)(m_poolAutoDisposeCheckIntervalProperty?.GetValue(pool) ?? 0f);
			var capacity      = (int)(m_poolCapacityProperty?.GetValue(pool)                   ?? 0);
			var expireTime    = (float)(m_poolExpireTimeAfterIdleProperty?.GetValue(pool)      ?? 0f);
			var priority      = (int)(m_poolPriorityProperty?.GetValue(pool)                   ?? 0);
			var canDisposeCnt = (int)(m_poolCanDisposeCountProperty?.GetValue(pool)            ?? 0);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label($"对象类型: {typeName}", GUILayout.MinWidth(100));
			DrawColumnSeparator();
			GUILayout.Label($"容量: {capacity}", GUILayout.Width(100));
			DrawColumnSeparator();
			GUILayout.Label($"可释放数量: {canDisposeCnt}", GUILayout.Width(120));
			DrawColumnSeparator();
			GUILayout.Label($"闲置后过期时间: {FormatTime(expireTime)}", GUILayout.Width(120));
			DrawColumnSeparator();
			GUILayout.Label($"销毁检查间隔: {FormatTime(autoDispose)}", GUILayout.Width(120));
			DrawColumnSeparator();
			GUILayout.Label($"优先级: {priority}", GUILayout.Width(100));
			DrawColumnSeparator();
			GUILayout.Label(allowInUse ? "允许获取使用中的对象" : "禁止获取使用中的对象", GUILayout.Width(150));
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制对象信息表头
		/// </summary>
		/// <param name="pool">对象池实例</param>
		private void DrawObjectInfoHeader(object pool)
		{
			var allowInUse = (bool)(m_poolAllowSpawnInUseProperty?.GetValue(pool) ?? false);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("名称",                      GUILayout.Width(160));
			GUILayout.Label("目标",                      GUILayout.Width(160));
			GUILayout.Label("锁定",                      GUILayout.Width(50));
			GUILayout.Label(allowInUse ? "计数" : "使用中", GUILayout.Width(60));
			GUILayout.Label("可销毁",                     GUILayout.Width(60));
			GUILayout.Label("优先级",                     GUILayout.Width(60));
			GUILayout.Label("闲置时长",                    GUILayout.Width(160));
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制单个对象信息
		/// </summary>
		/// <param name="pool">对象池实例</param>
		/// <param name="info">对象信息</param>
		private void DrawObjectInfo(object pool, object info)
		{
			if (info == null) return;

			var allowInUse  = (bool)(m_poolAllowSpawnInUseProperty?.GetValue(pool) ?? false);
			var objName     = m_infoNameProperty?.GetValue(info) as string;
			var target      = m_infoTargetProperty?.GetValue(info);
			var locked      = (bool)(m_infoLockedProperty?.GetValue(info)               ?? false);
			var canDispose  = (bool)(m_infoCustomCanDisposeFlagProperty?.GetValue(info) ?? false);
			var priority    = (int)(m_infoPriorityProperty?.GetValue(info)              ?? 0);
			var lastUseTime = (double)(m_infoLastUseTimeProperty?.GetValue(info)        ?? 0d);
			var spawnCount  = (int)(m_infoSpawnCountProperty?.GetValue(info)            ?? 0);
			var isInUse     = (bool)(m_infoIsInUseProperty?.GetValue(info)              ?? false);

			// 可释放对象（未使用 + 未加锁 + 允许销毁，与 GetCanDisposeObjects 一致）整行偏灰；锁定红、使用中绿优先级更高
			var oldColor     = GUI.color;
			var isDisposable = !isInUse && !locked && canDispose;
			var baseColor    = isDisposable ? new Color(0.65f, 0.65f, 0.65f) : oldColor;

			EditorGUILayout.BeginHorizontal();
			GUI.color = baseColor;
			GUILayout.Label(string.IsNullOrEmpty(objName) ? "<None>" : objName, GUILayout.Width(160));

			GUI.color = baseColor;
			GUILayout.Label(target == null ? "-" : target.ToString(), GUILayout.Width(160));

			GUI.color = locked ? Color.red : baseColor;
			GUILayout.Label(locked ? "是" : "否", GUILayout.Width(50));

			GUI.color = isInUse ? Color.green : baseColor;
			GUILayout.Label(allowInUse ? spawnCount.ToString() : (isInUse ? "是" : "否"), GUILayout.Width(60));

			// "可销毁"列：显示当前是否真的可销毁（未使用 + 未加锁 + 允许销毁），与 DisposeObjectInternal 判定一致。
			// 不能用 CustomCanDisposeFlag（恒为 true 的静态标记），否则使用中对象会误显示"可销毁=是"。
			GUI.color = baseColor;
			GUILayout.Label(isDisposable ? "是" : "否", GUILayout.Width(60));

			GUI.color = baseColor;
			GUILayout.Label(priority.ToString(), GUILayout.Width(60));
			// LastUseTime 是单调时钟秒数（Time.unscaledTimeAsDouble），不是墙钟时刻，无法还原成日期，
			// 故展示“距上次使用已过多少秒”——这正是过期判定的输入，比原始数值可读。
			GUILayout.Label(lastUseTime == default ? "-" : $"{Time.unscaledTimeAsDouble - lastUseTime:0.##}s", GUILayout.Width(160));
			GUI.color = oldColor;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制对象池级操作按钮
		/// </summary>
		/// <param name="pool">对象池实例</param>
		private void DrawPoolActions(object pool)
		{
			var poolName = m_poolNameProperty?.GetValue(pool) as string ?? "<Unknown>";

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("释放未使用对象", GUILayout.Width(160)))
			{
				try
				{
					m_poolDisposeAllUnusedMethod?.Invoke(pool, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"对象池 {poolName} 释放未使用对象失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			if (GUILayout.Button("释放超容量对象", GUILayout.Width(160)))
			{
				try
				{
					m_poolDisposeOverCapacityMethod?.Invoke(pool, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"对象池 {poolName} 释放超容量对象失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region 折叠操作

		/// <summary>
		/// 全部展开
		/// </summary>
		protected override void ExpandAll()
		{
			var pools = GetAllPools();
			if (pools == null) return;
			SetAllFoldouts(pools, m_foldoutStates, true);
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			var pools = GetAllPools();
			if (pools == null) return;
			SetAllFoldouts(pools, m_foldoutStates, false);
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_moduleInstance != null) return true;

			m_objectPoolModuleType = HotfixReflection.ObjectPoolModule;
			if (m_objectPoolModuleType == null) return false;

			m_objectPoolBaseType = HotfixReflection.ObjectPoolBase;
			if (m_objectPoolBaseType == null) return false;

			m_objectInfoType = HotfixReflection.ObjectInfo;
			if (m_objectInfoType == null) return false;

			// ObjectPoolModule 没有静态 Instance，通过 ModuleManager.GetModule<T>() 泛型方法获取热更实例
			m_moduleInstance = HotfixReflection.GetModuleInstance(m_objectPoolModuleType);
			if (m_moduleInstance == null) return false;

			// ObjectPoolModule 成员
			m_moduleCountProperty             = m_objectPoolModuleType.GetProperty("Count", BindingFlags.Public             | BindingFlags.Instance);
			m_getAllObjectPoolsMethod         = m_objectPoolModuleType.GetMethod("GetAllObjectPools",   BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
			m_moduleDisposeAllUnusedMethod    = m_objectPoolModuleType.GetMethod("DisposeAllUnused",    BindingFlags.Public | BindingFlags.Instance);
			m_moduleDisposeOverCapacityMethod = m_objectPoolModuleType.GetMethod("DisposeOverCapacity", BindingFlags.Public | BindingFlags.Instance);

			// ObjectPoolBase 成员
			m_poolNameProperty                     = m_objectPoolBaseType.GetProperty("Name",                     BindingFlags.Public | BindingFlags.Instance);
			m_poolObjectTypeProperty               = m_objectPoolBaseType.GetProperty("ObjectType",               BindingFlags.Public | BindingFlags.Instance);
			m_poolCountProperty                    = m_objectPoolBaseType.GetProperty("Count",                    BindingFlags.Public | BindingFlags.Instance);
			m_poolCanDisposeCountProperty          = m_objectPoolBaseType.GetProperty("CanDisposeCount",          BindingFlags.Public | BindingFlags.Instance);
			m_poolAllowSpawnInUseProperty          = m_objectPoolBaseType.GetProperty("AllowSpawnInUse",          BindingFlags.Public | BindingFlags.Instance);
			m_poolAutoDisposeCheckIntervalProperty = m_objectPoolBaseType.GetProperty("AutoDisposeCheckInterval", BindingFlags.Public | BindingFlags.Instance);
			m_poolCapacityProperty                 = m_objectPoolBaseType.GetProperty("Capacity",                 BindingFlags.Public | BindingFlags.Instance);
			m_poolExpireTimeAfterIdleProperty      = m_objectPoolBaseType.GetProperty("ExpireTimeAfterIdle",     BindingFlags.Public | BindingFlags.Instance);
			m_poolPriorityProperty                 = m_objectPoolBaseType.GetProperty("Priority",                 BindingFlags.Public | BindingFlags.Instance);
			m_poolDisposeAllUnusedMethod           = m_objectPoolBaseType.GetMethod("DisposeAllUnused",    BindingFlags.Public        | BindingFlags.Instance);
			m_poolDisposeOverCapacityMethod        = m_objectPoolBaseType.GetMethod("DisposeOverCapacity", BindingFlags.Public        | BindingFlags.Instance);
			m_poolGetAllObjectInfosMethod          = m_objectPoolBaseType.GetMethod("GetAllObjectInfos",   BindingFlags.Public        | BindingFlags.Instance);

			// ObjectInfo 成员
			m_infoNameProperty                 = m_objectInfoType.GetProperty("Name",                 BindingFlags.Public | BindingFlags.Instance);
			m_infoTargetProperty               = m_objectInfoType.GetProperty("Target",               BindingFlags.Public | BindingFlags.Instance);
			m_infoLockedProperty               = m_objectInfoType.GetProperty("Locked",               BindingFlags.Public | BindingFlags.Instance);
			m_infoCustomCanDisposeFlagProperty = m_objectInfoType.GetProperty("CustomCanDisposeFlag", BindingFlags.Public | BindingFlags.Instance);
			m_infoPriorityProperty             = m_objectInfoType.GetProperty("Priority",             BindingFlags.Public | BindingFlags.Instance);
			m_infoLastUseTimeProperty          = m_objectInfoType.GetProperty("LastUseTime",          BindingFlags.Public | BindingFlags.Instance);
			m_infoSpawnCountProperty           = m_objectInfoType.GetProperty("SpawnCount",           BindingFlags.Public | BindingFlags.Instance);
			m_infoIsInUseProperty              = m_objectInfoType.GetProperty("IsInUse",              BindingFlags.Public | BindingFlags.Instance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_objectPoolModuleType                 = null;
			m_objectPoolBaseType                   = null;
			m_objectInfoType                       = null;
			m_moduleInstance                       = null;
			m_moduleCountProperty                  = null;
			m_getAllObjectPoolsMethod              = null;
			m_moduleDisposeAllUnusedMethod         = null;
			m_moduleDisposeOverCapacityMethod      = null;
			m_poolNameProperty                     = null;
			m_poolObjectTypeProperty               = null;
			m_poolCountProperty                    = null;
			m_poolCanDisposeCountProperty          = null;
			m_poolAllowSpawnInUseProperty          = null;
			m_poolAutoDisposeCheckIntervalProperty = null;
			m_poolCapacityProperty                 = null;
			m_poolExpireTimeAfterIdleProperty      = null;
			m_poolPriorityProperty                 = null;
			m_poolDisposeAllUnusedMethod           = null;
			m_poolDisposeOverCapacityMethod        = null;
			m_poolGetAllObjectInfosMethod          = null;
			m_infoNameProperty                     = null;
			m_infoTargetProperty                   = null;
			m_infoLockedProperty                   = null;
			m_infoCustomCanDisposeFlagProperty     = null;
			m_infoPriorityProperty                 = null;
			m_infoLastUseTimeProperty              = null;
			m_infoSpawnCountProperty               = null;
			m_infoIsInUseProperty                  = null;
		}

		/// <summary>
		/// 获取所有对象池（按优先级排序）
		/// </summary>
		/// <returns>对象池数组，获取失败时返回 null</returns>
		private object[] GetAllPools()
		{
			var result = m_getAllObjectPoolsMethod?.Invoke(m_moduleInstance, new object[] { true });
			return result as object[];
		}

		/// <summary>
		/// 获取对象池中的所有对象信息。
		/// ObjectInfo 是值类型 struct，数组不能协变转换为 object[]，故以 IEnumerable 返回逐元素装箱。
		/// </summary>
		/// <param name="pool">对象池实例</param>
		/// <returns>对象信息枚举，无对象或获取失败时返回 null</returns>
		private IEnumerable GetPoolInfos(object pool)
		{
			var result = m_poolGetAllObjectInfosMethod?.Invoke(pool, null);
			return result as IEnumerable;
		}

		/// <summary>
		/// 统计对象池中正在使用（IsInUse）的对象数量
		/// </summary>
		/// <param name="pool">对象池实例</param>
		/// <returns>正在使用的对象数量</returns>
		private int CountInUseObjects(object pool)
		{
			var count = 0;
			var infos = GetPoolInfos(pool);
			if (infos == null) return 0;

			foreach (var info in infos)
			{
				if (info == null) continue;

				if (m_infoIsInUseProperty?.GetValue(info) is bool isInUse && isInUse)
					count++;
			}

			return count;
		}

		/// <summary>
		/// 格式化时间值。float.MaxValue 表示未启用
		/// </summary>
		/// <param name="value">秒数</param>
		/// <returns>格式化后的字符串</returns>
		private static string FormatTime(float value)
		{
			return value >= float.MaxValue ? "永不" : $"{value:0.##}s";
		}

		#endregion
	}
}
#endif