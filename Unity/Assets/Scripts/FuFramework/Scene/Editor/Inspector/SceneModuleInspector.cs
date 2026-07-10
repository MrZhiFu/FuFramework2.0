using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Editor
{
    /// <summary>
    /// 场景管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(SceneModule))]
    internal sealed class SceneModuleInspector : FuFrameworkInspector
    {
        // TODO: 后续考虑使用单独的调试界面去显示这些数据
        /*
        private SceneModule m_SceneModule;
        */

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            serializedObject.Update();

            m_SceneModule = target as SceneModule;
            if (m_SceneModule == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("已加载的场景名称：", GetSceneNameString(m_SceneModule.GetAllLoadedSceneAssetPaths()));
            EditorGUILayout.LabelField("正在加载的场景名称：", GetSceneNameString(m_SceneModule.GetAllLoadingSceneAssetPaths()));
            EditorGUILayout.LabelField("正在卸载的场景名称：", GetSceneNameString(m_SceneModule.GetAllUnloadingSceneAssetPaths()));
            */

            /*
            private string GetSceneNameString(string[] sceneAssetNames)
            {
                if (sceneAssetNames is not { Length: > 0 }) return "<Empty>";

                var sceneNameString = string.Empty;
                foreach (var sceneAssetName in sceneAssetNames)
                {
                    if (!string.IsNullOrEmpty(sceneNameString)) sceneNameString += ", ";
                    sceneNameString += m_SceneModule?.GetSceneName(sceneAssetName);
                }

                return sceneNameString;
            }
            */
        }
    }
}
