using System;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor
{
	/// <summary>
	/// 模块配置Inspector。
	/// </summary>
	[CustomEditor(typeof(Runtime.GameSetting))]
	internal sealed class GameSettingInspector : UnityEditor.Editor
	{
		private SerializedProperty m_FrameRate;       // 帧率
		private SerializedProperty m_GameSpeed;       // 游戏速度
		private SerializedProperty m_RunInBackground; // 是否后台运行
		private SerializedProperty m_NeverSleep;      // 是否禁止休眠
		private SerializedProperty m_OpenGuide;       // 是否开启引导


		private SerializedProperty m_PlayMode;
		private SerializedProperty m_DefaultPackageName;
		private SerializedProperty m_DownloadingMaxNum;
		private SerializedProperty m_FailedTryAgainNum;
		private SerializedProperty m_AsyncSystemMaxSlicePerFrame;
		private SerializedProperty m_ResCdnRootURL;
		private SerializedProperty m_EnableAutoSave;
		private SerializedProperty m_AutoSaveInterval;
		private SerializedProperty m_EnableEncrypt;
		private SerializedProperty m_EncryptKey;

		private void OnEnable()
		{
			m_FrameRate                   = serializedObject.FindProperty("m_FrameRate");
			m_GameSpeed                   = serializedObject.FindProperty("m_GameSpeed");
			m_RunInBackground             = serializedObject.FindProperty("m_RunInBackground");
			m_NeverSleep                  = serializedObject.FindProperty("m_NeverSleep");
			m_OpenGuide                   = serializedObject.FindProperty("m_OpenGuide");
			m_PlayMode                    = serializedObject.FindProperty("m_PlayMode");
			m_DefaultPackageName          = serializedObject.FindProperty("m_DefaultPackageName");
			m_DownloadingMaxNum           = serializedObject.FindProperty("m_DownloadingMaxNum");
			m_FailedTryAgainNum           = serializedObject.FindProperty("m_FailedTryAgainNum");
			m_AsyncSystemMaxSlicePerFrame = serializedObject.FindProperty("m_AsyncSystemMaxSlicePerFrame");
			m_ResCdnRootURL               = serializedObject.FindProperty("m_ResCdnRootURL");
			m_EnableAutoSave              = serializedObject.FindProperty("m_EnableAutoSave");
			m_AutoSaveInterval            = serializedObject.FindProperty("m_AutoSaveInterval");
			m_EnableEncrypt               = serializedObject.FindProperty("m_EnableEncrypt");
			m_EncryptKey                  = serializedObject.FindProperty("m_EncryptKey");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (target is not Runtime.GameSetting gameSetting) return;

			// 游戏基本设置
			EditorGUILayout.LabelField("游戏基本设置", EditorStyles.boldLabel);

			// 帧率
			ApplyEdit(m_FrameRate, EditorGUILayout.IntSlider("帧率设置：", m_FrameRate.intValue, 1, 120), gameSetting, (s, v) => s.FrameRate = v);

			// 游戏速度
			ApplyEdit(m_GameSpeed, EditorGUILayout.Slider("游戏速度设置：", m_GameSpeed.floatValue, 0f, 8f), gameSetting, (s, v) => s.GameSpeed = v);

			// 设置是否后台运行
			ApplyEdit(m_RunInBackground, EditorGUILayout.Toggle("是否可在后台运行", m_RunInBackground.boolValue), gameSetting, (s, v) => s.RunInBackground = v);

			// 设置是否禁止休眠
			ApplyEdit(m_NeverSleep, EditorGUILayout.Toggle("是否禁止休眠", m_NeverSleep.boolValue), gameSetting, (s, v) => s.NeverSleep = v);

			// 设置是否开启引导
			ApplyEdit(m_OpenGuide, EditorGUILayout.Toggle("是否开启引导", m_OpenGuide.boolValue), gameSetting, (s, v) => s.OpenGuide = v);

			// 资源系统配置
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("资源系统配置", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_PlayMode,                    new GUIContent("资源运行模式"));
			EditorGUILayout.PropertyField(m_DefaultPackageName,          new GUIContent("默认资源包名称"));
			EditorGUILayout.PropertyField(m_DownloadingMaxNum,           new GUIContent("下载最大并发数量"));
			EditorGUILayout.PropertyField(m_FailedTryAgainNum,           new GUIContent("下载失败重试次数"));
			EditorGUILayout.PropertyField(m_AsyncSystemMaxSlicePerFrame, new GUIContent("异步系统每帧最大时间切片（毫秒）"));
			EditorGUILayout.PropertyField(m_ResCdnRootURL,               new GUIContent("资源CDN根地址"));

			// 本地数据存储系统配置
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("本地数据存储系统配置", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_EnableAutoSave,   new GUIContent("是否自动保存"));
			EditorGUILayout.PropertyField(m_AutoSaveInterval, new GUIContent("自动保存间隔（秒）"));
			EditorGUILayout.PropertyField(m_EnableEncrypt,    new GUIContent("是否加密"));
			EditorGUILayout.PropertyField(m_EncryptKey,       new GUIContent("加密密钥"));

			serializedObject.ApplyModifiedProperties();
		}

		#region 字段修改写入

		/// <summary>
		/// 应用整型修改：Play 模式额外写运行时属性即时生效；序列化属性统一回写，
		/// 保证控件回显（Play 中的序列化写入不落盘，停止 Play 后随编辑器回滚）。
		/// </summary>
		private static void ApplyEdit(SerializedProperty prop, int uiValue, Runtime.GameSetting gameSetting, Action<Runtime.GameSetting, int> playApply)
		{
			if (uiValue == prop.intValue) return;

			if (EditorApplication.isPlaying)
			{
				playApply(gameSetting, uiValue);
			}

			prop.intValue = uiValue;
		}

		/// <summary>
		/// 应用浮点修改：Play 模式额外写运行时属性即时生效；序列化属性统一回写，
		/// 保证控件回显（Play 中的序列化写入不落盘，停止 Play 后随编辑器回滚）。
		/// </summary>
		private static void ApplyEdit(SerializedProperty prop, float uiValue, Runtime.GameSetting gameSetting, Action<Runtime.GameSetting, float> playApply)
		{
			if (Mathf.Approximately(uiValue, prop.floatValue)) return;

			if (EditorApplication.isPlaying)
			{
				playApply(gameSetting, uiValue);
			}

			prop.floatValue = uiValue;
		}

		/// <summary>
		/// 应用布尔修改：Play 模式额外写运行时属性即时生效；序列化属性统一回写，
		/// 保证控件回显（Play 中的序列化写入不落盘，停止 Play 后随编辑器回滚）。
		/// </summary>
		private static void ApplyEdit(SerializedProperty prop, bool uiValue, Runtime.GameSetting gameSetting, Action<Runtime.GameSetting, bool> playApply)
		{
			if (uiValue == prop.boolValue) return;

			if (EditorApplication.isPlaying)
			{
				playApply(gameSetting, uiValue);
			}

			prop.boolValue = uiValue;
		}

		#endregion
	}
}