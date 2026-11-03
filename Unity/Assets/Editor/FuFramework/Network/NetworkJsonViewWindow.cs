#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
	/// <summary>
	/// 网络 JSON 结构查看窗口：对消息的 JSON 快照做结构化折叠渲染。
	/// 支持 <c>{}</c>/<c>[]</c> 容器行首箭头点击切换折叠/展开（或点击容器整行）；键名/字符串值/数字/布尔语法着色；
	/// 展示去尾逗号、闭合符独立成行、根容器括号隐藏；支持复制全文。
	/// 说明：手写轻量行解析（按容器括号与逗号断行、括号配对），不引第三方库。
	/// </summary>
	public sealed class NetworkJsonViewWindow : EditorWindow
	{
		/// <summary>
		/// JSON 解析后的显示行。
		/// </summary>
		private sealed class JsonLine
		{
			/// <summary>缩进层级（容器嵌套深度）</summary>
			public int Depth;

			/// <summary>显示文本（不含缩进；可能含富文本着色标签）</summary>
			public string Text;

			/// <summary>是否容器开行（{ 或 [）——可折叠</summary>
			public bool IsContainer;

			/// <summary>是否闭合行（} 或 ] 独立行）</summary>
			public bool IsClose;

			/// <summary>容器开行对应的闭合行索引（-1=非容器）</summary>
			public int CloseIndex = -1;
		}

		// 语法着色（深色主题友好的配色；HTML 转义不在处理范围，值中出现尖括号时显示可能异常）
		private const string KeyColor     = "#8fd7ff"; // 键名：浅蓝
		private const string StringColor  = "#e8b87d"; // 字符串值：橙黄
		private const string NumberColor  = "#b5e8a3"; // 数字值：浅绿
		private const string LiteralColor = "#d0a8ff"; // true/false/null：淡紫
		private const string BracketColor = "#8a8a8a"; // 容器括号：灰

		/// <summary>键名着色（"xxx" 后跟冒号）</summary>
		private static readonly Regex KeyRegex = new("\"([^\"]*)\"(\\s*:)", RegexOptions.Compiled);

		/// <summary>字符串值着色（冒号后的字符串）</summary>
		private static readonly Regex StringValueRegex = new(":\\s*\"([^\"]*)\"", RegexOptions.Compiled);

		/// <summary>数字值着色（冒号后的整数/小数/负数）</summary>
		private static readonly Regex NumberValueRegex = new(":\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.Compiled);

		/// <summary>布尔/null 值着色</summary>
		private static readonly Regex LiteralValueRegex = new(":\\s*(true|false|null)", RegexOptions.Compiled);

		/// <summary>
		/// 解析后的全部行（渲染时按折叠状态跳行）。
		/// </summary>
		private readonly List<JsonLine> m_lines = new();

		/// <summary>
		/// 容器行的展开状态（以行索引为键）。
		/// </summary>
		private readonly HashSet<int> m_expandedLines = new();

		/// <summary>
		/// 原始 JSON 全文（复制用）。
		/// </summary>
		private string m_rawJson;

		/// <summary>
		/// 滚动位置。
		/// </summary>
		private Vector2 m_scroll;

		/// <summary>
		/// GUI 样式（懒初始化；richText 开启供语法着色）。
		/// </summary>
		private GUIStyle m_lineStyle;
		private GUIStyle m_foldoutLineStyle;

		/// <summary>
		/// 打开查看窗口
		/// </summary>
		/// <param name="json">JSON 全文</param>
		/// <param name="title">窗口标题（消息名+UniqueId）</param>
		public static void Show(string json, string title)
		{
			if (string.IsNullOrEmpty(json)) return;

			var window = GetWindow<NetworkJsonViewWindow>(true, $"JSON 结构 - {title}", true);
			window.minSize = new Vector2(560, 480);
			window.Load(json);
			window.Show();
		}

		/// <summary>
		/// 解析 JSON 为行列表
		/// </summary>
		private void Load(string json)
		{
			m_rawJson       = json;
			m_lines.Clear();
			m_expandedLines.Clear();
			m_scroll = Vector2.zero;

			var openStack = new Stack<int>();
			var sb        = new StringBuilder();
			var depth     = 0;
			var inString  = false;

			void FlushLine(bool isContainerOpen, bool isContainerClose, char bracket = '\0')
			{
				if (sb.Length == 0 && !isContainerOpen && !isContainerClose) return;
				var text = sb.ToString();
				sb.Length = 0;

				var line = new JsonLine { Depth = depth, Text = text };
				if (isContainerOpen)
				{
					// 开符号接在键名尾（"key": {），与内容间隔一个空格；开行可折叠
					line.IsContainer = true;
					line.Text        = text.Length == 0 ? bracket.ToString() : text.TrimEnd() + " " + bracket;
					openStack.Push(m_lines.Count);
				}
				else if (isContainerClose && openStack.Count > 0)
				{
					// 最后一个成员无逗号时其值还残留在缓冲：先独立成行——
					// 残留内容是容器内成员，层级用容器内层（此处 depth 已减一，+1 还原）
					if (text.Length > 0) m_lines.Add(new JsonLine { Depth = depth + 1, Text = text });

					var openIndex = openStack.Pop();
					m_lines[openIndex].CloseIndex = m_lines.Count;
					// 容器默认展开
					m_expandedLines.Add(openIndex);
					// 闭合符独立成行（与开行同层缩进）
					m_lines.Add(new JsonLine { Depth = depth, IsClose = true, Text = bracket.ToString() });
					return;
				}

				m_lines.Add(line);
			}

			foreach (var c in json)
			{
				if (inString)
				{
					sb.Append(c);
					if (c == '"' && !IsEscaped(sb)) inString = false;
					continue;
				}

				switch (c)
				{
					case '"':
						sb.Append(c);
						inString = true;
						break;
					case '{':
					case '[':
						// 容器开行：key: { 或独立 {
						FlushLine(true, false, c);
						depth++;
						break;
					case '}':
						// 先减层级再 Flush：闭合行缩进与开行同层
						depth = System.Math.Max(0, depth - 1);
						FlushLine(false, true, '}');
						break;
					case ']':
						depth = System.Math.Max(0, depth - 1);
						FlushLine(false, true, ']');
						break;
					case ',':
						sb.Append(c);
						FlushLine(false, false);
						break;
					default:
						sb.Append(c);
						break;
				}
			}

			FlushLine(false, false);

			// 展示规范化（着色前做纯文本操作）：逗号清理 → 语法着色
			NormalizeCommas();
			ColorizeLines();
		}

		/// <summary>
		/// 判断刚结束的引号是否被转义（检查其前是否为奇数个反斜杠）。
		/// </summary>
		private static bool IsEscaped(StringBuilder sb)
		{
			var backslashes = 0;
			for (var i = sb.Length - 2; i >= 0 && sb[i] == '\\'; i--) backslashes++;
			return backslashes % 2 == 1;
		}

		/// <summary>
		/// 逗号展示清理：行首逗号并入上一行尾（闭合行呈 "}," 形态，纯逗号行删除）；再去普通行尾逗号（闭行豁免——其逗号来自并入）。
		/// </summary>
		private void NormalizeCommas()
		{
			// 先并行首逗号（",xxx" → 上一行尾 "," + 本行 "xxx"）
			for (var i = 1; i < m_lines.Count; i++)
			{
				var line = m_lines[i];
				if (!line.Text.StartsWith(",")) continue;

				m_lines[i - 1].Text += ",";
				line.Text = line.Text.Substring(1);
				if (line.Text.Length == 0)
				{
					m_lines.RemoveAt(i);
					i--;
				}
			}

			// 再去原生普通行的尾逗号（闭合行的逗号来自并入，须保留）
			foreach (var line in m_lines)
			{
				if (!line.IsContainer && !line.IsClose && line.Text.EndsWith(",")) line.Text = line.Text[..^1];
			}
		}

		/// <summary>
		/// 语法着色：键名/字符串值/数字/布尔分色；闭合行整行灰。根容器括号不渲染无需着色。
		/// </summary>
		private void ColorizeLines()
		{
			for (var i = 1; i < m_lines.Count - 1; i++)
			{
				var line = m_lines[i];
				var text = line.Text;

				// 闭合行整行灰
				if (!line.IsContainer && (text == "}" || text == "]"))
				{
					line.Text = $"<color={BracketColor}>{text}</color>";
					continue;
				}

				// 着色顺序：键名 → 字符串值 → 数字 → 布尔（前序着色产生的标签不影响后序锚点）
				text = KeyRegex.Replace(text, $"<color={KeyColor}>\"$1\"</color>$2");
				text = StringValueRegex.Replace(text, $": <color={StringColor}>\"$1\"</color>");
				text = NumberValueRegex.Replace(text, $": <color={NumberColor}>$1</color>");
				text = LiteralValueRegex.Replace(text, $": <color={LiteralColor}>$1</color>");
				line.Text = text;
			}
		}

		private void OnGUI()
		{
			if (m_lineStyle == null)
			{
				// 行距加宽：上下 margin 各 3px
				m_lineStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					richText = true,
					fontSize = EditorStyles.miniLabel.fontSize + 3,
					margin   = new RectOffset(0, 0, 3, 3),
				};
				m_foldoutLineStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					richText  = true, // 开行文本含语法着色标签，必须开启否则标签字面显示
					fontStyle = FontStyle.Bold,
					fontSize  = EditorStyles.miniLabel.fontSize + 3,
					margin    = new RectOffset(0, 0, 3, 3),
				};
			}

			// 顶部工具行
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				for (var i = 1; i < m_lines.Count - 1; i++)
				{
					if (m_lines[i].IsContainer) m_expandedLines.Add(i);
				}
			}

			if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				m_expandedLines.RemoveWhere(i => m_lines[i].IsContainer);
			}

			if (GUILayout.Button("复制全文", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				EditorGUIUtility.systemCopyBuffer = m_rawJson;
			}

			GUILayout.FlexibleSpace();
			GUILayout.Label($"{m_lines.Count} 行", EditorStyles.miniLabel);
			EditorGUILayout.EndHorizontal();

			if (m_lines.Count == 0)
			{
				EditorGUILayout.HelpBox("无内容", MessageType.Info);
				return;
			}

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

			// 渲染循环：跳过根容器的开/闭行（index 0 与最后一行）；按折叠状态跳过隐藏区间；缩进 4 空格/层
			var index = 1;
			while (index < m_lines.Count - 1)
			{
				var line = m_lines[index];
				var indent = new GUIContent(string.Empty.PadLeft(line.Depth * 4) + line.Text);

				if (line.IsContainer)
				{
					var expanded = m_expandedLines.Contains(index);
					var oldColor = GUI.color;
					GUI.color = new Color(0.6f, 0.85f, 1f);
					var arrow = expanded ? "▼ " : "▶ ";
					var clicked = GUILayout.Button(arrow + line.Text, m_foldoutLineStyle);
					GUI.color = oldColor;
					if (clicked)
					{
						if (expanded) m_expandedLines.Remove(index);
						else m_expandedLines.Add(index);
					}

					index = expanded ? index + 1 : (line.CloseIndex >= 0 ? line.CloseIndex + 1 : index + 1);
				}
				else
				{
					GUILayout.Label(indent, m_lineStyle);
					index++;
				}
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
#endif
