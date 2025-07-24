using GameFrameX.Editor;
using GameFrameX.Web.Runtime;
using UnityEditor;

namespace GameFrameX.Web.Editor
{
    [CustomEditor(typeof(WebComponent))]
    internal sealed class WebComponentInspector : ComponentTypeComponentInspector
    {
        private SerializedProperty m_Timeout = null;

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IWebManager));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            WebComponent t = (WebComponent)target;
            float timeout = EditorGUILayout.Slider("Timeout", m_Timeout.floatValue, 0f, 120f);
            if (timeout != m_Timeout.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.Timeout = timeout;
                }
                else
                {
                    m_Timeout.floatValue = timeout;
                }
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