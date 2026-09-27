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
		private readonly Dictionary<object, bool> m_FoldoutStates = new();

		#endregion

		#region 反射缓存

		/// <summary>
		/// ObjectPoolModule 类型
		/// </summary>
		private Type m_ObjectPoolModuleType;

		/// <summary>
		/// ObjectPoolBase 类型
		/// </summary>
		private Type m_ObjectPoolBaseType;

		/// <summary>
		/// ObjectInfo 类型
		/// </summary>
		private Type m_ObjectInfoType;

		/// <summary>
		/// ObjectPoolModule 实例
		/// </summary>
		private object m_ModuleInstance;

		/// <summary>
		/// ObjectPoolModule.Count 属性
		/// </summary>
		private PropertyInfo m_ModuleCountProperty;

		/// <summary>
		/// ObjectPoolModule.GetAllObjectPools(bool) 方法
		/// </summary>
		private MethodInfo m_GetAllObjectPoolsMethod;

		/// <summary>
		/// ObjectPoolModule.DisposeAllUnused 方法
		/// </summary>
		private MethodInfo m_ModuleDisposeAllUnusedMethod;

		/// <summary>
		/// ObjectPoolModule.DisposeOverCapacity 方法
		/// </summary>
		private MethodInfo m_ModuleDisposeOverCapacityMethod;

		/// <summary>
		/// 对象池名称属性
		/// </summary>
		private PropertyInfo m_PoolNameProperty;

		/// <summary>
		/// 对象池对象类型属性
		/// </summary>
		private PropertyInfo m_PoolObjectTypeProperty;

		/// <summary>
		/// 对象池数量属性
		/// </summary>
		private PropertyInfo m_PoolCountProperty;

		/// <summary>
		/// 对象池可释放数量属性
		/// </summary>
		private PropertyInfo m_PoolCanDisposeCountProperty;

		/// <summary>
		/// 对象池是否允许获取使用中对象属性
		/// </summary>
		private PropertyInfo m_PoolAllowSpawnInUseProperty;

		/// <summary>
		/// 对象池自动销毁检查间隔属性
		/// </summary>
		private PropertyInfo m_PoolAutoDisposeCheckIntervalProperty;

		/// <summary>
		/// 对象池容量属性
		/// </summary>
		private PropertyInfo m_PoolCapacityProperty;

		/// <summary>
		/// 对象池过期时间属性
		/// </summary>
		private PropertyInfo m_PoolExpireTimeAfterIdleProperty;

		/// <summary>
		/// 对象池优先级属性
		/// </summary>
		private PropertyInfo m_PoolPriorityProperty;

		/// <summary>
		/// 对象池释放全部未使用对象方法
		/// </summary>
		private MethodInfo m_PoolDisposeAllUnusedMethod;

		/// <summary>
		/// 对象池释放超容量对象方法
		/// </summary>
		private MethodInfo m_PoolDisposeOverCapacityMethod;

		/// <summary>
		/// 对象池获取所有对象信息方法
		/// </summary>
		private MethodInfo m_PoolGetAllObjectInfosMethod;

		/// <summary>
		/// 对象名称属性
		/// </summary>
		private PropertyInfo m_InfoNameProperty;

		/// <summary>
		/// 对象目标真实对象属性
		/// </summary>
		private PropertyInfo m_InfoTargetProperty;

		/// <summary>
		/// 对象是否锁定属性
		/// </summary>
		private PropertyInfo m_InfoLockedProperty;

		/// <summary>
		/// 对象自定义可销毁标记属性
		/// </summary>
		private PropertyInfo m_InfoCustomCanDisposeFlagProperty;

		/// <summary>
		/// 对象优先级属性
		/// </summary>
		private PropertyInfo m_InfoPriorityProperty;

		/// <summary>
		/// 对象上次使用时间（单调时钟秒数）属性
		/// </summary>
		private PropertyInfo m_InfoLastUseTimeProperty;

		/// <summary>
		/// 对象获取计数属性
		/// </summary>
		private PropertyInfo m_InfoSpawnCountProperty;

		/// <summary>
		/// 对象是否使用中属性
		/// </summary>
		private PropertyInfo m_InfoIsInUseProperty;

		#endregion

		#region 概览与主体绘制

		/// <summary>
		/// 绘制模块级概览与一键释放操作
		/// </summary>
		protected override void DrawOverview()
		{
			var count = m_ModuleCountProperty?.GetValue(m_ModuleInstance) ?? 0;
			EditorGUILayout.LabelField($"对象池总个数：{count}");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("释放所有池中的未使用对象", GUILayout.Width(200)))
			{
				try
				{
					m_ModuleDisposeAllUnusedMethod?.Invoke(m_ModuleInstance, null);
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
					m_ModuleDisposeOverCapacityMethod?.Invoke(m_ModuleInstance, null);
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

			var poolName        = m_PoolNameProperty?.GetValue(pool) as string ?? "<Unknown>";
			var poolCount       = (int)(m_PoolCountProperty?.GetValue(pool) ?? 0);
			var poolUsingCount  = CountInUseObjects(pool);

			// 搜索过滤：池名或池内对象名匹配才展示
			if (!string.IsNullOrEmpty(m_SearchFilter))
			{
				if (!poolName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
				{
					var objectInfos = GetPoolInfos(pool);
					var objectMatch = false;
					if (objectInfos != null)
					{
						foreach (var info in objectInfos)
						{
							if (info == null) continue;

							var infoName = m_InfoNameProperty?.GetValue(info) as string;
							if (!string.IsNullOrEmpty(infoName) && infoName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
							{
								objectMatch = true;
								break;
							}
						}
					}

					if (!objectMatch) return;
				}
			}

			if (!m_FoldoutStates.TryGetValue(pool, out var isOpen))
			{
				isOpen                = true;
				m_FoldoutStates[pool] = true;
			}

			// 对象池名称（Foldout 标题）用青色高亮
			var foldoutOldColor = GUI.color;
			GUI.color             = Color.cyan;
			m_FoldoutStates[pool] = EditorGUILayout.Foldout(isOpen, $"{poolName} ({poolUsingCount}/{poolCount})", true);
			GUI.color             = foldoutOldColor;
			if (!m_FoldoutStates[pool]) return;

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
			var typeName      = (m_PoolObjectTypeProperty?.GetValue(pool) as Type)?.Name ?? "Unknown";
			var allowInUse    = (bool)(m_PoolAllowSpawnInUseProperty?.GetValue(pool)           ?? false);
			var autoDispose   = (float)(m_PoolAutoDisposeCheckIntervalProperty?.GetValue(pool) ?? 0f);
			var capacity      = (int)(m_PoolCapacityProperty?.GetValue(pool)                   ?? 0);
			var expireTime    = (float)(m_PoolExpireTimeAfterIdleProperty?.GetValue(pool)      ?? 0f);
			var priority      = (int)(m_PoolPriorityProperty?.GetValue(pool)                   ?? 0);
			var canDisposeCnt = (int)(m_PoolCanDisposeCountProperty?.GetValue(pool)            ?? 0);

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
			var allowInUse = (bool)(m_PoolAllowSpawnInUseProperty?.GetValue(pool) ?? false);

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

			var allowInUse  = (bool)(m_PoolAllowSpawnInUseProperty?.GetValue(pool) ?? false);
			var objName     = m_InfoNameProperty?.GetValue(info) as string;
			var target      = m_InfoTargetProperty?.GetValue(info);
			var locked      = (bool)(m_InfoLockedProperty?.GetValue(info)               ?? false);
			var canDispose  = (bool)(m_InfoCustomCanDisposeFlagProperty?.GetValue(info) ?? false);
			var priority    = (int)(m_InfoPriorityProperty?.GetValue(info)              ?? 0);
			var lastUseTime = (double)(m_InfoLastUseTimeProperty?.GetValue(info)        ?? 0d);
			var spawnCount  = (int)(m_InfoSpawnCountProperty?.GetValue(info)            ?? 0);
			var isInUse     = (bool)(m_InfoIsInUseProperty?.GetValue(info)              ?? false);

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
			var poolName = m_PoolNameProperty?.GetValue(pool) as string ?? "<Unknown>";

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("释放未使用对象", GUILayout.Width(160)))
			{
				try
				{
					m_PoolDisposeAllUnusedMethod?.Invoke(pool, null);
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
					m_PoolDisposeOverCapacityMethod?.Invoke(pool, null);
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
			SetAllFoldouts(pools, m_FoldoutStates, true);
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			var pools = GetAllPools();
			if (pools == null) return;
			SetAllFoldouts(pools, m_FoldoutStates, false);
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_ModuleInstance != null) return true;

			m_ObjectPoolModuleType = HotfixReflection.ObjectPoolModule;
			if (m_ObjectPoolModuleType == null) return false;

			m_ObjectPoolBaseType = HotfixReflection.ObjectPoolBase;
			if (m_ObjectPoolBaseType == null) return false;

			m_ObjectInfoType = HotfixReflection.ObjectInfo;
			if (m_ObjectInfoType == null) return false;

			// ObjectPoolModule 没有静态 Instance，通过 ModuleManager.GetModule<T>() 泛型方法获取热更实例
			m_ModuleInstance = HotfixReflection.GetModuleInstance(m_ObjectPoolModuleType);
			if (m_ModuleInstance == null) return false;

			// ObjectPoolModule 成员
			m_ModuleCountProperty             = m_ObjectPoolModuleType.GetProperty("Count", BindingFlags.Public             | BindingFlags.Instance);
			m_GetAllObjectPoolsMethod         = m_ObjectPoolModuleType.GetMethod("GetAllObjectPools",   BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
			m_ModuleDisposeAllUnusedMethod    = m_ObjectPoolModuleType.GetMethod("DisposeAllUnused",    BindingFlags.Public | BindingFlags.Instance);
			m_ModuleDisposeOverCapacityMethod = m_ObjectPoolModuleType.GetMethod("DisposeOverCapacity", BindingFlags.Public | BindingFlags.Instance);

			// ObjectPoolBase 成员
			m_PoolNameProperty                     = m_ObjectPoolBaseType.GetProperty("Name",                     BindingFlags.Public | BindingFlags.Instance);
			m_PoolObjectTypeProperty               = m_ObjectPoolBaseType.GetProperty("ObjectType",               BindingFlags.Public | BindingFlags.Instance);
			m_PoolCountProperty                    = m_ObjectPoolBaseType.GetProperty("Count",                    BindingFlags.Public | BindingFlags.Instance);
			m_PoolCanDisposeCountProperty          = m_ObjectPoolBaseType.GetProperty("CanDisposeCount",          BindingFlags.Public | BindingFlags.Instance);
			m_PoolAllowSpawnInUseProperty          = m_ObjectPoolBaseType.GetProperty("AllowSpawnInUse",          BindingFlags.Public | BindingFlags.Instance);
			m_PoolAutoDisposeCheckIntervalProperty = m_ObjectPoolBaseType.GetProperty("AutoDisposeCheckInterval", BindingFlags.Public | BindingFlags.Instance);
			m_PoolCapacityProperty                 = m_ObjectPoolBaseType.GetProperty("Capacity",                 BindingFlags.Public | BindingFlags.Instance);
			m_PoolExpireTimeAfterIdleProperty      = m_ObjectPoolBaseType.GetProperty("ExpireTimeAfterIdle",     BindingFlags.Public | BindingFlags.Instance);
			m_PoolPriorityProperty                 = m_ObjectPoolBaseType.GetProperty("Priority",                 BindingFlags.Public | BindingFlags.Instance);
			m_PoolDisposeAllUnusedMethod           = m_ObjectPoolBaseType.GetMethod("DisposeAllUnused",    BindingFlags.Public        | BindingFlags.Instance);
			m_PoolDisposeOverCapacityMethod        = m_ObjectPoolBaseType.GetMethod("DisposeOverCapacity", BindingFlags.Public        | BindingFlags.Instance);
			m_PoolGetAllObjectInfosMethod          = m_ObjectPoolBaseType.GetMethod("GetAllObjectInfos",   BindingFlags.Public        | BindingFlags.Instance);

			// ObjectInfo 成员
			m_InfoNameProperty                 = m_ObjectInfoType.GetProperty("Name",                 BindingFlags.Public | BindingFlags.Instance);
			m_InfoTargetProperty               = m_ObjectInfoType.GetProperty("Target",               BindingFlags.Public | BindingFlags.Instance);
			m_InfoLockedProperty               = m_ObjectInfoType.GetProperty("Locked",               BindingFlags.Public | BindingFlags.Instance);
			m_InfoCustomCanDisposeFlagProperty = m_ObjectInfoType.GetProperty("CustomCanDisposeFlag", BindingFlags.Public | BindingFlags.Instance);
			m_InfoPriorityProperty             = m_ObjectInfoType.GetProperty("Priority",             BindingFlags.Public | BindingFlags.Instance);
			m_InfoLastUseTimeProperty          = m_ObjectInfoType.GetProperty("LastUseTime",          BindingFlags.Public | BindingFlags.Instance);
			m_InfoSpawnCountProperty           = m_ObjectInfoType.GetProperty("SpawnCount",           BindingFlags.Public | BindingFlags.Instance);
			m_InfoIsInUseProperty              = m_ObjectInfoType.GetProperty("IsInUse",              BindingFlags.Public | BindingFlags.Instance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_ObjectPoolModuleType                 = null;
			m_ObjectPoolBaseType                   = null;
			m_ObjectInfoType                       = null;
			m_ModuleInstance                       = null;
			m_ModuleCountProperty                  = null;
			m_GetAllObjectPoolsMethod              = null;
			m_ModuleDisposeAllUnusedMethod         = null;
			m_ModuleDisposeOverCapacityMethod      = null;
			m_PoolNameProperty                     = null;
			m_PoolObjectTypeProperty               = null;
			m_PoolCountProperty                    = null;
			m_PoolCanDisposeCountProperty          = null;
			m_PoolAllowSpawnInUseProperty          = null;
			m_PoolAutoDisposeCheckIntervalProperty = null;
			m_PoolCapacityProperty                 = null;
			m_PoolExpireTimeAfterIdleProperty      = null;
			m_PoolPriorityProperty                 = null;
			m_PoolDisposeAllUnusedMethod           = null;
			m_PoolDisposeOverCapacityMethod        = null;
			m_PoolGetAllObjectInfosMethod          = null;
			m_InfoNameProperty                     = null;
			m_InfoTargetProperty                   = null;
			m_InfoLockedProperty                   = null;
			m_InfoCustomCanDisposeFlagProperty     = null;
			m_InfoPriorityProperty                 = null;
			m_InfoLastUseTimeProperty              = null;
			m_InfoSpawnCountProperty               = null;
			m_InfoIsInUseProperty                  = null;
		}

		/// <summary>
		/// 获取所有对象池（按优先级排序）
		/// </summary>
		/// <returns>对象池数组，获取失败时返回 null</returns>
		private object[] GetAllPools()
		{
			var result = m_GetAllObjectPoolsMethod?.Invoke(m_ModuleInstance, new object[] { true });
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
			var result = m_PoolGetAllObjectInfosMethod?.Invoke(pool, null);
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

				if (m_InfoIsInUseProperty?.GetValue(info) is bool isInUse && isInUse)
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