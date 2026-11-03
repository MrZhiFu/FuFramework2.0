#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
	/// <summary>
	/// 网络消息重发窗口：预填选中请求的 JSON（可编辑），确认后经 NetworkMonitor.Resend 重发到服务器。
	/// 说明：
	///     1. 重发真实发到服务器——本工具仅 Editor 环境使用，默认可用（无总开关把守）。
	///     2. 发起成功后结果（响应/超时/取消）经既有 RPC 终点自然落入记录列表（IsResend 标记），本窗口只负责发起。
	/// </summary>
	public sealed class NetworkResendWindow : EditorWindow
	{
		/// <summary>
		/// 反射缓存：NetworkMonitor.Resend 方法（主面板传入，避免重复反射初始化）
		/// </summary>
		private MethodInfo m_resendMethod;

		/// <summary>
		/// 原请求记录序号。
		/// </summary>
		private long m_seq;

		/// <summary>
		/// 目标消息显示名。
		/// </summary>
		private string m_messageName = string.Empty;

		/// <summary>
		/// 可编辑的请求 JSON。
		/// </summary>
		private string m_json = string.Empty;

		/// <summary>
		/// 滚动位置。
		/// </summary>
		private Vector2 m_scroll;

		/// <summary>
		/// 打开重发窗口
		/// </summary>
		/// <param name="resendMethod">NetworkMonitor.Resend 的 MethodInfo（主面板传入，避免重复反射初始化）</param>
		/// <param name="seq">原请求记录序号</param>
		/// <param name="messageName">消息显示名</param>
		/// <param name="json">预填 JSON</param>
		public static void Show(MethodInfo resendMethod, long seq, string messageName, string json)
		{
			var window = GetWindow<NetworkResendWindow>(true, $"再次发送 - {messageName}", true);
			window.minSize = new Vector2(560, 420);
			window.m_resendMethod = resendMethod;
			window.m_seq          = seq;
			window.m_messageName  = messageName;
			window.m_json         = json ?? string.Empty;
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("重发会真实发送到服务器（字段数值可直接修改后发出），请确认在测试环境使用。", MessageType.Warning);

			EditorGUILayout.LabelField("目标消息", m_messageName);
			EditorGUILayout.LabelField("原请求序号", m_seq.ToString());

			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("请求 JSON（可编辑）：");
			m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.ExpandHeight(true));
			m_json = EditorGUILayout.TextArea(m_json, GUILayout.ExpandHeight(true));
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(6);
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("确认发送", GUILayout.Height(28)))
			{
				TryResend();
			}

			if (GUILayout.Button("取消", GUILayout.Width(90), GUILayout.Height(28)))
			{
				Close();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 发起重发：调 NetworkMonitor.Resend，失败弹提示（原因已由其内部记日志）
		/// </summary>
		private void TryResend()
		{
			bool result;
			try
			{
				result = m_resendMethod != null && (bool)(m_resendMethod.Invoke(null, new object[] { m_seq, m_json }) ?? false);
			}
			catch (Exception e)
			{
				// 反射调用可能穿出未捕获异常（如模块未初始化的 NRE），就地兜底避免 OnGUI 事件中断（审查 M-5）
				result = false;
				UnityEngine.Debug.LogWarning($"[NetworkResendWindow] 重发调用异常：{e.InnerException?.Message ?? e.Message}");
			}

			if (result)
			{
				Close();
			}
			else
			{
				EditorUtility.DisplayDialog("重发失败", "请求未发起（原记录已滚出 / 类型未注册 / JSON 非法），详见控制台日志。", "知道了");
			}
		}
	}
}
#endif
