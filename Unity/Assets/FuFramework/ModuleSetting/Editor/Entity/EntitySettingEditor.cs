#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    /// <summary>
    /// 实体配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(EntitySetting))]
    public class EntitySettingEditor : UnityEditor.Editor
    {
        private SerializedProperty m_EntityGroupsProperty; // 所有实体组列表属性

        private bool[] m_GroupFoldouts;                    // 实体组折叠状态数组，true表示展开，false表示折叠
        private bool   m_ShowTools;                        // 是否显示工具区域
        private string m_NewGroupName = "New Entity Group"; // 新实体组名称

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_EntityGroupsProperty = serializedObject.FindProperty("m_EntityGroups");
            UpdateFoldoutState();
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var entitySetting = target as EntitySetting;
            if (!entitySetting) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"实体组管理(数量: {entitySetting.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DisplayEntityGroupsList(entitySetting); // 显示实体组列表

            EditorGUILayout.Space(20);
            DisplayToolsArea(entitySetting); // 工具区域

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 显示实体组列表
        /// </summary>
        /// <param name="setting">实体配置</param>
        private void DisplayEntityGroupsList(EntitySetting setting)
        {
            if (m_EntityGroupsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("没有实体组，点击下方按钮添加", MessageType.Info);
                return;
            }

            for (var i = 0; i < m_EntityGroupsProperty.arraySize; i++)
            {
                var groupProperty = m_EntityGroupsProperty.GetArrayElementAtIndex(i);
                if (groupProperty != null)
                {
                    DisplayEntityGroup(i, groupProperty, setting);
                }
            }
        }

        /// <summary>
        /// 显示单个实体组
        /// </summary>
        /// <param name="index">实体组索引</param>
        /// <param name="groupProperty">实体组序列化属性</param>
        /// <param name="setting">实体配置</param>
        private void DisplayEntityGroup(int index, SerializedProperty groupProperty, EntitySetting setting)
        {
            var nameProperty = groupProperty.FindPropertyRelative("m_Name");
            var autoReleaseIntervalProperty = groupProperty.FindPropertyRelative("m_InstanceAutoReleaseInterval");
            var capacityProperty = groupProperty.FindPropertyRelative("m_InstanceCapacity");
            var expireTimeProperty = groupProperty.FindPropertyRelative("m_InstanceExpireTime");
            var priorityProperty = groupProperty.FindPropertyRelative("m_InstancePriority");

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
                RemoveEntityGroup(setting, index);
                return; // 退出，因为数组大小改变了
            }

            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (m_GroupFoldouts[index])
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(nameProperty, new GUIContent("名称"));
                EditorGUILayout.PropertyField(capacityProperty, new GUIContent("对象池容量"));
                EditorGUILayout.PropertyField(expireTimeProperty, new GUIContent("对象过期时间(秒)"));
                EditorGUILayout.PropertyField(autoReleaseIntervalProperty, new GUIContent("自动释放间隔(秒)"));
                EditorGUILayout.PropertyField(priorityProperty, new GUIContent("对象优先级"));

                EditorGUILayout.Space(5);

                // 重置按钮
                if (GUILayout.Button("重置设置"))
                {
                    ResetEntityGroup(groupProperty);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// 显示工具区域
        /// </summary>
        /// <param name="setting">实体配置</param>
        private void DisplayToolsArea(EntitySetting setting)
        {
            m_ShowTools = EditorGUILayout.Foldout(m_ShowTools, "工具", true);
            if (m_ShowTools)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("添加新实体组", EditorStyles.boldLabel);
                m_NewGroupName = EditorGUILayout.TextField("组名称", m_NewGroupName);

                EditorGUILayout.BeginHorizontal();

                // + 号按钮 - 添加新实体组
                if (GUILayout.Button("+ 添加实体组", GUILayout.Height(30)))
                {
                    AddNewEntityGroup(setting, m_NewGroupName);
                    m_NewGroupName = "New Entity Group";
                }

                // 添加默认组按钮
                if (GUILayout.Button("添加默认组", GUILayout.Height(30)))
                {
                    AddDefaultEntityGroups(setting);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 清空按钮
                if (GUILayout.Button("清空所有实体组"))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有实体组吗？", "确定", "取消"))
                    {
                        ClearAllEntityGroups(setting);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 添加新实体组
        /// </summary>
        /// <param name="setting">实体配置</param>
        /// <param name="groupName">实体组名称</param>
        private void AddNewEntityGroup(EntitySetting setting, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                EditorUtility.DisplayDialog("错误", "实体组名称不能为空", "确定");
                return;
            }

            // 在集合中创建新组
            var newGroup = setting.CreateNewEntityGroup(groupName);
            if (newGroup == null) return;

            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已创建实体组: {groupName}");
        }

        /// <summary>
        /// 添加默认实体组
        /// </summary>
        /// <param name="setting">实体配置</param>
        private void AddDefaultEntityGroups(EntitySetting setting)
        {
            string[] defaultGroups = { "Player", "Enemy", "NPC", "Item", "Effect" };

            foreach (var groupName in defaultGroups)
            {
                if (setting.ContainsGroup(groupName)) continue;
                setting.CreateNewEntityGroup(groupName);
            }

            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log("已添加默认实体组");
        }

        /// <summary>
        /// 移除实体组
        /// </summary>
        /// <param name="setting">实体配置</param>
        /// <param name="index">要移除的实体组索引</param>
        private void RemoveEntityGroup(EntitySetting setting, int index)
        {
            var groupName = setting[index]?.Name ?? "未知";

            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除实体组 '{groupName}' 吗？", "删除", "取消")) return;

            setting.RemoveGroupAt(index);
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已删除实体组: {groupName}");
        }

        /// <summary>
        /// 重置实体组设置
        /// </summary>
        /// <param name="groupProperty">实体组序列化属性</param>
        private void ResetEntityGroup(SerializedProperty groupProperty)
        {
            groupProperty.FindPropertyRelative("m_InstanceAutoReleaseInterval").floatValue = 60f;
            groupProperty.FindPropertyRelative("m_InstanceCapacity").intValue = 16;
            groupProperty.FindPropertyRelative("m_InstanceExpireTime").floatValue = 60f;
            groupProperty.FindPropertyRelative("m_InstancePriority").intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 清空所有实体组
        /// </summary>
        /// <param name="setting">实体配置</param>
        private void ClearAllEntityGroups(EntitySetting setting)
        {
            setting.ClearGroups();
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log("已清空所有实体组");
        }

        /// <summary>
        /// 更新折叠状态数组
        /// </summary>
        private void UpdateFoldoutState()
        {
            m_GroupFoldouts = new bool[m_EntityGroupsProperty.arraySize];
            for (var i = 0; i < m_GroupFoldouts.Length; i++)
            {
                m_GroupFoldouts[i] = true; // 默认展开
            }
        }
    }
}
#endif