using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Entity.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Editor
{
    /// <summary>
    /// 自定义实体组件的Inspector
    /// </summary>
    [CustomEditor(typeof(EntityManager))]
    internal sealed class EntityGameComponentInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var entityComp = target as EntityManager;
            if (!entityComp) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("实体组数量：",   entityComp.EntityGroupCount.ToString());
                EditorGUILayout.LabelField("实体总数量：", entityComp.EntityCount.ToString());
                EditorGUILayout.Space(20);
                EntityGroup[] entityGroups = entityComp.GetAllEntityGroups();
                foreach (EntityGroup entityGroup in entityGroups)
                {
                    EditorGUILayout.LabelField($"实体组({entityGroup.Name})", entityGroup.EntityCount.ToString());
                }
            }
        }
    }
}