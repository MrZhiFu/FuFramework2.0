using FuFramework.Core.Editor;
using FuFramework.Sound.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Editor
{
    [CustomEditor(typeof(SoundComponent))]
    internal sealed class SoundGameComponentInspector : GameComponentInspector
    {
        // private SerializedProperty m_EnablePlaySoundUpdateEvent = null;
        // private SerializedProperty m_EnablePlaySoundDependencyAssetEvent = null;
        private SerializedProperty m_InstanceRoot;
        private SerializedProperty m_AudioMixer;
        private SerializedProperty m_SoundGroups;

        private readonly HelperInfo<SoundGroupHelperBase> m_SoundGroupHelperInfo = new("SoundGroup");
        private readonly HelperInfo<SoundAgentHelperBase> m_SoundAgentHelperInfo = new("SoundAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                // EditorGUILayout.PropertyField(m_EnablePlaySoundUpdateEvent);
                // EditorGUILayout.PropertyField(m_EnablePlaySoundDependencyAssetEvent);
                EditorGUILayout.PropertyField(m_InstanceRoot);
                EditorGUILayout.PropertyField(m_AudioMixer);
                m_SoundGroupHelperInfo.Draw();
                m_SoundAgentHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_SoundGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            var soundComp = (SoundComponent)target;
            if (EditorApplication.isPlaying && IsPrefabInHierarchy(soundComp.gameObject))
            {
                EditorGUILayout.LabelField("声音组数量：", soundComp.SoundGroupCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ISoundManager));
            m_SoundGroupHelperInfo.Refresh();
            m_SoundAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void Enable()
        {
            // m_EnablePlaySoundUpdateEvent = serializedObject.FindProperty("m_EnablePlaySoundUpdateEvent");
            // m_EnablePlaySoundDependencyAssetEvent = serializedObject.FindProperty("m_EnablePlaySoundDependencyAssetEvent");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_AudioMixer   = serializedObject.FindProperty("m_AudioMixer");
            m_SoundGroups  = serializedObject.FindProperty("m_SoundGroups");

            m_SoundGroupHelperInfo.Init(serializedObject);
            m_SoundAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }
    }
}