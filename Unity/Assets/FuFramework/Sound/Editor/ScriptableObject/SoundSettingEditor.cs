#if UNITY_EDITOR
using FuFramework.Core.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Editor
{
    /// <summary>
    /// 声音配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(SoundSetting))]
    public class SoundSettingEditor : GameComponentInspector
    {
        private SerializedProperty m_SoundGroupsProperty; // 所有声音组列表属性

        private bool[] m_GroupFoldouts;                    // 声音组折叠状态数组，true表示展开，false表示折叠
        private bool   m_ShowTools    = true;              // 是否显示工具区域
        private string m_NewGroupName = "New Sound Group"; // 新声音组名称

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_SoundGroupsProperty = serializedObject.FindProperty("m_SoundGroups");
            UpdateFoldoutState();
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var soundSetting = (SoundSetting)target;

            EditorGUILayout.LabelField($"声音组管理(数量: {soundSetting.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            DisplaySoundGroupsList(soundSetting);// 显示声音组列表

            EditorGUILayout.Space(20);
            DisplayToolsArea(soundSetting);// 工具区域

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 显示声音组列表
        /// </summary>
        /// <param name="setting">声音配置</param>
        private void DisplaySoundGroupsList(SoundSetting setting)
        {
            if (m_SoundGroupsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("没有声音组，点击下方按钮添加", MessageType.Info);
                return;
            }

            for (var i = 0; i < m_SoundGroupsProperty.arraySize; i++)
            {
                var groupProperty = m_SoundGroupsProperty.GetArrayElementAtIndex(i);
                if (groupProperty != null)
                {
                    DisplaySoundGroup(i, groupProperty, setting);
                }
            }
        }

        /// <summary>
        /// 显示单个声音组
        /// </summary>
        /// <param name="index">声音组索引</param>
        /// <param name="groupProperty">声音组序列化属性</param>
        /// <param name="setting">声音配置</param>
        private void DisplaySoundGroup(int index, SerializedProperty groupProperty, SoundSetting setting)
        {
            var nameProperty         = groupProperty.FindPropertyRelative("m_GroupName");
            var muteProperty         = groupProperty.FindPropertyRelative("m_IsMute");
            var volumeProperty       = groupProperty.FindPropertyRelative("m_Volume");
            var agentCountProperty   = groupProperty.FindPropertyRelative("m_AgentHelperCount");
            var avoidReplaceProperty = groupProperty.FindPropertyRelative("m_AllowBeingReplacedBySamePriority");

            // 确保折叠状态数组足够大
            if (m_GroupFoldouts == null || m_GroupFoldouts.Length <= index)
                UpdateFoldoutState();

            if (m_GroupFoldouts is null) return;

            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();

            m_GroupFoldouts[index] = EditorGUILayout.Foldout(m_GroupFoldouts[index], nameProperty.stringValue, true);

            GUILayout.FlexibleSpace();

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemoveSoundGroup(setting, index);
                return; // 退出，因为数组大小改变了
            }

            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (m_GroupFoldouts[index])
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(nameProperty,         new GUIContent("名称"));
                EditorGUILayout.PropertyField(muteProperty,         new GUIContent("静音"));
                EditorGUILayout.PropertyField(volumeProperty,       new GUIContent("音量"));
                EditorGUILayout.PropertyField(agentCountProperty,   new GUIContent("播放代理数量"));
                EditorGUILayout.PropertyField(avoidReplaceProperty, new GUIContent("允许同优先级替换"));

                EditorGUILayout.Space(5);

                // 重置按钮
                if (GUILayout.Button("重置设置"))
                {
                    ResetSoundGroup(groupProperty);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 显示工具区域
        /// </summary>
        /// <param name="setting">声音配置</param>
        private void DisplayToolsArea(SoundSetting setting)
        {
            m_ShowTools = EditorGUILayout.Foldout(m_ShowTools, "工具", true);
            if (m_ShowTools)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("添加新声音组", EditorStyles.boldLabel);
                m_NewGroupName = EditorGUILayout.TextField("组名称", m_NewGroupName);

                EditorGUILayout.BeginHorizontal();

                // + 号按钮 - 添加新声音组
                if (GUILayout.Button("+ 添加声音组", GUILayout.Height(30)))
                {
                    AddNewSoundGroup(setting, m_NewGroupName);
                    m_NewGroupName = "New Sound Group";
                }

                // 添加默认组按钮
                if (GUILayout.Button("添加默认组", GUILayout.Height(30)))
                {
                    AddDefaultSoundGroups(setting);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 清空按钮
                if (GUILayout.Button("清空所有声音组"))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有声音组吗？", "确定", "取消"))
                    {
                        ClearAllSoundGroups(setting);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 添加新声音组
        /// </summary>
        /// <param name="setting">声音配置</param>
        /// <param name="groupName">声音组名称</param>
        private void AddNewSoundGroup(SoundSetting setting, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                EditorUtility.DisplayDialog("错误", "声音组名称不能为空", "确定");
                return;
            }

            // 在集合中创建新组
            var newGroup = setting.CreateNewSoundGroup(groupName);
            if (newGroup == null) return;
            
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已创建声音组: {groupName}");
        }

        /// <summary>
        /// 添加默认声音组
        /// </summary>
        /// <param name="setting">声音配置</param>
        private void AddDefaultSoundGroups(SoundSetting setting)
        {
            string[] defaultGroups = { "BGM", "SFX", "UI"};

            foreach (var groupName in defaultGroups)
            {
                if (setting.ContainsGroup(groupName)) continue;
                setting.CreateNewSoundGroup(groupName);
            }

            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log("已添加默认声音组");
        }

        /// <summary>
        /// 移除声音组
        /// </summary>
        /// <param name="setting">声音配置</param>
        /// <param name="index">要移除的声音组索引</param>
        private void RemoveSoundGroup(SoundSetting setting, int index)
        {
            var groupName = setting[index]?.Name ?? "未知";

            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除声音组 '{groupName}' 吗？", "删除", "取消")) return;
            
            setting.RemoveGroupAt(index);
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已删除声音组: {groupName}");
        }

        /// <summary>
        /// 重置声音组设置
        /// </summary>
        /// <param name="groupProperty">声音组序列化属性</param>
        private void ResetSoundGroup(SerializedProperty groupProperty)
        {
            groupProperty.FindPropertyRelative("m_Mute").boolValue                             = false;
            groupProperty.FindPropertyRelative("m_Volume").floatValue                          = 1f;
            groupProperty.FindPropertyRelative("m_AgentHelperCount").intValue                  = 1;
            groupProperty.FindPropertyRelative("m_AvoidBeingReplacedBySamePriority").boolValue = false;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 清空所有声音组
        /// </summary>
        /// <param name="setting">声音配置</param>
        private void ClearAllSoundGroups(SoundSetting setting)
        {
            setting.ClearGroups();
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log("已清空所有声音组");
        }

        /// <summary>
        /// 更新折叠状态数组
        /// </summary>
        private void UpdateFoldoutState()
        {
            m_GroupFoldouts = new bool[m_SoundGroupsProperty.arraySize];
            for (var i = 0; i < m_GroupFoldouts.Length; i++)
            {
                m_GroupFoldouts[i] = true; // 默认展开
            }
        }

        /// <summary>
        /// 刷新组件类型名称数组
        /// </summary>
        protected override void RefreshTypeNames()
        {
            // Nothing
        }
    }
}
#endif