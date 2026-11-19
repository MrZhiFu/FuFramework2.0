#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Editor
{
	/// <summary>
	/// 本地化调试面板。
	/// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 LocalizationModule。
	/// 临时验证入口（用户裁定）：设置界面语言切换 UI 后续项目再做，本面板仅用于验证 SetLanguageAsync 闭环。
	/// 功能：
	///     1. 展示当前语言与目标语种资源就绪状态（按 tag 查询下载量）。
	///     2. 15 个语种切换按钮：经反射调用 SetLanguageAsync 异步切换（下载→重填→广播），切换期间按钮置灰。
	///     3. 最近一次切换结果提示（成功/失败/取消），切换后旧语言数据释放、全 UI 经事件重译。
	/// </summary>
	public sealed class LocalizationModuleWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/本地化调试面板", false, FuMenuPriority.DEBUG_PANEL_LOCALIZATION)]
		public static void ShowWindow()
		{
			var window = GetWindow<LocalizationModuleWindow>("本地化调试");
			window.minSize = new Vector2(420, 480);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "LocalizationModule";

		#endregion

		#region 私有字段

		/// <summary>
		/// 本地化模块类型（Hotfix 程序集）
		/// </summary>
		private System.Type m_moduleType;

		/// <summary>
		/// 本地化模块实例
		/// </summary>
		private object m_moduleInstance;

		/// <summary>
		/// 语言属性（Instance.Language，反射读当前语言）
		/// </summary>
		private PropertyInfo m_languageProperty;

		/// <summary>
		/// SetLanguageAsync 方法（反射调用异步切换）
		/// </summary>
		private MethodInfo m_setLanguageAsyncMethod;

		/// <summary>
		/// ELanguage 枚举类型（Hotfix 程序集，反射取值）
		/// </summary>
		private System.Type m_languageEnumType;

		/// <summary>
		/// 15 语种枚举值缓存（Enum.GetValues 排序后数组，索引即枚举值）
		/// </summary>
		private Array m_languageValues;

		/// <summary>
		/// 语种显示名缓存（枚举名，按钮文案）
		/// </summary>
		private string[] m_languageNames;

		/// <summary>
		/// 是否有切换正在进行（期间按钮置灰，UniTask 完成回调复位）
		/// </summary>
		private bool m_switching;

		/// <summary>
		/// 最近一次切换结果提示（空 = 无记录）
		/// </summary>
		private string m_lastResult = string.Empty;

		/// <summary>
		/// 最近一次结果是否成功（提示颜色用）
		/// </summary>
		private bool m_lastResultOk;

		#endregion

		/// <summary>
		/// 绘制主体：当前语言 + 切换按钮 + 结果提示
		/// </summary>
		protected override void DrawContent()
		{
			// 当前语言
			var currentLanguage = ReadCurrentLanguage();
			EditorGUILayout.LabelField("当前语言", currentLanguage, GUILayout.MinHeight(22f));

			// 切换进行中提示
			using (new EditorGUI.DisabledScope(m_switching))
			{
				EditorGUILayout.Space(6f);
				EditorGUILayout.LabelField("切换语言（SetLanguageAsync：下载→重填表→广播事件）", EditorStyles.boldLabel);

				// 15 语种按钮网格（4 列）
				const int columns = 4;
				for (var i = 0; i < m_languageNames.Length; i++)
				{
					if (i % columns == 0) EditorGUILayout.BeginHorizontal();

					var isCurrent = m_languageNames[i] == currentLanguage;
					if (isCurrent)
					{
						// 当前语言按钮置灰（无操作意义）
						using (new EditorGUI.DisabledScope(true))
						{
							EditorGUILayout.LabelField(m_languageNames[i], GUI.skin.button, GUILayout.Height(24f));
						}
					}
					else if (GUILayout.Button(m_languageNames[i], GUILayout.Height(24f)))
					{
						StartSwitch(i);
					}

					if (i % columns == columns - 1 || i == m_languageNames.Length - 1) EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.Space(8f);

			// 最近一次切换结果
			if (m_lastResult.Length > 0)
			{
				var style = new GUIStyle(EditorStyles.helpBox) { normal = { textColor = m_lastResultOk ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f) } };
				EditorGUILayout.LabelField(m_lastResult, style, GUILayout.MinHeight(30f));
			}
		}

		/// <summary>
		/// 发起一次异步切换（反射调 SetLanguageAsync，Forget 不阻塞 GUI，完成回调更新结果与置灰状态）
		/// </summary>
		private void StartSwitch(int languageIndex)
		{
			if (m_switching || m_setLanguageAsyncMethod == null || m_moduleInstance == null) return;

			var languageValue = m_languageValues.GetValue(languageIndex);
			m_switching   = true;
			m_lastResult  = $"正在切换到 {m_languageNames[languageIndex]} ...（必要时下载语种资源）";
			m_lastResultOk = true;
			try
			{
				var task = m_setLanguageAsyncMethod.Invoke(m_moduleInstance, new[] { languageValue, CancellationToken.None });
				var taskType = task.GetType();
				// Hotfix 程序集为热更域（Editor 静态引用不到其类型），Forget 扩展按类型实例所在程序集定位
				// （UniTask<bool> 实例的 typeof 所在程序集即 UniTask），再调 Forget(UniTask<bool>) 扩展
				var forgetMethod = taskType.Assembly.GetType("Cysharp.Threading.Tasks.UniTaskExtensions")
					?.GetMethod("Forget", BindingFlags.Public | BindingFlags.Static, null, new[] { taskType }, null);
				if (forgetMethod == null)
				{
					m_lastResult  = $"切换调用失败：Forget 扩展未找到（{taskType.Name}）";
					m_lastResultOk = false;
					m_switching    = false;
					return;
				}

				forgetMethod.Invoke(null, new[] { task });
				m_pendingLanguageIndex = languageIndex;
			}
			catch (Exception e)
			{
				m_lastResult  = $"切换调用异常：{e.Message}";
				m_lastResultOk = false;
				m_switching    = false;
			}
		}

		/// <summary>
		/// 待感知完成的切换目标索引（-1 = 无）；完成判定 = 当前语言变化或结果回滚
		/// </summary>
		private int m_pendingLanguageIndex = -1;

		/// <summary>
		/// 自动刷新节拍：感知切换完成/失败（语言变化 → 完成；长时间无变化 → 由用户重试）
		/// </summary>
		protected override void OnRefresh()
		{
			if (m_pendingLanguageIndex < 0) return;

			var currentLanguage = ReadCurrentLanguage();
			if (currentLanguage == m_languageNames[m_pendingLanguageIndex])
			{
				m_lastResult  = $"切换到 {currentLanguage} 成功（表已重填，全 UI 已重译）";
				m_lastResultOk = true;
				m_switching           = false;
				m_pendingLanguageIndex = -1;
			}
		}

		/// <summary>
		/// 反射读当前语言（Instance.Language 的枚举名）
		/// </summary>
		/// <returns>当前语言枚举名；反射不可用时返回占位</returns>
		private string ReadCurrentLanguage()
		{
			if (m_languageProperty == null || m_moduleInstance == null) return "<不可用>";
			var value = m_languageProperty.GetValue(m_moduleInstance);
			return value?.ToString() ?? "<不可用>";
		}

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_moduleInstance != null) return true;

			m_moduleType = HotfixReflection.LocalizationModule;
			if (m_moduleType == null) return false;

			m_moduleInstance = HotfixReflection.GetStaticInstance(m_moduleType);
			if (m_moduleInstance == null) return false;

			m_languageProperty      = m_moduleType.GetProperty("Language", BindingFlags.Public | BindingFlags.Instance);
			m_setLanguageAsyncMethod = m_moduleType.GetMethod("SetLanguageAsync", BindingFlags.Public | BindingFlags.Instance);
			var languagePropertyType = m_languageProperty?.PropertyType;
			if (languagePropertyType == null || !languagePropertyType.IsEnum) return false;

			m_languageEnumType = languagePropertyType;
			m_languageValues   = Enum.GetValues(m_languageEnumType);
			m_languageNames    = Enum.GetNames(m_languageEnumType);

			return m_setLanguageAsyncMethod != null && m_languageValues.Length > 0;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_moduleType            = null;
			m_moduleInstance        = null;
			m_languageProperty      = null;
			m_setLanguageAsyncMethod = null;
			m_languageEnumType      = null;
			m_languageValues        = null;
			m_languageNames         = null;
			m_switching             = false;
			m_pendingLanguageIndex  = -1;
			m_lastResult            = string.Empty;
		}
	}
}
#endif
