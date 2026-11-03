#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
	/// <summary>
	/// 网络二进制数据查看窗口：纯 Hex 视图（每行 16 字节，第 8/9 字节半行分隔），支持复制纯 Hex 文本。
	/// 说明：预格式化全部行后整窗滚动，包体上限 64KB（由监控采集层截断），极端情况下行数可控。
	/// 字符串内容不在此处展示（Protobuf 中文经 UTF-8 后多为不可打印字节）——请看 JSON 结构窗口。
	/// </summary>
	public sealed class NetworkHexViewWindow : EditorWindow
	{
		/// <summary>
		/// 每行显示的字节数。
		/// </summary>
		private const int BytesPerLine = 16;

		/// <summary>
		/// 预格式化的行文本。
		/// </summary>
		private string[] m_lines = Array.Empty<string>();

		/// <summary>
		/// 纯 Hex 文本（复制用）。
		/// </summary>
		private string m_hexText;

		/// <summary>
		/// 滚动位置。
		/// </summary>
		private Vector2 m_scroll;

		/// <summary>
		/// 行文本样式（字号与 JSON 折叠窗口一致）。GUIStyle 会被窗口序列化持久化，须 NonSerialized 防止旧样式钉死。
		/// </summary>
		[NonSerialized] private GUIStyle m_hexStyle;

		/// <summary>
		/// 打开查看窗口
		/// </summary>
		/// <param name="bytes">包体字节</param>
		/// <param name="title">窗口标题（消息名+UniqueId）</param>
		public static void Show(byte[] bytes, string title)
		{
			if (bytes == null || bytes.Length == 0) return;

			var window = GetWindow<NetworkHexViewWindow>(true, $"二进制数据 - {title}", true);
			window.minSize = new Vector2(560, 480);
			window.Load(bytes);
			window.Show();
		}

		/// <summary>
		/// 预格式化字节为行文本
		/// </summary>
		private void Load(byte[] bytes)
		{
			var lineCount = (bytes.Length + BytesPerLine - 1) / BytesPerLine;
			m_lines   = new string[lineCount];
			var hexSb = new StringBuilder(bytes.Length * 2);

			for (var line = 0; line < lineCount; line++)
			{
				var offset = line * BytesPerLine;
				var count  = Math.Min(BytesPerLine, bytes.Length - offset);
				var sb     = new StringBuilder(64);

				sb.Append(offset.ToString("X8"));
				sb.Append("  ");

				for (var i = 0; i < BytesPerLine; i++)
				{
					if (i == BytesPerLine / 2) sb.Append("  "); // 半行分隔（第 8/9 字节之间，加宽到 2 空格）

					if (i < count)
					{
						sb.Append(bytes[offset + i].ToString("X2"));
						hexSb.Append(bytes[offset + i].ToString("X2"));
					}
					else
					{
						sb.Append("  ");
					}

					sb.Append(i == BytesPerLine - 1 ? "" : " "); // 行尾不加尾空格
				}

				m_lines[line] = sb.ToString();
				hexSb.AppendLine();
			}

			m_hexText = hexSb.ToString();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button("复制 Hex", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				EditorGUIUtility.systemCopyBuffer = m_hexText;
			}

			GUILayout.FlexibleSpace();
			GUILayout.Label($"{m_lines.Length} 行 × {BytesPerLine} B", EditorStyles.miniLabel);
			EditorGUILayout.EndHorizontal();

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.ExpandHeight(true));

			// 行文本绘制——字号与 JSON 折叠窗口一致（miniLabel+3）
			if (m_hexStyle == null)
			{
				m_hexStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = EditorStyles.miniLabel.fontSize + 3 };
			}

			foreach (var line in m_lines)
			{
				GUILayout.Label(line, m_hexStyle);
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
#endif
