using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Editor
{
    /// <summary>
    /// 自定义实体管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(EntityModule))]
    internal sealed class EntityModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
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
            */
        }
    }
}
