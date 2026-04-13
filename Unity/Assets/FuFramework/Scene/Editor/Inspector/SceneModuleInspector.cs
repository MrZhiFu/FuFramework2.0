using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Scene.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Editor
{
    /// <summary>
    /// 场景管理模块的Inspector
    /// </summary>
    [CustomEditor(typeof(SceneModule))]
    internal sealed class SceneModuleInspector : FuFrameworkInspector
    {
        private SceneModule m_SceneModule;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            m_SceneModule = target as SceneModule;
            if (m_SceneModule == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("已加载的场景名称：", GetSceneNameString(m_SceneModule.GetAllLoadedSceneAssetPaths()));
            EditorGUILayout.LabelField("正在加载的场景名称：", GetSceneNameString(m_SceneModule.GetAllLoadingSceneAssetPaths()));
            EditorGUILayout.LabelField("正在卸载的场景名称：", GetSceneNameString(m_SceneModule.GetAllUnloadingSceneAssetPaths()));
        }

        /// <summary>
        /// 获取场景名称字符串
        /// </summary>
        /// <param name="sceneAssetNames"></param>
        /// <returns></returns>
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
    }
}