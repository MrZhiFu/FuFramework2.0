#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Editor
{
	/// <summary>
	/// 事件模块调试面板。
	/// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 EventModule（经 ModuleManager.GetModule 取热更实例）。
	/// 功能：
	///     1. 模块总览：待分发事件数、订阅事件 ID 数、handler 总数。
	///     2. 订阅明细：按事件 ID 分组折叠，展示每个 handler 的方法名/所属类型/静态或实例标记，支持跳转源码。
	///     3. 待分发队列：展示队列中每条事件的 eventId/sender/参数类型，支持一键清空。
	///     4. 搜索过滤（事件 ID 或 handler 所属类型）、自动刷新、全部展开/折叠。
	/// 数据源全部为 EventModule 公共 API（EventCount/EventHandlerCount/ForEachHandler/ForEachEvent），
	/// 仅「清空待分发队列」经反射取 EventModule 私有字段 m_eventPool 调用池的 Clear（EventModule 未转发该方法）。
	/// 回调绑定：ForEachHandler/ForEachEvent 的回调委托签名为 (string, EventHandler&lt;GameEventArgs&gt;) 与 (object, GameEventArgs)，
	/// 经窗口内泛型适配方法 MakeGenericMethod + CreateDelegate 精确签名绑定，无需动态发射。
	/// </summary>
	public sealed class EventModuleWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/事件调试面板", false, FuMenuPriority.DEBUG_PANEL_EVENT)]
		public static void ShowWindow()
		{
			var window = GetWindow<EventModuleWindow>("事件调试");
			window.minSize = new Vector2(760, 600);

			// 初始位置居中显示
			const float width  = 900f;
			const float height = 640f;
			var x = (Screen.currentResolution.width  - width)  / 2f;
			var y = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "EventModule";

		/// <summary>
		/// 搜索框宽度（事件 ID / handler 方法名 / 所属类型全名的过滤串较长，用加宽输入框）
		/// </summary>
		protected override int SearchFieldWidth => 180;

		#endregion

		#region 显示行数据结构

		/// <summary>
		/// 订阅 handler 显示行
		/// </summary>
		private sealed class HandlerRow
		{
			/// <summary>
			/// handler 方法元数据（跳转源码用；仅 Play 会话内有效，快照重建时刷新）
			/// </summary>
			public MethodInfo Method;

			/// <summary>
			/// handler 方法名（含泛型反引号时保持原样）
			/// </summary>
			public string MethodName;

			/// <summary>
			/// 方法所属类型全名
			/// </summary>
			public string DeclaringTypeFullName;

			/// <summary>
			/// 是否静态方法（无目标对象）
			/// </summary>
			public bool IsStatic;

			/// <summary>
			/// 实例 handler 的目标对象类型名（静态为空串）
			/// </summary>
			public string TargetTypeName;
		}

		/// <summary>
		/// 按事件 ID 分组的订阅组
		/// </summary>
		private sealed class HandlerGroup
		{
			/// <summary>
			/// 事件 ID
			/// </summary>
			public string EventId;

			/// <summary>
			/// 该 ID 下的 handler 行
			/// </summary>
			public readonly List<HandlerRow> Rows = new();
		}

		/// <summary>
		/// 待分发队列显示行
		/// </summary>
		private sealed class EventRow
		{
			/// <summary>
			/// 队列序号（从 1 开始）
			/// </summary>
			public int Index;

			/// <summary>
			/// 事件 ID
			/// </summary>
			public string EventId;

			/// <summary>
			/// 发送者类型名（null sender 显示为空串）
			/// </summary>
			public string SenderTypeName;

			/// <summary>
			/// 事件参数类型名
			/// </summary>
			public string ArgsTypeName;
		}

		#endregion

		#region 私有字段

		/// <summary>
		/// 订阅明细段折叠状态
		/// </summary>
		private bool m_handlerSectionOpen = true;

		/// <summary>
		/// 待分发队列段折叠状态（队列内容变化快，默认折叠避免刷屏）
		/// </summary>
		private bool m_queueSectionOpen;

		/// <summary>
		/// 各事件 ID 的订阅组折叠状态缓存
		/// </summary>
		private readonly Dictionary<string, bool> m_groupFoldoutStates = new();

		/// <summary>
		/// 订阅分组快照（按事件 ID 升序）
		/// </summary>
		private readonly List<HandlerGroup> m_handlerGroups = new();

		/// <summary>
		/// 待分发队列快照
		/// </summary>
		private readonly List<EventRow> m_eventRows = new();

		/// <summary>
		/// 快照构建中的当前分组（回调填充用，避免逐条字典查找开销大的替代方案）
		/// </summary>
		private HandlerGroup m_buildingGroup;

		#endregion

		#region 反射缓存

		/// <summary>
		/// EventModule 类型
		/// </summary>
		private Type m_moduleType;

		/// <summary>
		/// EventModule 热更实例
		/// </summary>
		private object m_moduleInstance;

		/// <summary>
		/// GameEventArgs 类型（构造回调委托的泛型实参）
		/// </summary>
		private Type m_gameEventArgsType;

		/// <summary>
		/// EventModule.EventCount 属性（待分发事件数）
		/// </summary>
		private PropertyInfo m_eventCountProperty;

		/// <summary>
		/// EventModule.EventHandlerCount 属性（handler 总数）
		/// </summary>
		private PropertyInfo m_eventHandlerCountProperty;

		/// <summary>
		/// GameEventArgs.Id 属性（队列行取事件 ID）
		/// </summary>
		private PropertyInfo m_eventIdProperty;

		/// <summary>
		/// EventModule.ForEachHandler 方法（参数 Action&lt;string, EventHandler&lt;GameEventArgs&gt;&gt;）
		/// </summary>
		private MethodInfo m_forEachHandlerMethod;

		/// <summary>
		/// EventModule.ForEachEvent 方法（参数 Action&lt;object, GameEventArgs&gt;）
		/// </summary>
		private MethodInfo m_forEachEventMethod;

		/// <summary>
		/// EventModule 私有字段 m_eventPool（清空队列时经池实例调用 Clear）
		/// </summary>
		private FieldInfo m_eventPoolField;

		/// <summary>
		/// 池实例类型的 Clear 方法
		/// </summary>
		private MethodInfo m_poolClearMethod;

		/// <summary>
		/// 已绑定的 ForEachHandler 回调委托（Action&lt;string, EventHandler&lt;GameEventArgs&gt;&gt;）
		/// </summary>
		private Delegate m_forEachHandlerCallback;

		/// <summary>
		/// 已绑定的 ForEachEvent 回调委托（Action&lt;object, GameEventArgs&gt;）
		/// </summary>
		private Delegate m_forEachEventCallback;

		#endregion

		#region 概览与主体绘制

		/// <summary>
		/// 刷新动作：重建两份快照（自动刷新到点、手动点击「刷新」、反射绑定完成与清空队列后触发）。
		/// 经 ForEachHandler/ForEachEvent 的回调逐条填充
		/// （二者为主线程 API 且自带重入快照保护，OnGUI 主线程调用安全）
		/// </summary>
		protected override void OnRefresh()
		{
			m_handlerGroups.Clear();
			m_eventRows.Clear();
			m_buildingGroup = null;

			try
			{
				// 把回调作为实参传入 ForEachHandler/ForEachEvent（模块内部遍历订阅/队列时逐条回调）：
				// 不能直接 DynamicInvoke 回调本身——它需要 (id, handler)/(sender, eArgs) 实参，那些由模块提供
				m_forEachHandlerMethod?.Invoke(m_moduleInstance, new object[] { m_forEachHandlerCallback });
				m_forEachEventMethod?.Invoke(m_moduleInstance, new object[] { m_forEachEventCallback });
			}
			catch (Exception e)
			{
				Debug.LogError($"[事件调试]构建快照失败:{e.InnerException?.Message ?? e.Message}");
			}

			m_buildingGroup = null;

			// 订阅组按事件 ID 升序，保证展示顺序稳定
			m_handlerGroups.Sort((a, b) => string.CompareOrdinal(a.EventId, b.EventId));
		}

		/// <summary>
		/// 绘制模块总览条
		/// </summary>
		protected override void DrawOverview()
		{
			var eventCount        = (int)(m_eventCountProperty?.GetValue(m_moduleInstance) ?? 0);
			var handlerTotalCount = (int)(m_eventHandlerCountProperty?.GetValue(m_moduleInstance) ?? 0);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label($"待分发事件: {eventCount}", GUILayout.MinWidth(110));
			DrawColumnSeparator();
			GUILayout.Label($"订阅事件 ID 数: {m_handlerGroups.Count}", GUILayout.MinWidth(130));
			DrawColumnSeparator();
			GUILayout.Label($"handler 总数: {handlerTotalCount}", GUILayout.MinWidth(130));
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制主体内容：订阅明细 + 待分发队列（基类滚动容器内调用）
		/// </summary>
		protected override void DrawContent()
		{
			DrawHandlerSection();
			EditorGUILayout.Separator();
			DrawQueueSection();
		}

		#endregion

		#region 展开折叠

		/// <summary>
		/// 全部展开（设置全部订阅组的折叠状态）
		/// </summary>
		protected override void ExpandAll()
		{
			foreach (var group in m_handlerGroups)
			{
				m_groupFoldoutStates[group.EventId] = true;
			}
		}

		/// <summary>
		/// 全部折叠（设置全部订阅组的折叠状态）
		/// </summary>
		protected override void CollapseAll()
		{
			foreach (var group in m_handlerGroups)
			{
				m_groupFoldoutStates[group.EventId] = false;
			}
		}

		#endregion

		#region 订阅明细

		/// <summary>
		/// 绘制订阅明细段
		/// </summary>
		private void DrawHandlerSection()
		{
			var foldoutOldColor = GUI.color;
			GUI.color             = Color.cyan;
			m_handlerSectionOpen  = EditorGUILayout.Foldout(m_handlerSectionOpen, $"订阅明细（{m_handlerGroups.Count} 个事件 ID）", true);
			GUI.color             = foldoutOldColor;
			if (!m_handlerSectionOpen) return;

			EditorGUILayout.BeginVertical("box");
			foreach (var group in m_handlerGroups)
			{
				DrawHandlerGroup(group);
			}

			if (m_handlerGroups.Count == 0)
			{
				EditorGUILayout.HelpBox("当前无任何事件订阅", MessageType.Info);
			}
			EditorGUILayout.EndVertical();
		}

		/// <summary>
		/// 绘制单个事件 ID 的订阅组
		/// </summary>
		/// <param name="group">订阅分组</param>
		private void DrawHandlerGroup(HandlerGroup group)
		{
			// 搜索过滤：事件 ID 或组内任一 handler 匹配才展示
			if (!MatchSearch(group)) return;

			if (!m_groupFoldoutStates.TryGetValue(group.EventId, out var isOpen))
			{
				isOpen = true;
				m_groupFoldoutStates[group.EventId] = true;
			}

			m_groupFoldoutStates[group.EventId] = EditorGUILayout.Foldout(isOpen, $"{group.EventId}  ({group.Rows.Count} 个 handler)", true);
			if (!m_groupFoldoutStates[group.EventId]) return;

			EditorGUILayout.BeginVertical("box");
			foreach (var row in group.Rows)
			{
				DrawHandlerRow(row);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();
		}

		/// <summary>
		/// 绘制单个 handler 行：方法名 | 所属类型 | 静态/实例 | 目标对象类型 | [跳转]
		/// </summary>
		private void DrawHandlerRow(HandlerRow row)
		{
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(row.MethodName, GUILayout.MinWidth(180));
			DrawColumnSeparator();
			GUILayout.Label(row.DeclaringTypeFullName, GUILayout.MinWidth(200));
			DrawColumnSeparator();
			GUILayout.Label(row.IsStatic ? "静态" : $"实例({row.TargetTypeName})", GUILayout.MinWidth(120));
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("跳转", GUILayout.Width(44)))
			{
				OpenHandlerSource(row.Method);
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 判断订阅组是否命中搜索过滤：事件 ID、方法名或所属类型名任一包含即命中（大小写不敏感）
		/// </summary>
		private bool MatchSearch(HandlerGroup group)
		{
			if (string.IsNullOrEmpty(m_searchFilter)) return true;

			if (group.EventId.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;

			foreach (var row in group.Rows)
			{
				if (row.MethodName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
				if (row.DeclaringTypeFullName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
			}

			return false;
		}

		/// <summary>
		/// 跳转到 handler 方法源码：
		///     1. 按所属类名收集全部候选脚本文件（分部类会有多个，文件名精确一致者优先），
		///        逐文件扫描「声明形态」的方法行——C# 分部类不允许重复声明同一方法，方法声明在且仅在
		///        唯一一个分部文件中，故声明行命中即唯一真身；
		///     2. 无声明行命中时退化为首个含「方法名(」出现的文件行（重载/同名的近似场景）；
		///     3. 再退化仅打开首个候选文件，找不到任何候选文件时告警。
		/// 说明：曾评估 Mono.Cecil 读调试符号精确映射，但 Unity 安装目录自带 DLL 对 asmdef 程序集不可见
		/// （仅经典 Editor 文件夹自动引用），改造 asmdef 引用方式会影响其它预编译引用，故不采用。
		/// </summary>
		/// <param name="method">handler 方法元数据</param>
		private static void OpenHandlerSource(MethodInfo method)
		{
			if (method == null) return;

			var declaringTypeName = method.DeclaringType?.Name ?? string.Empty;
			var candidatePaths    = FindScriptAssetsByClassName(SimplifyTypeName(declaringTypeName));
			if (candidatePaths.Count == 0)
			{
				Debug.LogWarning($"[事件调试]未找到方法 '{method.DeclaringType?.FullName}.{method.Name}' 对应的源文件.");
				return;
			}

			// 声明行正则：方法名同行前方需存在访问/函数修饰符，天然排除调用点（xxx.MethodName(、var a = MethodName( 等）
			var declarationPattern = new Regex(
				@"\b(public|private|protected|internal|static|async|override|virtual|sealed|new)\b[^;{=]*\b" + Regex.Escape(method.Name) + @"\s*\(",
				RegexOptions.Compiled);

			string declarationPath = null;
			var declarationLine    = 0;
			string occurrencePath  = null;
			var occurrenceLine     = 0;

			foreach (var candidatePath in candidatePaths)
			{
				ScanMethodInFile(candidatePath, method.Name, declarationPattern, out var lineDeclaration, out var lineFirstOccurrence);

				// 声明行命中即唯一真身（分部类不可重复声明），立即采用
				if (lineDeclaration > 0)
				{
					declarationPath = candidatePath;
					declarationLine = lineDeclaration;
					break;
				}

				// 首个含方法名出现的文件记为次选
				if (occurrencePath == null && lineFirstOccurrence > 0)
				{
					occurrencePath = candidatePath;
					occurrenceLine = lineFirstOccurrence;
				}
			}

			if (TryOpenAtLine(declarationPath, declarationLine)) return;
			if (TryOpenAtLine(occurrencePath, occurrenceLine)) return;

			// 找不到任何行级定位：仅打开首个候选文件
			var fallbackAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidatePaths[0]);
			if (fallbackAsset != null)
			{
				AssetDatabase.OpenAsset(fallbackAsset);
				return;
			}

			Debug.LogWarning($"[事件调试]未找到方法 '{method.DeclaringType?.FullName}.{method.Name}' 对应的源文件.");
		}

		/// <summary>
		/// 按资产路径打开文件并定位到指定行（行号无效时仅打开文件），资产不存在返回 false
		/// </summary>
		private static bool TryOpenAtLine(string assetPath, int lineNumber)
		{
			if (string.IsNullOrEmpty(assetPath) || lineNumber <= 0) return false;

			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
			if (asset == null) return false;

			AssetDatabase.OpenAsset(asset, lineNumber);
			return true;
		}

		/// <summary>
		/// 剥离类型名中的泛型元数（如 EventPool`1 的 `1）与嵌套分隔，得到可与文件名/简单名比对的形态
		/// </summary>
		private static string SimplifyTypeName(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			var backtick = name.IndexOf('`');
			var simple   = backtick >= 0 ? name.Substring(0, backtick) : name;
			var plus     = simple.IndexOf('+');
			return plus >= 0 ? simple.Substring(plus + 1) : simple;
		}

		/// <summary>
		/// 按类名在工程内收集候选脚本资产（分部类可能对应多个文件）：
		/// 文件名与类名精确一致的排在最前，其余模糊命中（如 WinBase.EventRegister.cs）按 FindAssets 顺序跟随
		/// </summary>
		private static List<string> FindScriptAssetsByClassName(string className)
		{
			var paths = new List<string>();
			if (string.IsNullOrEmpty(className)) return paths;

			var guids = AssetDatabase.FindAssets($"t:Script {className}");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path)) continue;

				if (Path.GetFileNameWithoutExtension(path) == className) paths.Insert(0, path);
				else paths.Add(path);
			}

			return paths;
		}

		/// <summary>
		/// 扫描单个源文件：输出「声明形态的方法行」与「方法名( 首次出现行」（均 1 起始，未找到为 0）
		/// </summary>
		private static void ScanMethodInFile(string assetPath, string methodName, Regex declarationPattern, out int declarationLine, out int firstOccurrenceLine)
		{
			declarationLine     = 0;
			firstOccurrenceLine = 0;

			try
			{
				var lines = File.ReadAllLines(Path.GetFullPath(assetPath));
				var token = methodName + "(";
				for (var i = 0; i < lines.Length; i++)
				{
					// 首次出现行：方法名后紧跟左括号
					if (firstOccurrenceLine == 0 && lines[i].Contains(token)) firstOccurrenceLine = i + 1;

					// 声明形态行：命中修饰符前缀正则
					if (declarationLine == 0 && declarationPattern.IsMatch(lines[i])) declarationLine = i + 1;

					if (declarationLine > 0 && firstOccurrenceLine > 0) return;
				}
			}
			catch (Exception)
			{
				// 读文件失败按「未找到」处理，由调用方降级
			}
		}

		#endregion

		#region 待分发队列

		/// <summary>
		/// 绘制待分发队列段
		/// </summary>
		private void DrawQueueSection()
		{
			var foldoutOldColor = GUI.color;
			GUI.color             = Color.cyan;
			m_queueSectionOpen    = EditorGUILayout.Foldout(m_queueSectionOpen, $"待分发队列（{m_eventRows.Count} 条）", true);
			GUI.color             = foldoutOldColor;
			if (!m_queueSectionOpen) return;

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("清空待分发队列", "调用事件池 Clear：丢弃队列中全部未分发事件（含参数回收）。"), GUILayout.Width(140)))
			{
				ClearPendingEvents();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			if (m_eventRows.Count == 0)
			{
				EditorGUILayout.LabelField("队列为空");
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("#", GUILayout.Width(40));
				GUILayout.Label("事件 ID", GUILayout.MinWidth(220));
				GUILayout.Label("发送者", GUILayout.MinWidth(160));
				GUILayout.Label("参数类型", GUILayout.MinWidth(200));
				EditorGUILayout.EndHorizontal();

				foreach (var row in m_eventRows)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label(row.Index.ToString(), GUILayout.Width(40));
					GUILayout.Label(row.EventId, GUILayout.MinWidth(220));
					GUILayout.Label(string.IsNullOrEmpty(row.SenderTypeName) ? "(null)" : row.SenderTypeName, GUILayout.MinWidth(160));
					GUILayout.Label(row.ArgsTypeName, GUILayout.MinWidth(200));
					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndVertical();
		}

		/// <summary>
		/// 清空待分发队列：经反射取 EventModule 私有字段 m_eventPool 调用池的 Clear
		/// （EventModule 未转发 Clear；Clear 会丢弃全部未分发事件并回收其参数，属破坏性调试操作）
		/// </summary>
		private void ClearPendingEvents()
		{
			try
			{
				var pool = m_eventPoolField?.GetValue(m_moduleInstance);
				if (pool == null)
				{
					Debug.LogError("[事件调试]获取事件池实例失败，无法清空队列.");
					return;
				}

				m_poolClearMethod?.Invoke(pool, null);
				OnRefresh();
				Repaint();
			}
			catch (Exception e)
			{
				Debug.LogError($"清空待分发队列失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
			}
		}

		#endregion

		#region 快照构建

		/// <summary>
		/// ForEachHandler 回调的泛型适配（经 MakeGenericMethod 绑定为精确签名委托）：
		/// 把 (id, handler) 折叠进分组快照
		/// </summary>
		private void HandlerEntry<T>(string id, EventHandler<T> handler) where T : EventArgs
		{
			// 事件 ID 变化时切换当前分组（ForEachHandler 按 id 连续枚举同一事件的多 handler，但不依赖该前提：显式查重）
			if (m_buildingGroup == null || m_buildingGroup.EventId != id)
			{
				m_buildingGroup = FindOrAddGroup(id);
			}

			var method = handler.Method;
			m_buildingGroup.Rows.Add(new HandlerRow
			{
				Method                = method,
				MethodName            = method.Name,
				DeclaringTypeFullName = method.DeclaringType?.FullName ?? "(unknown)",
				IsStatic              = handler.Target == null,
				TargetTypeName        = handler.Target?.GetType().Name ?? string.Empty,
			});
		}

		/// <summary>
		/// ForEachEvent 回调的泛型适配（经 MakeGenericMethod 绑定为精确签名委托）：
		/// 把队列中的 (sender, eArgs) 折叠进行快照
		/// </summary>
		private void EventEntry<T>(object sender, T eArgs) where T : EventArgs
		{
			m_eventRows.Add(new EventRow
			{
				Index          = m_eventRows.Count + 1,
				EventId        = m_eventIdProperty?.GetValue(eArgs) as string ?? "(unknown)",
				SenderTypeName = sender?.GetType().Name ?? string.Empty,
				ArgsTypeName   = eArgs?.GetType().Name ?? "(null)",
			});
		}

		/// <summary>
		/// 查找或建立事件 ID 对应的订阅分组
		/// </summary>
		private HandlerGroup FindOrAddGroup(string id)
		{
			for (var i = 0; i < m_handlerGroups.Count; i++)
			{
				if (m_handlerGroups[i].EventId == id) return m_handlerGroups[i];
			}

			var group = new HandlerGroup { EventId = id };
			m_handlerGroups.Add(group);
			return group;
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

			m_moduleType        = HotfixReflection.EventModule;
			m_gameEventArgsType = HotfixReflection.GameEventArgs;
			if (m_moduleType == null || m_gameEventArgsType == null) return false;

			// EventModule 无静态 Instance 保证，经 ModuleManager.GetModule<T> 泛型方法获取热更实例（ObjectPoolModuleWindow 同款）
			m_moduleInstance = HotfixReflection.GetModuleInstance(m_moduleType);
			if (m_moduleInstance == null) return false;

			// EventModule 成员
			m_eventCountProperty        = m_moduleType.GetProperty("EventCount",        BindingFlags.Public | BindingFlags.Instance);
			m_eventHandlerCountProperty = m_moduleType.GetProperty("EventHandlerCount", BindingFlags.Public | BindingFlags.Instance);
			m_forEachHandlerMethod      = m_moduleType.GetMethod("ForEachHandler",      BindingFlags.Public | BindingFlags.Instance);
			m_forEachEventMethod        = m_moduleType.GetMethod("ForEachEvent",        BindingFlags.Public | BindingFlags.Instance);
			m_eventPoolField            = m_moduleType.GetField("m_eventPool", BindingFlags.NonPublic | BindingFlags.Instance);

			// GameEventArgs 成员
			m_eventIdProperty = m_gameEventArgsType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

			// 事件池 Clear（清空队列用）
			var eventPoolType = m_eventPoolField?.FieldType;
			m_poolClearMethod = eventPoolType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);

			if (m_forEachHandlerMethod == null || m_forEachEventMethod == null || m_eventIdProperty == null) return false;

			// 回调绑定：窗口内泛型适配方法 MakeGenericMethod 后 CreateDelegate，签名精确匹配
			var actionOfStringAndHandler = typeof(Action<,>).MakeGenericType(typeof(string), typeof(EventHandler<>).MakeGenericType(m_gameEventArgsType));
			var actionOfObjectAndArgs    = typeof(Action<,>).MakeGenericType(typeof(object), m_gameEventArgsType);

			var handlerAdapter = GetType().GetMethod(nameof(HandlerEntry), BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(m_gameEventArgsType);
			var eventAdapter   = GetType().GetMethod(nameof(EventEntry),   BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(m_gameEventArgsType);
			if (handlerAdapter == null || eventAdapter == null) return false;

			m_forEachHandlerCallback = Delegate.CreateDelegate(actionOfStringAndHandler, this, handlerAdapter);
			m_forEachEventCallback   = Delegate.CreateDelegate(actionOfObjectAndArgs, this, eventAdapter);

			if (m_forEachHandlerCallback == null || m_forEachEventCallback == null) return false;

			// 绑定后立即重建一次快照，避免首帧展示空数据
			OnRefresh();
			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_moduleType              = null;
			m_moduleInstance          = null;
			m_gameEventArgsType       = null;
			m_eventCountProperty      = null;
			m_eventHandlerCountProperty = null;
			m_eventIdProperty         = null;
			m_forEachHandlerMethod    = null;
			m_forEachEventMethod      = null;
			m_eventPoolField          = null;
			m_poolClearMethod         = null;
			m_forEachHandlerCallback  = null;
			m_forEachEventCallback    = null;

			m_handlerGroups.Clear();
			m_eventRows.Clear();
			m_buildingGroup = null;
		}

		#endregion
	}
}
#endif
