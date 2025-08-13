using FuFramework.Core.Editor;
using FuFramework.Entity.Runtime;
using UnityEditor;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Editor
{
    [CustomEditor(typeof(EntityComponent))]
    internal sealed class EntityGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_EnableShowEntityUpdateEvent;
        private SerializedProperty m_EnableShowEntityDependencyAssetEvent;
        private SerializedProperty m_InstanceRoot;
        private SerializedProperty m_EntityGroups;

        private readonly HelperInfo<EntityHelperBase>      m_EntityHelperInfo      = new("Entity");
        private readonly HelperInfo<EntityGroupHelperBase> m_EntityGroupHelperInfo = new("EntityGroup");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var entityComp = target as EntityComponent;
            if (!entityComp) return;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnableShowEntityUpdateEvent);
                EditorGUILayout.PropertyField(m_EnableShowEntityDependencyAssetEvent);
                EditorGUILayout.PropertyField(m_InstanceRoot);
                m_EntityHelperInfo.Draw();
                m_EntityGroupHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_EntityGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(entityComp.gameObject))
            {
                EditorGUILayout.LabelField("Entity Group Count",   entityComp.EntityGroupCount.ToString());
                EditorGUILayout.LabelField("Entity Count (Total)", entityComp.EntityCount.ToString());
                IEntityGroup[] entityGroups = entityComp.GetAllEntityGroups();
                foreach (IEntityGroup entityGroup in entityGroups)
                {
                    EditorGUILayout.LabelField(Utility.Text.Format("Entity Count ({0})", entityGroup.Name), entityGroup.EntityCount.ToString());
                }
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void Enable()
        {
            m_EnableShowEntityUpdateEvent          = serializedObject.FindProperty("m_EnableShowEntityUpdateEvent");
            m_EnableShowEntityDependencyAssetEvent = serializedObject.FindProperty("m_EnableShowEntityDependencyAssetEvent");
            m_InstanceRoot                         = serializedObject.FindProperty("m_InstanceRoot");
            m_EntityGroups                         = serializedObject.FindProperty("m_EntityGroups");

            m_EntityHelperInfo.Init(serializedObject);
            m_EntityGroupHelperInfo.Init(serializedObject);

            m_EntityHelperInfo.Refresh();
            m_EntityGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IEntityManager));
        }
    }
}