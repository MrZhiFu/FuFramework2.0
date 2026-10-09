#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FuMenuPriority = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase = FuFramework.Core.Editor.DebugWindowBase;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Editor
{
	/// <summary>
	/// 计时器调试面板。
	/// 仅在 Play 模式下可用，经 HotfixReflection 访问 Hotfix 中的 TimerModule。
	/// 功能：
	///     1. 概览：活跃总数、类型分布、暂停数、累计启动/完成/停止/异常终止与批量操作。
	///     2. 逐计时器：Id/类型/进度摘要折叠行，展开展示回调、剩余/累计、状态标志与启动来源堆栈。
	///     3. 逐计时器操作：暂停/恢复、重置、停止。
	/// 注意：搜索过滤支持 Id、类型标签与启动堆栈（类名/方法名）片段匹配。
	/// </summary>
	public sealed partial class TimerModuleWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/计时器调试面板", false, FuMenuPriority.DEBUG_PANEL_TIMER)]
		public static void ShowWindow()
		{
			var window = GetWindow<TimerModuleWindow>("计时器调试");
			window.minSize = new Vector2(760, 500);

			// 初始位置居中显示
			const float width  = 1000f;
			const float height = 600f;
			var         x      = (Screen.currentResolution.width  - width)  / 2f;
			var         y      = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "TimerModule";

		#endregion

		#region 私有字段

		/// <summary>
		/// 计时器折叠状态缓存（Id 单调递增不复用，可作稳定键）
		/// </summary>
		private readonly Dictionary<int, bool> m_foldoutStates = new();

		/// <summary>
		/// 启动堆栈展开状态缓存
		/// </summary>
		private readonly HashSet<int> m_stackOpenStates = new();

		/// <summary>
		/// 待刷新标记：行内操作（SafeInvoke）发生在 DrawContent 的 m_infos 枚举途中，
		/// 不得当场 OnRefresh 重建列表（「遍历中修改容器」），挂起到下一次 OnGUI 的 DrawOverview 顶部统一消费。
		/// </summary>
		private bool m_refreshPending;

		/// <summary>
		/// 模块实例（每次刷新重建；非 Play 或模块未初始化为 null）
		/// </summary>
		private object m_moduleInstance;

		/// <summary>
		/// 计时器信息快照（GetAllTimerInfos 返回的结构体数组逐元素装箱）
		/// </summary>
		private readonly List<object> m_infos = new();

		/// <summary>
		/// 各类型活跃计数（倒计时/时间间隔/帧间隔）
		/// </summary>
		private readonly int[] m_typeCounts = new int[3];

		/// <summary>
		/// 暂停中数量
		/// </summary>
		private int m_pausedCount;

		/// <summary>
		/// 累计启动数
		/// </summary>
		private int m_statStarted;

		/// <summary>
		/// 累计完成数
		/// </summary>
		private int m_statFinished;

		/// <summary>
		/// 累计停止数
		/// </summary>
		private int m_statStopped;

		/// <summary>
		/// 累计异常终止数
		/// </summary>
		private int m_statError;

		#endregion


		#region 概览与主体绘制

		/// <summary>
		/// 绘制模块级概览（计数统计 + 批量操作 + 堆栈记录开关）
		/// </summary>
		protected override void DrawOverview()
		{
			// 消费行内操作挂起的刷新：此时 m_infos 尚未进入枚举，重建安全（早于 DrawContent）
			if (m_refreshPending)
			{
				m_refreshPending = false;
				OnRefresh();
			}

			if (m_moduleInstance == null)
			{
				EditorGUILayout.HelpBox("TimerModule 尚未初始化（需进入 Play 模式并完成框架启动）", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField(
			                           $"活跃计时器：{m_infos.Count}（一次性倒计时 {m_typeCounts[0]} · 时间间隔 {m_typeCounts[1]} · 帧间隔 {m_typeCounts[2]}）｜ 暂停中：{m_pausedCount}");
			EditorGUILayout.LabelField($"累计：启动 {m_statStarted} · 完成 {m_statFinished} · 停止 {m_statStopped} · 异常终止 {m_statError}");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("全部暂停", GUILayout.Width(90)))
			{
				SafeInvoke(m_pauseAllMethod);
			}

			if (GUILayout.Button("全部恢复", GUILayout.Width(90)))
			{
				SafeInvoke(m_resumeAllMethod);
			}

			if (GUILayout.Button("全部重置", GUILayout.Width(90)))
			{
				SafeInvoke(m_resetAllMethod);
			}

			if (GUILayout.Button("全部停止", GUILayout.Width(90)))
			{
				SafeInvoke(m_stopAllMethod);
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制主体（计时器列表）
		/// </summary>
		protected override void DrawContent()
		{
			if (m_moduleInstance == null) return;

			if (m_infos.Count == 0)
			{
				EditorGUILayout.HelpBox("当前无活跃计时器（启动计时器后此处展示列表）", MessageType.Info);
				return;
			}

			foreach (var info in m_infos)
			{
				DrawTimer(info);
			}
		}

		#endregion

		#region 计时器绘制

		/// <summary>
		/// 绘制单个计时器（折叠行 + 明细 + 行内操作）
		/// </summary>
		/// <param name="info">计时器信息（TimerInfo 装箱实例）</param>
		private void DrawTimer(object info)
		{
			if (info == null) return;

			var id               = (int)(m_infoIdProperty?.GetValue(info) ?? -1);
			var typeName         = GetTimerTypeLabel(m_infoTypeProperty?.GetValue(info));
			var isPaused         = (bool)(m_infoIsPausedProperty?.GetValue(info)        ?? false);
			var ignoreScale      = (bool)(m_infoIgnoreTimeScaleProperty?.GetValue(info) ?? false);
			var executed         = (int)(m_infoExecutedCountProperty?.GetValue(info)    ?? 0);
			var maxCount         = (int)(m_infoMaxCountProperty?.GetValue(info)         ?? -1);
			var remaining        = (float)(m_infoRemainingTimeProperty?.GetValue(info)  ?? -1f);
			var duration         = (float)(m_infoDurationTimeProperty?.GetValue(info)   ?? -1f);
			var interval         = (float)(m_infoIntervalProperty?.GetValue(info)       ?? -1f);
			var accumulated      = (float)(m_infoAccumulatedProperty?.GetValue(info)    ?? -1f);
			var stackTrace       = m_infoStartStackTraceProperty?.GetValue(info) as string;
			var isCountdown      = duration >= 0f;
			var immediatePending = (bool)(m_infoImmediatePendingProperty?.GetValue(info) ?? false);

			// 搜索过滤：Id、类型标签、启动堆栈（含启动处类名/方法名）任一命中即展示
			if (!string.IsNullOrEmpty(m_searchFilter))
			{
				var hit = id.ToString().Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)
				          || typeName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)
				          || (stackTrace ?? "").Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase);
				if (!hit) return;
			}

			if (!m_foldoutStates.TryGetValue(id, out var isOpen))
			{
				isOpen              = true;
				m_foldoutStates[id] = true;
			}

			var progressText = isCountdown
				? $" {Mathf.Max(remaining, 0f):F2}s / {duration:F2}s"
				: maxCount < 0
					? $"{executed}/∞"
					: $"{executed}/{maxCount}";
			var title = $"#{id} [{typeName}] {progressText}" + (isPaused ? " · ⏸已暂停" : "");

			// 折叠行标题：青色常规，暂停黄色高亮
			var oldColor = GUI.color;
			GUI.color           = isPaused ? Color.yellow : Color.cyan;
			m_foldoutStates[id] = EditorGUILayout.Foldout(isOpen, title, true);
			GUI.color           = oldColor;
			if (!m_foldoutStates[id]) return;

			EditorGUILayout.BeginVertical("box");
			{
				if (isCountdown)
				{
					EditorGUILayout.LabelField("剩余时间", $"{remaining:F3}s");
					EditorGUILayout.LabelField("总时长",  $"{duration:F3}s");
				}
				else
				{
					// 单位按类型区分：时间间隔计时器为秒，帧间隔计时器为帧
					var unit = typeName == "帧间隔" ? "帧" : "s";
					EditorGUILayout.LabelField("间隔", $"{interval}{unit}");
					EditorGUILayout.LabelField("累计", $"{accumulated}{unit}");
					EditorGUILayout.LabelField("次数", maxCount < 0 ? $"{executed} / 无限" : $"{executed} / {maxCount}");
				}

				var flags = new List<string>();
				if (isPaused) flags.Add("暂停中");
				if (ignoreScale) flags.Add("忽略时间缩放");
				if (immediatePending) flags.Add("立即回调待发");
				EditorGUILayout.LabelField("状态", flags.Count > 0 ? string.Join("、", flags) : "运行中");

				if (!string.IsNullOrEmpty(stackTrace))
				{
					DrawStackTrace(id, stackTrace);
				}

				DrawTimerActions(id, isPaused);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();
		}

		/// <summary>
		/// 绘制启动堆栈（可折叠 + 代码定位按钮）
		/// </summary>
		private void DrawStackTrace(int id, string stackTrace)
		{
			EditorGUILayout.BeginHorizontal();

			var isOpen = m_stackOpenStates.Contains(id);
			isOpen = EditorGUILayout.Foldout(isOpen, "启动堆栈", true);
			if (isOpen)
			{
				m_stackOpenStates.Add(id);
			}
			else
			{
				m_stackOpenStates.Remove(id);
			}

			GUILayout.Space(24);

			if (GUILayout.Button("代码定位", GUILayout.Width(70)))
			{
				OpenStartSource(stackTrace);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			if (isOpen)
			{
				EditorGUILayout.TextArea(stackTrace, GUILayout.Height(120));
			}
		}

		/// <summary>
		/// 绘制单计时器操作按钮（暂停/恢复按状态二选一 + 重置 + 停止）
		/// </summary>
		private void DrawTimerActions(int id, bool isPaused)
		{
			EditorGUILayout.BeginHorizontal();
			if (isPaused)
			{
				if (GUILayout.Button("恢复", GUILayout.Width(80)))
				{
					SafeInvoke(m_resumeTimerMethod, id);
				}
			}
			else
			{
				if (GUILayout.Button("暂停", GUILayout.Width(80)))
				{
					SafeInvoke(m_pauseTimerMethod, id);
				}
			}

			if (GUILayout.Button("重置", GUILayout.Width(80)))
			{
				SafeInvoke(m_resetTimerMethod, id);
			}

			if (GUILayout.Button("停止", GUILayout.Width(80)))
			{
				SafeInvoke(m_stopTimerMethod, id);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region 工具

		/// <summary>
		/// 安全调用模块方法（单参数或无参；异常落日志不中断绘制），调用后刷新一次快照
		/// </summary>
		private void SafeInvoke(MethodInfo method, int argument = -1)
		{
			try
			{
				if (method == null) return;
				if (argument >= 0)
				{
					method.Invoke(m_moduleInstance, new object[] { argument });
				}
				else
				{
					method.Invoke(m_moduleInstance, null);
				}

				// 操作后不立即重建快照（此时正处于 DrawContent 的 m_infos 枚举途中，
				// 当场 Clear/重填会「遍历中修改容器」抛异常）——挂起标记，下个 GUI 事件在 DrawOverview 顶部统一刷新
				m_refreshPending = true;
			}
			catch (Exception e)
			{
				Debug.LogError($"计时器调试面板调用 {method.Name} 失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
			}
		}

		/// <summary>
		/// 将装箱的 ETimerType 转为中文标签（未知值返回原 ToString）
		/// </summary>
		private static string GetTimerTypeLabel(object boxedType)
		{
			switch (boxedType?.ToString())
			{
				// @formatter:off
				case "Countdown": return "一次性倒计时";
				case "Interval":  return "时间间隔";
				case "Frame":     return "帧间隔";
				default:          return boxedType?.ToString() ?? "未知";
				// @formatter:on
			}
		}

		/// <summary>
		/// 从启动堆栈解析首个业务帧并跳转源码：
		///     1. 逐帧跳过运行时内部帧（System.*）、计时器框架管线帧（Hotfix.Framework.Timer.*）与
		///        GlobalModule 访问器帧，取首个业务帧（WinBase 等转发垫片视为业务边界）；
		///     2. 业务帧携带「文件路径:行号」且行号即 StartXxxTimer 调用行，截取 "Assets/" 起始的
		///        工程相对路径后经 AssetDatabase.OpenAsset(asset, line) 精确打开；
		///     3. 无可跳转业务帧（无符号信息，如热更 DLL）时告警。
		/// 格式注意：本工程 Unity Mono 的 Environment.StackTrace 帧行号为「in 路径:行号」——
		/// 与标准 .NET 的「in 路径:line 行号」不同（2026-10-09 TempStackProbe 实测），正则两种都兼容。
		/// </summary>
		/// <param name="stackTrace">启动来源堆栈文本</param>
		private static void OpenStartSource(string stackTrace)
		{
			if (string.IsNullOrEmpty(stackTrace)) return;

			// 帧形态：at 方法全名(参数) [IL偏移] in 文件路径:行号
			// （[IL偏移] 段可缺省；行号兼容 Mono「:39」与标准 .NET「:line 39」两种写法）
			var framePattern = new Regex(@"at\s+(?<method>[^(]+)\([^)]*\)(?:\s+\[[^\]]*\])?(?:\s+in\s+(?<path>.+?):(?:line\s+)?(?<line>\d+))?",
			                             RegexOptions.Compiled);

			foreach (var line in stackTrace.Split('\n'))
			{
				var match = framePattern.Match(line);
				if (!match.Success) continue;

				var method = match.Groups["method"].Value.Trim();

				// 跳过运行时内部帧、计时器框架管线帧与 GlobalModule 访问器帧（业务帧即「谁启动的这个计时器」）
				if (method.StartsWith("System.",                 StringComparison.Ordinal)) continue;
				if (method.StartsWith("Hotfix.Framework.Timer.", StringComparison.Ordinal)) continue;
				if (method.Contains(".get_TimerModule", StringComparison.Ordinal)) continue;

				var pathGroup = match.Groups["path"];
				var lineGroup = match.Groups["line"];
				if (!pathGroup.Success || !lineGroup.Success) continue;

				// 绝对路径 → 工程相对 "Assets/..." 路径（堆栈内为平台绝对路径，统一分隔符后截取）
				var absolutePath = pathGroup.Value.Replace('\\', '/');
				var assetsIndex  = absolutePath.IndexOf("/Assets/", StringComparison.Ordinal);
				if (assetsIndex < 0) continue;

				var assetPath = absolutePath.Substring(assetsIndex + 1);
				var asset     = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
				if (asset == null) continue;

				AssetDatabase.OpenAsset(asset, int.Parse(lineGroup.Value));
				return;
			}

			Debug.LogWarning("[计时器调试] 启动堆栈中未找到可跳转的业务代码帧（无文件行号信息）.");
		}

		#endregion

		#region 折叠操作

		/// <summary>
		/// 全部展开
		/// </summary>
		protected override void ExpandAll()
		{
			SetAllFoldouts(EnumerateIds(), m_foldoutStates, true);
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			SetAllFoldouts(EnumerateIds(), m_foldoutStates, false);
		}

		/// <summary>
		/// 枚举当前快照中的计时器 Id
		/// </summary>
		private IEnumerable<int> EnumerateIds()
		{
			foreach (var info in m_infos)
			{
				yield return (int)(m_infoIdProperty?.GetValue(info) ?? -1);
			}
		}

		#endregion
	}
}
#endif