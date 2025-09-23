using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Scene.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Editor
{
    /// <summary>
    /// 自定义场景管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(GameSceneManager))]
    internal sealed class SceneGameComponentInspector : FuFrameworkInspector
    {
        private GameSceneManager m_SceneManager;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            m_SceneManager = target as GameSceneManager;
            if (m_SceneManager == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("所有已加载的场景名称：", GetSceneNameString(m_SceneManager.GetAllLoadedSceneAssetPaths()));
            EditorGUILayout.LabelField("正在加载的场景名称：", GetSceneNameString(m_SceneManager.GetAllLoadingSceneAssetPaths()));
            EditorGUILayout.LabelField("正在卸载的场景名称：", GetSceneNameString(m_SceneManager.GetAllUnloadingSceneAssetPaths()));
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
                sceneNameString += m_SceneManager?.GetSceneName(sceneAssetName);
            }

            return sceneNameString;
        }
    }
}