#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
	/// <summary>
	/// 网络监控器调试面板：实时查看 TCP 链路的请求/响应/推送收发历史，支撑联调排错。
	/// 功能：
	///     1. 实时列表（虚拟化绘制，最多 2000 条循环记录）：Tab 筛选 + 搜索 + 消息号忽略，颜色区分消息类型与状态。
	///     2. 详情查看：基本信息 + JSON 结构查看（折叠）+ 二进制 Hex 查看 + 请求重发（受总开关把守）。
	///     3. 暂停/继续、清空记录；面板打开即开启录制、关闭即停（经基座 OnPanelEnabled/OnPanelDisabled 钩子）。
	/// 说明：
	///     1. 数据源为 Hotfix 侧 NetworkMonitor 静态采集器（不进包），经 HotfixReflection 反射访问（Editor 程序集不引用 HotFix 程序集的既定模式）。
	///     2. HTTP 链路的监控不在此面板——Web 模块调试面板已完整覆盖。
	/// </summary>
	public sealed class NetworkModuleWindow : DebugWindowBase
	{
		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "NetworkMonitor";

		/// <summary>
		/// 自动刷新间隔 100ms（对齐参考工具 10FPS，兼顾实时性与重绘开销）
		/// </summary>
		protected override float RefreshInterval => 0.1f;

		/// <summary>
		/// 无展开/折叠需求
		/// </summary>
		protected override bool HasExpandCollapse => false;

		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/网络监控器", false, FuMenuPriority.DEBUG_PANEL_NETWORK)]
		public static void ShowWindow()
		{
			var window = GetWindow<NetworkModuleWindow>("网络监控器");
			window.minSize = new Vector2(800, 600);

			// 初始位置居中显示
			const float width  = 1100f;
			const float height = 720f;
			var x = (Screen.currentResolution.width  - width)  / 2f;
			var y = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#endregion

		#region 私有类型

		/// <summary>
		/// 列表筛选页签。
		/// </summary>
		private enum ETab
		{
			/// <summary>全部消息</summary>
			All,

			/// <summary>仅请求</summary>
			Request,

			/// <summary>仅响应</summary>
			Response,

			/// <summary>仅推送</summary>
			Notify,

			/// <summary>仅心跳（双实现心跳通知，如校时）</summary>
			HeartBeat,

			/// <summary>仅异常（超时/取消）</summary>
			Error,
		}

		#endregion

		#region 常量

		/// <summary>
		/// 列表行高（固定，虚拟化绘制按行号推算位置）。
		/// </summary>
		private const float RowHeight = 25f;

		/// <summary>
		/// 忽略消息号列表的 EditorPrefs 键（逗号分隔存储）。
		/// </summary>
		private const string IgnoredMessagesKey = "NetworkMonitor_IgnoredMessages";

		/// <summary>
		/// 表头文字（前 8 列；第 9 列"备注"自适应宽度）。
		/// </summary>
		private static readonly string[] HeaderLabels = { "序号", "时刻", "类型", "消息名", "UniqueId", "状态", "耗时", "大小" };

		/// <summary>
		/// 前 8 列列宽（表头与数据行共用；第 9 列备注自适应）。
		/// </summary>
		private static readonly float[] ColumnWidths = { 100f, 125f, 85f, 250f, 110f, 105f, 100f, 100f };

		// 消息类型颜色（对齐参考工具；心跳绿色单独一类）
		private static readonly Color RequestTextColor = new(0xEE / 255f, 0x40 / 255f, 0x00 / 255f, 1f); // 请求：橙红
		private static readonly Color ResponseTextColor = new(0xB3 / 255f, 0xEE / 255f, 0x3A / 255f, 1f); // 响应：黄绿
		private static readonly Color NotifyTextColor = new(0xF7 / 255f, 0x09 / 255f, 0xC7 / 255f, 1f); // 推送：粉红
		private static readonly Color HeartBeatTextColor = new(0x32 / 255f, 0xCD / 255f, 0x32 / 255f, 1f); // 心跳：LimeGreen
		private static readonly Color ErrorStatusColor = new(1f, 0.35f, 0.35f, 1f);                     // 异常状态：红
		private static readonly Color SelectedRowColor = new(0.25f, 0.35f, 0.5f, 0.5f);                 // 选中行：半透明蓝

		#endregion

		#region 私有字段

		/// <summary>
		/// 当前页签。
		/// </summary>
		private ETab m_currentTab;

		/// <summary>
		/// 选中的记录序号（以 Seq 定位，列表滚动后保持稳定；-1=未选中）。
		/// </summary>
		private long m_selectedSeq = -1;

		/// <summary>
		/// 快照缓存（每次刷新整表拉取）。
		/// </summary>
		private object[] m_cachedRecords = Array.Empty<object>();

		/// <summary>
		/// 过滤后的显示列表（Tab+搜索+忽略 三重过滤产物，虚拟化绘制的数据源）。
		/// </summary>
		private List<object> m_displayRecords = new();

		/// <summary>
		/// 忽略的消息号集合（仅影响显示，不影响采集）。
		/// </summary>
		private readonly HashSet<int> m_ignoredMessageIds = new();

		/// <summary>
		/// 列表滚动位置（手工管理——虚拟化绘制需按滚动量推算可见行）。
		/// </summary>
		private Vector2 m_listScroll;

		/// <summary>
		/// GUI 样式（懒初始化，避免每帧构造）。行样式每行绘制前按消息类型改写 textColor（显式定色，不与主题叠加）。
		/// 注意：GUIStyle 会被 EditorWindow 序列化持久化——不加 [NonSerialized] 的话，样式在首次创建后被钉死，
		/// 后续改样式代码永远不生效（跨 domain reload 甚至跨编辑器重启都恢复旧值）。
		/// </summary>
		[NonSerialized] private GUIStyle m_rowStyle;

		/// <summary>
		/// 表头样式（固定浅灰白，不随消息类型变色）。
		/// </summary>
		[NonSerialized] private GUIStyle m_headerStyle;

		/// <summary>
		/// 底部操作条的选中摘要样式（按钮同源：垂直居中 + 颜色随选中消息类型）。
		/// </summary>
		[NonSerialized] private GUIStyle m_selectedSummaryStyle;

		/// <summary>
		/// 各页签的消息计数（与页签枚举值一一对应，每次刷新随快照重算）。
		/// </summary>
		private readonly int[] m_tabCounts = new int[6];

		/// <summary>
		/// 数据行文本容器（虚拟化绘制每行复用，避免每帧每行分配数组）。
		/// </summary>
		[NonSerialized] private string[] m_rowCells;

		#endregion

		#region 反射缓存

		/// <summary>
		/// NetworkMonitor 类型（静态采集器）
		/// </summary>
		private System.Type m_monitorType;

		/// <summary>
		/// NetworkMessageRecord 类型（记录条目）
		/// </summary>
		private System.Type m_recordType;

		// NetworkMonitor 静态成员
		private PropertyInfo m_enabledProperty;
		private MethodInfo   m_setEnabledMethod;
		private PropertyInfo m_isPausedProperty;
		private MethodInfo   m_getSnapshotMethod;
		private MethodInfo   m_clearMethod;
		private MethodInfo   m_resendMethod;
		private MethodInfo   m_triggerDisconnectMethod;

		/// <summary>
		/// NetworkModule 类型与 Instance 属性（触发断线入口的宿主）
		/// </summary>
		private System.Type m_moduleType;
		private PropertyInfo m_moduleInstanceProperty;

		// NetworkMessageRecord 实例成员（面板读取显示）
		private PropertyInfo m_recSeqProperty;
		private PropertyInfo m_recKindProperty;
		private PropertyInfo m_recTimeUtcProperty;
		private PropertyInfo m_recUniqueIdProperty;
		private PropertyInfo m_recMessageIdProperty;
		private PropertyInfo m_recMessageNameProperty;
		private PropertyInfo m_recStatusProperty;
		private PropertyInfo m_recElapsedMsProperty;
		private PropertyInfo m_recBodySizeProperty;
		private PropertyInfo m_recBodyBytesProperty;
		private PropertyInfo m_recBodyTruncatedProperty;
		private PropertyInfo m_recJsonSnapshotProperty;
		private PropertyInfo m_recIsResendProperty;

		#endregion

		#region 面板开闭（录制开关接线）

		/// <summary>
		/// 面板启用：开启 Hotfix 侧录制（面板打开才录制、关闭即停，收敛运行时开销）
		/// </summary>
		protected override void OnPanelEnabled()
		{
			LoadIgnoredMessages();
			SetMonitorEnabled(true);
		}

		/// <summary>
		/// 面板禁用：关闭 Hotfix 侧录制
		/// </summary>
		protected override void OnPanelDisabled()
		{
			SetMonitorEnabled(false);
		}

		/// <summary>
		/// 反射设置录制开关（反射未就绪时静默跳过——OnEnable 早于首次 OnGUI，此处先尝试初始化）
		/// </summary>
		private void SetMonitorEnabled(bool enabled)
		{
			if (m_setEnabledMethod == null && !EnsureReflection()) return;
			m_setEnabledMethod?.Invoke(null, new object[] { enabled });
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_monitorType != null) return true;

			m_monitorType = HotfixReflection.NetworkMonitor;
			if (m_monitorType == null) return false;

			m_recordType = m_monitorType.Assembly.GetType("Hotfix.Framework.Network.NetworkMessageRecord");
			if (m_recordType == null) return false;

			const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static;
			const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

			// NetworkMonitor 静态成员
			m_enabledProperty        = m_monitorType.GetProperty("Enabled", StaticFlags);
			m_setEnabledMethod       = m_monitorType.GetMethod("SetEnabled", StaticFlags);
			m_isPausedProperty       = m_monitorType.GetProperty("IsPaused", StaticFlags);
			m_getSnapshotMethod      = m_monitorType.GetMethod("GetSnapshot", StaticFlags);
			m_clearMethod            = m_monitorType.GetMethod("Clear", StaticFlags);
			m_resendMethod           = m_monitorType.GetMethod("Resend", StaticFlags);

			// 触发断线在 NetworkModule（监控分部）上：Instance 属性取实例后调 internal 方法
			m_moduleType             = HotfixReflection.NetworkModule;
			m_moduleInstanceProperty = m_moduleType?.GetProperty("Instance", StaticFlags);
			m_triggerDisconnectMethod = m_moduleType?.GetMethod("TriggerDisconnectForMonitor", BindingFlags.NonPublic | BindingFlags.Instance);

			// NetworkMessageRecord 实例成员
			m_recSeqProperty           = m_recordType.GetProperty("Seq", InstanceFlags);
			m_recKindProperty          = m_recordType.GetProperty("Kind", InstanceFlags);
			m_recTimeUtcProperty       = m_recordType.GetProperty("TimeUtc", InstanceFlags);
			m_recUniqueIdProperty      = m_recordType.GetProperty("UniqueId", InstanceFlags);
			m_recMessageIdProperty     = m_recordType.GetProperty("MessageId", InstanceFlags);
			m_recMessageNameProperty   = m_recordType.GetProperty("MessageName", InstanceFlags);
			m_recStatusProperty        = m_recordType.GetProperty("Status", InstanceFlags);
			m_recElapsedMsProperty     = m_recordType.GetProperty("ElapsedMs", InstanceFlags);
			m_recBodySizeProperty      = m_recordType.GetProperty("BodySize", InstanceFlags);
			m_recBodyBytesProperty     = m_recordType.GetProperty("BodyBytes", InstanceFlags);
			m_recBodyTruncatedProperty = m_recordType.GetProperty("BodyTruncated", InstanceFlags);
			m_recJsonSnapshotProperty  = m_recordType.GetProperty("JsonSnapshot", InstanceFlags);
			m_recIsResendProperty      = m_recordType.GetProperty("IsResend", InstanceFlags);

			return m_getSnapshotMethod != null && m_recSeqProperty != null;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_monitorType               = null;
			m_recordType                = null;
			m_enabledProperty           = null;
			m_setEnabledMethod          = null;
			m_isPausedProperty          = null;
			m_getSnapshotMethod         = null;
			m_clearMethod               = null;
			m_resendMethod              = null;
			m_triggerDisconnectMethod   = null;
			m_moduleType                = null;
			m_moduleInstanceProperty    = null;
			m_recSeqProperty            = null;
			m_recKindProperty           = null;
			m_recTimeUtcProperty        = null;
			m_recUniqueIdProperty       = null;
			m_recMessageIdProperty      = null;
			m_recMessageNameProperty    = null;
			m_recStatusProperty         = null;
			m_recElapsedMsProperty      = null;
			m_recBodySizeProperty       = null;
			m_recBodyBytesProperty      = null;
			m_recBodyTruncatedProperty  = null;
			m_recJsonSnapshotProperty   = null;
			m_recIsResendProperty       = null;
			m_cachedRecords             = Array.Empty<object>();
			m_displayRecords.Clear();
		}

		#endregion

		#region 记录读取辅助（装箱实例的属性反射读取）

		/// <summary>读取记录序号</summary>
		private long RecSeq(object record) => (long)(m_recSeqProperty?.GetValue(record) ?? 0L);

		/// <summary>读取记录类型（Kind 枚举转字符串）</summary>
		private string RecKind(object record) => m_recKindProperty?.GetValue(record)?.ToString() ?? "";

		/// <summary>读取记录时刻</summary>
		private DateTime RecTime(object record) => (DateTime)(m_recTimeUtcProperty?.GetValue(record) ?? DateTime.MinValue);

		/// <summary>读取消息唯一号</summary>
		private int RecUniqueId(object record) => (int)(m_recUniqueIdProperty?.GetValue(record) ?? 0);

		/// <summary>读取消息 ID</summary>
		private int RecMessageId(object record) => (int)(m_recMessageIdProperty?.GetValue(record) ?? 0);

		/// <summary>读取消息类型名</summary>
		private string RecMessageName(object record) => m_recMessageNameProperty?.GetValue(record) as string ?? "";

		/// <summary>读取状态（Status 枚举转字符串）</summary>
		private string RecStatus(object record) => m_recStatusProperty?.GetValue(record)?.ToString() ?? "";

		/// <summary>读取耗时（毫秒）</summary>
		private long RecElapsedMs(object record) => (long)(m_recElapsedMsProperty?.GetValue(record) ?? 0L);

		/// <summary>读取包体大小</summary>
		private int RecBodySize(object record) => (int)(m_recBodySizeProperty?.GetValue(record) ?? 0);

		/// <summary>读取包体字节</summary>
		private byte[] RecBodyBytes(object record) => m_recBodyBytesProperty?.GetValue(record) as byte[];

		/// <summary>读取包体截断标记</summary>
		private bool RecBodyTruncated(object record) => (bool)(m_recBodyTruncatedProperty?.GetValue(record) ?? false);

		/// <summary>读取 JSON 快照</summary>
		private string RecJson(object record) => m_recJsonSnapshotProperty?.GetValue(record) as string ?? "";

		/// <summary>读取重发标记</summary>
		private bool RecIsResend(object record) => (bool)(m_recIsResendProperty?.GetValue(record) ?? false);

		/// <summary>
		/// 是否异常记录（超时/取消）
		/// </summary>
		private static bool IsErrorStatus(string status) => status is "Timeout" or "Canceled" or "Error";

		#endregion

		#region 数据刷新与过滤

		/// <summary>
		/// 刷新动作：拉取快照缓存并重建过滤显示列表
		/// </summary>
		protected override void OnRefresh()
		{
			if (m_getSnapshotMethod == null) return;

			m_cachedRecords = m_getSnapshotMethod.Invoke(null, null) as object[] ?? Array.Empty<object>();
			CountTabRecords();
			RebuildDisplayRecords();
		}

		/// <summary>
		/// 统计各页签的消息计数（页签按钮上的数字；一次遍历产出全部计数）
		/// </summary>
		private void CountTabRecords()
		{
			Array.Clear(m_tabCounts, 0, m_tabCounts.Length);
			foreach (var record in m_cachedRecords)
			{
				if (record == null) continue;

				m_tabCounts[(int)ETab.All]++;
				switch (RecKind(record))
				{
					case "Request":   m_tabCounts[(int)ETab.Request]++;   break;
					case "Response":  m_tabCounts[(int)ETab.Response]++;  break;
					case "Notify":    m_tabCounts[(int)ETab.Notify]++;    break;
					case "HeartBeat": m_tabCounts[(int)ETab.HeartBeat]++; break;
				}

				if (IsErrorStatus(RecStatus(record))) m_tabCounts[(int)ETab.Error]++;
			}
		}

		/// <summary>
		/// 重建过滤显示列表（Tab + 搜索 + 忽略 三重过滤，保持快照的从新到旧顺序）
		/// </summary>
		private void RebuildDisplayRecords()
		{
			m_displayRecords.Clear();

			var search = m_searchFilter;
			var hasSearch = !string.IsNullOrEmpty(search);

			foreach (var record in m_cachedRecords)
			{
				if (record == null) continue;

				var kind  = RecKind(record);
				var status = RecStatus(record);

				// 页签过滤
				switch (m_currentTab)
				{
					case ETab.Request:   if (kind != "Request") continue;   break;
					case ETab.Response:  if (kind != "Response") continue;  break;
					case ETab.Notify:    if (kind != "Notify") continue;    break;
					case ETab.HeartBeat: if (kind != "HeartBeat") continue; break;
					case ETab.Error:     if (!IsErrorStatus(status)) continue; break;
				}

				// 忽略过滤（仅影响显示）
				if (m_ignoredMessageIds.Contains(RecMessageId(record))) continue;

				// 搜索过滤：消息名 / 消息号 / UniqueId 任一包含即命中（忽略大小写）
				if (hasSearch)
				{
					var messageIdStr = RecMessageId(record).ToString();
					var uniqueIdStr  = RecUniqueId(record).ToString();
					if (!RecMessageName(record).Contains(search, StringComparison.OrdinalIgnoreCase)
						&& !messageIdStr.Contains(search, StringComparison.Ordinal)
						&& !uniqueIdStr.Contains(search, StringComparison.Ordinal))
					{
						continue;
					}
				}

				m_displayRecords.Add(record);
			}
		}

		#endregion

		#region 概览与主体绘制

		/// <summary>
		/// 绘制概览区：页签 + 统计摘要 + 重发总开关
		/// </summary>
		protected override void DrawOverview()
		{
			// 页签（高亮当前页；按钮上直接带消息计数）
			EditorGUILayout.BeginHorizontal();
			DrawTabButton("全部",     ETab.All);
			DrawTabButton("请求",     ETab.Request);
			DrawTabButton("响应",     ETab.Response);
			DrawTabButton("推送",     ETab.Notify);
			DrawTabButton("心跳",     ETab.HeartBeat);
			DrawTabButton("错误",     ETab.Error);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制单个页签按钮（标签带消息计数，如"请求 2"）
		/// </summary>
		private void DrawTabButton(string label, ETab tab)
		{
			var isActive = m_currentTab == tab;
			var style = isActive ? EditorStyles.toolbarButton : EditorStyles.miniButton;
			var oldColor = GUI.backgroundColor;
			if (isActive) GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
			if (GUILayout.Button($"{label} {m_tabCounts[(int)tab]}", style, GUILayout.Width(70)))
			{
				m_currentTab = tab;
				RebuildDisplayRecords();
			}

			GUI.backgroundColor = oldColor;
		}

		/// <summary>
		/// 绘制主体：虚拟化消息列表 + 详情区（基类滚动容器内调用）
		/// </summary>
		protected override void DrawContent()
		{
			if (m_rowStyle == null)
			{
				m_rowStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					alignment = TextAnchor.MiddleLeft,
					padding   = new RectOffset(4, 4, 0, 0),
					fontSize  = EditorStyles.miniLabel.fontSize + 2, // 列表字号调大 2 号（随 DPI 缩放基准）
				};
				m_headerStyle = new GUIStyle(m_rowStyle);
			}

			DrawVirtualizedList();
		}

		#endregion

		#region 虚拟化列表

		/// <summary>
		/// 虚拟化绘制消息列表：只绘制可见行（IMGUI 手工 Rect 布局，按滚动量推算首行索引）
		/// </summary>
		private void DrawVirtualizedList()
		{
			if (m_displayRecords.Count == 0)
			{
				EditorGUILayout.HelpBox("暂无消息记录（确认已连接服务器并产生了收发）", MessageType.Info);
				return;
			}

			// ExpandHeight 占满基类滚动容器的剩余空间（数据不足时也撑满，数据多时按行高滚动）
			var viewRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			// 内容高度 = 表头一行 + 数据行（表头占据内容坐标第 0 行位置，数据从第 1 行起画）
			var contentHeight = Mathf.Max(viewRect.height, (m_displayRecords.Count + 1) * RowHeight);
			var contentRect = new Rect(0, 0, viewRect.width - 16f, contentHeight);

			m_listScroll = GUI.BeginScrollView(viewRect, m_listScroll, contentRect);

			// 表头行（占据内容坐标第 0 行）
			DrawRowHeader(new Rect(0, 0, contentRect.width, RowHeight));

			// 可见数据行：滚动量折算数据索引时扣除表头那一行；数据索引从 0 起（此前 Max(1,...) 会漏画索引 0）
			var firstLine = Mathf.Max(0, (int)(m_listScroll.y / RowHeight) - 1);
			var visibleLines = Mathf.CeilToInt(viewRect.height / RowHeight) + 2;
			var lastLine = Mathf.Min(m_displayRecords.Count, firstLine + visibleLines);

			for (var i = firstLine; i < lastLine; i++)
			{
				var rowRect = new Rect(0, (i + 1) * RowHeight, contentRect.width, RowHeight); // +1 让出表头行
				DrawRow(i, rowRect);
			}

			GUI.EndScrollView();
		}

		/// <summary>
		/// 绘制表头行（固定浅灰白，显式定色不与主题叠加）
		/// </summary>
		private void DrawRowHeader(Rect rect)
		{
			m_headerStyle.normal.textColor = new Color(0.75f, 0.78f, 0.82f);

			var x = rect.x;
			for (var i = 0; i < HeaderLabels.Length; i++)
			{
				DrawCellLabel(HeaderLabels[i], ref x, rect, ColumnWidths[i], m_headerStyle);
			}
		}

		/// <summary>
		/// 绘制单个数据行（列布局与表头一致）
		/// </summary>
		private void DrawRow(int index, Rect rect)
		{
			var record = m_displayRecords[index];
			var seq    = RecSeq(record);
			var kind   = RecKind(record);
			var status = RecStatus(record);
			var isSelected = seq == m_selectedSeq;

			// 选中行底色
			if (isSelected)
			{
				var oldColor = GUI.color;
				GUI.color = SelectedRowColor;
				GUI.DrawTexture(rect, Texture2D.whiteTexture);
				GUI.color = oldColor;
			}

			// 行点击 → 选中
			var clickRect = new Rect(rect.x, rect.y, rect.width, RowHeight);
			if (UnityEngine.Event.current.type == EventType.MouseDown && clickRect.Contains(UnityEngine.Event.current.mousePosition))
			{
				m_selectedSeq = seq;
				UnityEngine.Event.current.Use();
			}

			// 类型配色（显式定色）；异常状态整体标红
			m_rowStyle.normal.textColor = IsErrorStatus(status) ? ErrorStatusColor : KindColor(kind);

			// 行文本容器复用（避免每帧每行分配数组）
			if (m_rowCells == null) m_rowCells = new string[8];
			m_rowCells[0] = seq.ToString();
			m_rowCells[1] = RecTime(record).ToLocalTime().ToString("HH:mm:ss.fff");
			m_rowCells[2] = KindLabel(kind);
			m_rowCells[3] = RecMessageName(record);
			m_rowCells[4] = RecUniqueId(record).ToString();
			// 状态概念只属于请求（等待→终结）；响应/推送/心跳在列表里可见即已终结，显示 "-"
			m_rowCells[5] = kind == "Request" ? StatusLabel(status) : "-";
			var elapsed = RecElapsedMs(record);
			m_rowCells[6] = elapsed > 0 ? $"{elapsed}ms" : "-";
			m_rowCells[7] = $"{RecBodySize(record)}B";

			var x = rect.x;
			for (var i = 0; i < ColumnWidths.Length; i++)
			{
				DrawCellLabel(m_rowCells[i], ref x, rect, ColumnWidths[i]);
			}
		}

		/// <summary>
		/// 在指定列宽内绘制文本（x 推进；默认用行样式，可传表头样式）
		/// </summary>
		private void DrawCellLabel(string text, ref float x, Rect rowRect, float width, GUIStyle style = null)
		{
			var cellRect = width > 0
				? new Rect(x, rowRect.y, width, rowRect.height)
				: new Rect(x, rowRect.y, rowRect.width - (x - rowRect.x), rowRect.height);
			GUI.Label(cellRect, text, style ?? m_rowStyle);
			x += width > 0 ? width : 8f;
		}

		/// <summary>
		/// 消息类型的显示色
		/// </summary>
		private static Color KindColor(string kind)
		{
			switch (kind)
			{
				case "Request":   return RequestTextColor;
				case "Response":  return ResponseTextColor;
				case "Notify":    return NotifyTextColor;
				case "HeartBeat": return HeartBeatTextColor;
				default:          return Color.white;
			}
		}

		/// <summary>
		/// 消息类型的中文短标签
		/// </summary>
		private static string KindLabel(string kind)
		{
			switch (kind)
			{
				case "Request":   return "请求";
				case "Response":  return "响应";
				case "Notify":    return "推送";
				case "HeartBeat": return "心跳";
				default:          return kind;
			}
		}

		/// <summary>
		/// 状态的中文短标签
		/// </summary>
		private static string StatusLabel(string status)
		{
			switch (status)
			{
				case "Sent":     return "待回";
				case "Replied":  return "已回";
				case "Timeout":  return "超时";
				case "Canceled": return "取消";
				case "Error":    return "错误";
				default:         return status;
			}
		}

		#endregion

		#region 底部固定操作条

		/// <summary>
		/// 查找当前选中的记录（未选中返回 null）
		/// </summary>
		private object FindSelectedRecord()
		{
			if (m_selectedSeq < 0) return null;

			foreach (var record in m_displayRecords)
			{
				if (RecSeq(record) == m_selectedSeq) return record;
			}

			return null;
		}

		/// <summary>
		/// 窗口底部固定操作条：选中记录后的四个操作按钮（基座滚动容器之外，不随列表滚动）
		/// </summary>
		protected override void DrawBottomBar()
		{
			var selected = FindSelectedRecord();
			if (selected == null) return;

			var json = RecJson(selected);
			var body = RecBodyBytes(selected);
			var title = $"{RecMessageName(selected)}({RecUniqueId(selected)})";

			// 底条按钮高度 36px，用标准按钮样式拉伸不变形
			var barOptions = new[] { GUILayout.Height(36f) };

			EditorGUILayout.BeginHorizontal();

			// 选中摘要（按钮之前）：以真 Button 呈现（空回调不可用）——渲染机制与四按钮完全一致，对齐/高度天然相同；
			// 左对齐 + textColor 随选中消息的类型色
			if (m_selectedSummaryStyle == null)
			{
				m_selectedSummaryStyle = new GUIStyle(GUI.skin.button)
				{
					alignment = TextAnchor.MiddleLeft,
					padding   = new RectOffset(10, 10, 0, 0),
				};
			}
			m_selectedSummaryStyle.normal.textColor = IsErrorStatus(RecStatus(selected))
				? ErrorStatusColor
				: KindColor(RecKind(selected));
			if (GUILayout.Button($"选中--{RecMessageName(selected)}", m_selectedSummaryStyle, barOptions))
			{
				// 空回调：仅作展示，不可用
			}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(json)))
			{
				if (GUILayout.Button("查看 JSON 结构", GUI.skin.button, barOptions))
				{
					NetworkJsonViewWindow.Show(json, title);
				}
			}

			using (new EditorGUI.DisabledScope(body == null))
			{
				if (GUILayout.Button(RecBodyTruncated(selected) ? "查看二进制数据(已截断)" : "查看二进制数据", GUI.skin.button, barOptions))
				{
					NetworkHexViewWindow.Show(body, title);
				}
			}

			// 仅请求可重发（Editor 开发工具默认可用，无总开关）
			using (new EditorGUI.DisabledScope(RecKind(selected) != "Request"))
			{
				if (GUILayout.Button("再次发送", GUI.skin.button, barOptions))
				{
					// 重发窗口复用主面板的反射缓存传参（避免窗口内重复反射初始化）
					NetworkResendWindow.Show(m_resendMethod, RecSeq(selected), title, json);
				}
			}

			// 忽略此消息号（仅影响显示）
			if (GUILayout.Button("忽略此消息号", GUI.skin.button, barOptions))
			{
				if (m_ignoredMessageIds.Add(RecMessageId(selected)))
				{
					SaveIgnoredMessages();
					RebuildDisplayRecords();
				}
			}

			// 选中摘要（与列表列重复的信息不重复展示，只留定位提示）
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region 工具栏扩展（暂停/清空）

		/// <summary>
		/// 工具栏自定义按钮：暂停/继续 + 清空（在基类「刷新」按钮前绘制）
		/// </summary>
		protected override void DrawToolbarExtraButtons()
		{
			// 触发断线（测试断线重连）：Lost 语义断开 → ReconnectManager 自动重连编排，无需真实停服
			if (GUILayout.Button("触发断线", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				if (EditorUtility.DisplayDialog("触发断线",
					    "将模拟一次意外断线（等同服务器崩溃或断网），随后自动进入重连流程。\n\n确定触发？",
					    "确定", "取消"))
				{
					TriggerDisconnect();
				}
			}

			var paused = (bool)(m_isPausedProperty?.GetValue(null) ?? false);
			var newPaused = GUILayout.Toggle(paused, paused ? "已暂停" : "暂停", EditorStyles.toolbarButton, GUILayout.Width(70));
			if (newPaused != paused) m_isPausedProperty?.SetValue(null, newPaused);

			// 清除忽略（有忽略项才显示）：恢复被忽略消息号的显示
			if (m_ignoredMessageIds.Count > 0 && GUILayout.Button($"清除忽略({m_ignoredMessageIds.Count})", EditorStyles.toolbarButton, GUILayout.Width(100)))
			{
				m_ignoredMessageIds.Clear();
				SaveIgnoredMessages();
				RebuildDisplayRecords();
			}

			if (GUILayout.Button("清空记录", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				m_clearMethod?.Invoke(null, null);
				m_cachedRecords    = Array.Empty<object>();
				m_displayRecords.Clear();
				m_selectedSeq      = -1;
			}
		}

		#endregion

		#region 触发断线

		/// <summary>
		/// 反射触发 Lost 断线（NetworkModule.TriggerDisconnectForMonitor）
		/// </summary>
		private void TriggerDisconnect()
		{
			var instance = m_moduleInstanceProperty?.GetValue(null);
			if (instance == null || m_triggerDisconnectMethod == null)
			{
				EditorUtility.DisplayDialog("触发失败", "未能反射到 NetworkModule 实例（Hotfix 未加载或未连接），详见控制台。", "知道了");
				return;
			}

			m_triggerDisconnectMethod.Invoke(instance, null);
		}

		#endregion

		#region 忽略列表持久化

		/// <summary>
		/// 从 EditorPrefs 读出忽略消息号列表
		/// </summary>
		private void LoadIgnoredMessages()
		{
			m_ignoredMessageIds.Clear();
			var raw = EditorPrefs.GetString(IgnoredMessagesKey, string.Empty);
			if (string.IsNullOrEmpty(raw)) return;

			foreach (var part in raw.Split(','))
			{
				if (int.TryParse(part, out var id)) m_ignoredMessageIds.Add(id);
			}
		}

		/// <summary>
		/// 把忽略消息号列表写入 EditorPrefs（逗号分隔）
		/// </summary>
		private void SaveIgnoredMessages()
		{
			var sb = new StringBuilder();
			foreach (var id in m_ignoredMessageIds)
			{
				if (sb.Length > 0) sb.Append(',');
				sb.Append(id);
			}

			EditorPrefs.SetString(IgnoredMessagesKey, sb.ToString());
		}

		#endregion
	}
}
#endif
