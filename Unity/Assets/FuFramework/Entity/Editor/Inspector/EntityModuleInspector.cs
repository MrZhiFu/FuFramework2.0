using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Entity.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Editor
{
    /// <summary>
    /// 自定义实体组件的Inspector
    /// </summary>
    [CustomEditor(typeof(EntityModule))]
    internal sealed class EntityModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (target is not EntityModule module) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("实体组数量：", module.EntityGroupCount.ToString());
                EditorGUILayout.LabelField("实体总数量：", module.EntityCount.ToString());

                EditorGUILayout.Space(20);

                EntityGroup[] entityGroups = module.GetAllEntityGroups();
                foreach (EntityGroup entityGroup in entityGroups)
                {
                    EditorGUILayout.LabelField($"实体组({entityGroup.Name})", entityGroup.EntityCount.ToString());
                }
            }
        }
    }
}