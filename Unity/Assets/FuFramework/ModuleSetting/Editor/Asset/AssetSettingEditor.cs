#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 资源配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(AssetSetting))]
    public class AssetSettingEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PlayMode;              // 资源运行模式
        private SerializedProperty m_AssetPackagesProperty; // 所有资源包列表属性

        private bool[] m_PackageFoldouts;                // 资源包折叠状态数组
        private bool   m_ShowTools;                      // 是否显示工具区域
        private string m_NewPackageName = "New Package"; // 新资源包名称
        private bool   m_NewPackageIsDefault;            // 新包是否为默认包

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_PlayMode              = serializedObject.FindProperty("m_PlayMode");
            m_AssetPackagesProperty = serializedObject.FindProperty("m_AssetPackages");
            UpdateFoldoutState();
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var assetSetting = target as AssetSetting;
            if (!assetSetting) return;

            // 显示资源运行模式属性（使用枚举弹出菜单）
            EditorGUI.BeginChangeCheck();
            var playMode = (EPlayMode)m_PlayMode.enumValueIndex;
            playMode = (EPlayMode)EditorGUILayout.EnumPopup(new GUIContent("运行模式"), playMode);
            if (EditorGUI.EndChangeCheck())
            {
                m_PlayMode.enumValueIndex = (int)playMode;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"资源包管理(数量: {assetSetting.Count})", EditorStyles.boldLabel);

            // 显示默认包信息
            var defaultPackage = assetSetting.DefaultPackage;
            if (defaultPackage != null)
            {
                EditorGUILayout.HelpBox($"默认包: {defaultPackage.PackageName}", MessageType.Info);
            }

            EditorGUILayout.Space(5);
            DisplayAssetPackagesList(assetSetting); // 显示资源包列表

            EditorGUILayout.Space(20);
            DisplayToolsArea(assetSetting); // 工具区域

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 显示资源包列表
        /// </summary>
        /// <param name="setting">资源配置</param>
        private void DisplayAssetPackagesList(AssetSetting setting)
        {
            if (m_AssetPackagesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("没有资源包，点击下方按钮添加", MessageType.Info);
                return;
            }

            for (var i = 0; i < m_AssetPackagesProperty.arraySize; i++)
            {
                var packageProperty = m_AssetPackagesProperty.GetArrayElementAtIndex(i);
                if (packageProperty != null)
                {
                    DisplayAssetPackage(i, packageProperty, setting);
                }
            }
        }

        /// <summary>
        /// 显示单个资源包
        /// </summary>
        private void DisplayAssetPackage(int index, SerializedProperty packageProperty, AssetSetting setting)
        {
            var packageNameProperty = packageProperty.FindPropertyRelative("m_PackageName");
            var isDefaultProperty   = packageProperty.FindPropertyRelative("m_IsDefaultPackage");
            var downloadUrlProperty = packageProperty.FindPropertyRelative("m_DownloadURL");
            var fallbackUrlProperty = packageProperty.FindPropertyRelative("m_FallbackDownloadURL");

            // 确保折叠状态数组足够大
            if (m_PackageFoldouts == null || m_PackageFoldouts.Length <= index)
                UpdateFoldoutState();

            if (m_PackageFoldouts is null) return;

            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();
            var isDefault = isDefaultProperty.boolValue;
            var title     = isDefault ? $"{packageNameProperty.stringValue} (默认)" : packageNameProperty.stringValue;

            m_PackageFoldouts[index] = EditorGUILayout.Foldout(m_PackageFoldouts[index], title, true);
            GUILayout.FlexibleSpace();

            // 设置为默认包按钮
            if (!isDefault && GUILayout.Button("设为默认", GUILayout.Width(60)))
            {
                setting.SetDefaultPackage(packageNameProperty.stringValue);
                EditorUtility.SetDirty(setting);
                serializedObject.Update();
            }

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemoveAssetPackage(setting, index);
                return;
            }

            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (m_PackageFoldouts[index])
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(packageNameProperty, new GUIContent("包名称"));
                EditorGUILayout.PropertyField(isDefaultProperty,   new GUIContent("是否为默认包"));
                EditorGUILayout.PropertyField(downloadUrlProperty, new GUIContent("下载地址"));
                EditorGUILayout.PropertyField(fallbackUrlProperty, new GUIContent("备用下载地址"));

                EditorGUILayout.Space(5);

                // 重置按钮
                if (GUILayout.Button("重置设置"))
                {
                    ResetAssetPackage(packageProperty);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 显示工具区域
        /// </summary>
        private void DisplayToolsArea(AssetSetting setting)
        {
            m_ShowTools = EditorGUILayout.Foldout(m_ShowTools, "工具", true);
            if (m_ShowTools)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("添加新资源包", EditorStyles.boldLabel);
                m_NewPackageName      = EditorGUILayout.TextField("包名称", m_NewPackageName);
                m_NewPackageIsDefault = EditorGUILayout.Toggle("设为默认包", m_NewPackageIsDefault);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("+ 添加资源包", GUILayout.Height(30)))
                {
                    AddNewAssetPackage(setting, m_NewPackageName, m_NewPackageIsDefault);
                    m_NewPackageName      = "New Package";
                    m_NewPackageIsDefault = false;
                }

                if (GUILayout.Button("添加默认包", GUILayout.Height(30)))
                {
                    setting.AddDefaultPackage();
                    EditorUtility.SetDirty(setting);
                    serializedObject.Update();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                if (GUILayout.Button("清空所有资源包"))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有资源包吗？", "确定", "取消"))
                    {
                        setting.ClearPackages();
                        EditorUtility.SetDirty(setting);
                        serializedObject.Update();
                    }
                }

                if (GUILayout.Button("重置配置"))
                {
                    if (EditorUtility.DisplayDialog("确认重置", "确定要重置资源配置吗？", "确定", "取消"))
                    {
                        setting.Reset();
                        EditorUtility.SetDirty(setting);
                        serializedObject.Update();
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 添加新资源包
        /// </summary>
        private void AddNewAssetPackage(AssetSetting setting, string packageName, bool isDefault)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                EditorUtility.DisplayDialog("错误", "资源包名称不能为空", "确定");
                return;
            }

            setting.CreateNewPackage(packageName, isDefault);
            EditorUtility.SetDirty(setting);
            serializedObject.Update();
        }

        /// <summary>
        /// 移除资源包
        /// </summary>
        private void RemoveAssetPackage(AssetSetting setting, int index)
        {
            var package = setting[index];
            if (package == null) return;

            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除资源包 '{package.PackageName}' 吗？", "删除", "取消")) return;

            setting.RemovePackageAt(index);
            EditorUtility.SetDirty(setting);
            serializedObject.Update();
        }

        /// <summary>
        /// 重置资源包设置
        /// </summary>
        private void ResetAssetPackage(SerializedProperty packageProperty)
        {
            packageProperty.FindPropertyRelative("m_PackageName").stringValue         = "New Package";
            packageProperty.FindPropertyRelative("m_IsDefaultPackage").boolValue      = false;
            packageProperty.FindPropertyRelative("m_DownloadURL").stringValue         = "https://example.com/download";
            packageProperty.FindPropertyRelative("m_FallbackDownloadURL").stringValue = "https://example.com/download";
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 更新折叠状态数组
        /// </summary>
        private void UpdateFoldoutState()
        {
            m_PackageFoldouts = new bool[m_AssetPackagesProperty.arraySize];
            for (var i = 0; i < m_PackageFoldouts.Length; i++)
            {
                m_PackageFoldouts[i] = true;
            }
        }
    }
}
#endif