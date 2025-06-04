//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using GameFrameX.UI.Runtime;
using UnityEditor;

namespace GameFrameX.UI.Editor
{
    [CustomEditor(typeof(UIComponent))]
    internal sealed class UIComponentInspector : ComponentTypeComponentInspector
    {
        private SerializedProperty m_EnableOpenUIFormSuccessEvent = null;

        private SerializedProperty m_EnableOpenUIFormFailureEvent = null;

        // private SerializedProperty m_EnableOpenUIFormUpdateEvent = null;
        // private SerializedProperty m_EnableOpenUIFormDependencyAssetEvent = null;
        private SerializedProperty m_EnableCloseUIFormCompleteEvent = null;
        private SerializedProperty m_InstanceAutoReleaseInterval = null;
        private SerializedProperty m_InstanceCapacity = null;

        private SerializedProperty m_InstanceExpireTime = null;

        // private SerializedProperty m_InstancePriority = null;
        private SerializedProperty m_InstanceUGUIRoot = null;
        private SerializedProperty m_InstanceFairyGUIRoot = null;
        private SerializedProperty m_UIGroups = null;

        private HelperInfo<UIFormHelperBase> m_UIFormHelperInfo = new HelperInfo<UIFormHelperBase>("UIForm");
        private HelperInfo<UIGroupHelperBase> m_UIGroupHelperInfo = new HelperInfo<UIGroupHelperBase>("UIGroup");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            UIComponent t = (UIComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_UIFormHelperInfo.Draw();
                m_UIGroupHelperInfo.Draw();

                EditorGUILayout.HelpBox("以上的组件前缀的命名空间必须设置为一致，否则将会初始化失败", MessageType.Warning);

                EditorGUILayout.PropertyField(m_EnableOpenUIFormSuccessEvent);
                EditorGUILayout.PropertyField(m_EnableOpenUIFormFailureEvent);
                // EditorGUILayout.PropertyField(m_EnableOpenUIFormUpdateEvent);
                // EditorGUILayout.PropertyField(m_EnableOpenUIFormDependencyAssetEvent);
                EditorGUILayout.PropertyField(m_EnableCloseUIFormCompleteEvent);
            }
            EditorGUI.EndDisabledGroup();

            float instanceAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Instance Auto Release Interval", m_InstanceAutoReleaseInterval.floatValue);
            if (instanceAutoReleaseInterval != m_InstanceAutoReleaseInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceAutoReleaseInterval = instanceAutoReleaseInterval;
                }
                else
                {
                    m_InstanceAutoReleaseInterval.floatValue = instanceAutoReleaseInterval;
                }
            }

            int instanceCapacity = EditorGUILayout.DelayedIntField("Instance Capacity", m_InstanceCapacity.intValue);
            if (instanceCapacity != m_InstanceCapacity.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceCapacity = instanceCapacity;
                }
                else
                {
                    m_InstanceCapacity.intValue = instanceCapacity;
                }
            }

            float instanceExpireTime = EditorGUILayout.DelayedFloatField("Instance Expire Time", m_InstanceExpireTime.floatValue);
            if (instanceExpireTime != m_InstanceExpireTime.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceExpireTime = instanceExpireTime;
                }
                else
                {
                    m_InstanceExpireTime.floatValue = instanceExpireTime;
                }
            }

            /*
            int instancePriority = EditorGUILayout.DelayedIntField("Instance Priority", m_InstancePriority.intValue);
            if (instancePriority != m_InstancePriority.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstancePriority = instancePriority;
                }
                else
                {
                    m_InstancePriority.intValue = instancePriority;
                }
            }*/

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.HelpBox("设置为UGUI的根节点", MessageType.Info);
                EditorGUILayout.PropertyField(m_InstanceUGUIRoot);
                EditorGUILayout.HelpBox("设置为FairyGUI的根节点", MessageType.Info);
                EditorGUILayout.PropertyField(m_InstanceFairyGUIRoot);

                if (m_UIGroups.arraySize <= 0)
                {
                    EditorGUILayout.HelpBox("必须要设置至少一个UIGroup", MessageType.Error);
                }

                EditorGUILayout.HelpBox("强烈推荐不要设置为同一个Depth(深度)", MessageType.Info);
                EditorGUILayout.PropertyField(m_UIGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("UI Group Count", t.UIGroupCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        protected override void Enable()
        {
            m_EnableOpenUIFormSuccessEvent = serializedObject.FindProperty("m_EnableOpenUIFormSuccessEvent");
            m_EnableOpenUIFormFailureEvent = serializedObject.FindProperty("m_EnableOpenUIFormFailureEvent");
            // m_EnableOpenUIFormUpdateEvent = serializedObject.FindProperty("m_EnableOpenUIFormUpdateEvent");
            // m_EnableOpenUIFormDependencyAssetEvent = serializedObject.FindProperty("m_EnableOpenUIFormDependencyAssetEvent");
            m_EnableCloseUIFormCompleteEvent = serializedObject.FindProperty("m_EnableCloseUIFormCompleteEvent");
            m_InstanceAutoReleaseInterval = serializedObject.FindProperty("m_InstanceAutoReleaseInterval");
            m_InstanceCapacity = serializedObject.FindProperty("m_InstanceCapacity");
            m_InstanceExpireTime = serializedObject.FindProperty("m_InstanceExpireTime");
            // m_InstancePriority = serializedObject.FindProperty("m_InstancePriority");
            m_InstanceUGUIRoot = serializedObject.FindProperty("m_InstanceUGUIRoot");
            m_InstanceFairyGUIRoot = serializedObject.FindProperty("m_InstanceFairyGUIRoot");
            m_UIGroups = serializedObject.FindProperty("m_UIGroups");

            m_UIFormHelperInfo.Init(serializedObject);
            m_UIGroupHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IUIManager));
            m_UIFormHelperInfo.Refresh();
            m_UIGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}