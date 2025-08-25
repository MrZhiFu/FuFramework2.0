using FuFramework.Core.Editor;
using FuFramework.Web.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Editor
{
    /// <summary>
    /// 自定义Web组件的Inspector
    /// </summary>
    [CustomEditor(typeof(WebComponent))]
    internal sealed class WebGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_Timeout;

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IWebManager));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            var webComp = target as WebComponent;
            if (webComp == null) return;
            
            var timeout = EditorGUILayout.Slider("Timeout", m_Timeout.floatValue, 0f, 120f);
            if (!Mathf.Approximately(timeout, m_Timeout.floatValue))
            {
                if (EditorApplication.isPlaying)
                    webComp.Timeout = timeout;
                else
                    m_Timeout.floatValue = timeout;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void Enable()
        {
            base.Enable();

            m_Timeout = serializedObject.FindProperty("m_Timeout");
            serializedObject.ApplyModifiedProperties();
        }
    }
}