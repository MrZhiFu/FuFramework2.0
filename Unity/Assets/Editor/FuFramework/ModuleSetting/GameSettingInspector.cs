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
		private SerializedProperty m_frameRate;       // 帧率
		private SerializedProperty m_gameSpeed;       // 游戏速度
		private SerializedProperty m_runInBackground; // 是否后台运行
		private SerializedProperty m_neverSleep;      // 是否禁止休眠
		private SerializedProperty m_openGuide;       // 是否开启引导


		private SerializedProperty m_playMode;
		private SerializedProperty m_defaultPackageName;
		private SerializedProperty m_downloadingMaxNum;
		private SerializedProperty m_failedTryAgainNum;
		private SerializedProperty m_asyncSystemMaxSlicePerFrame;
		private SerializedProperty m_resCdnRootURL;
		private SerializedProperty m_enableAutoSave;
		private SerializedProperty m_autoSaveInterval;
		private SerializedProperty m_enableEncrypt;
		private SerializedProperty m_encryptKey;

		private void OnEnable()
		{
			m_frameRate                   = serializedObject.FindProperty("m_frameRate");
			m_gameSpeed                   = serializedObject.FindProperty("m_gameSpeed");
			m_runInBackground             = serializedObject.FindProperty("m_runInBackground");
			m_neverSleep                  = serializedObject.FindProperty("m_neverSleep");
			m_openGuide                   = serializedObject.FindProperty("m_openGuide");
			m_playMode                    = serializedObject.FindProperty("m_playMode");
			m_defaultPackageName          = serializedObject.FindProperty("m_defaultPackageName");
			m_downloadingMaxNum           = serializedObject.FindProperty("m_downloadingMaxNum");
			m_failedTryAgainNum           = serializedObject.FindProperty("m_failedTryAgainNum");
			m_asyncSystemMaxSlicePerFrame = serializedObject.FindProperty("m_asyncSystemMaxSlicePerFrame");
			m_resCdnRootURL               = serializedObject.FindProperty("m_resCdnRootURL");
			m_enableAutoSave              = serializedObject.FindProperty("m_enableAutoSave");
			m_autoSaveInterval            = serializedObject.FindProperty("m_autoSaveInterval");
			m_enableEncrypt               = serializedObject.FindProperty("m_enableEncrypt");
			m_encryptKey                  = serializedObject.FindProperty("m_encryptKey");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (target is not Runtime.GameSetting gameSetting) return;

			// 游戏基本设置
			EditorGUILayout.LabelField("游戏基本设置", EditorStyles.boldLabel);

			// 帧率
			ApplyEdit(m_frameRate, EditorGUILayout.IntSlider("帧率设置：", m_frameRate.intValue, 1, 120), gameSetting, (s, v) => s.FrameRate = v);

			// 游戏速度
			ApplyEdit(m_gameSpeed, EditorGUILayout.Slider("游戏速度设置：", m_gameSpeed.floatValue, 0f, 8f), gameSetting, (s, v) => s.GameSpeed = v);

			// 设置是否后台运行
			ApplyEdit(m_runInBackground, EditorGUILayout.Toggle("是否可在后台运行", m_runInBackground.boolValue), gameSetting, (s, v) => s.RunInBackground = v);

			// 设置是否禁止休眠
			ApplyEdit(m_neverSleep, EditorGUILayout.Toggle("是否禁止休眠", m_neverSleep.boolValue), gameSetting, (s, v) => s.NeverSleep = v);

			// 设置是否开启引导
			ApplyEdit(m_openGuide, EditorGUILayout.Toggle("是否开启引导", m_openGuide.boolValue), gameSetting, (s, v) => s.OpenGuide = v);

			// 资源系统配置
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("资源系统配置", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_playMode,                    new GUIContent("资源运行模式"));
			EditorGUILayout.PropertyField(m_defaultPackageName,          new GUIContent("默认资源包名称"));
			EditorGUILayout.PropertyField(m_downloadingMaxNum,           new GUIContent("下载最大并发数量"));
			EditorGUILayout.PropertyField(m_failedTryAgainNum,           new GUIContent("下载失败重试次数"));
			EditorGUILayout.PropertyField(m_asyncSystemMaxSlicePerFrame, new GUIContent("异步系统每帧最大时间切片（毫秒）"));
			EditorGUILayout.PropertyField(m_resCdnRootURL,               new GUIContent("资源CDN根地址"));

			// 本地数据存储系统配置
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("本地数据存储系统配置", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_enableAutoSave,   new GUIContent("是否自动保存"));
			EditorGUILayout.PropertyField(m_autoSaveInterval, new GUIContent("自动保存间隔（秒）"));
			EditorGUILayout.PropertyField(m_enableEncrypt,    new GUIContent("是否加密"));
			EditorGUILayout.PropertyField(m_encryptKey,       new GUIContent("加密密钥"));

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