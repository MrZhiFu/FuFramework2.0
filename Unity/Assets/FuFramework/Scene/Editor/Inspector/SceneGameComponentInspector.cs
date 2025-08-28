// using FuFramework.Core.Editor;
// using FuFramework.Scene.Runtime;
// using UnityEditor;
// using UnityEngine;
//
// // ReSharper disable once CheckNamespace
// namespace FuFramework.Scene.Editor
// {
//     /// <summary>
//     /// 自定义场景组件的Inspector
//     /// </summary>
//     [CustomEditor(typeof(SceneComponent))]
//     internal sealed class SceneGameComponentInspector : GameComponentInspector
//     {
//         private SerializedProperty m_EnableLoadSceneUpdateEvent;
//         private SerializedProperty m_EnableLoadSceneDependencyAssetEvent;
//
//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();
//
//             serializedObject.Update();
//
//             var sceneComp = (SceneComponent)target;
//
//             EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
//             {
//                 EditorGUILayout.PropertyField(m_EnableLoadSceneUpdateEvent);
//                 EditorGUILayout.PropertyField(m_EnableLoadSceneDependencyAssetEvent);
//             }
//             EditorGUI.EndDisabledGroup();
//
//             serializedObject.ApplyModifiedProperties();
//
//             if (!EditorApplication.isPlaying || !IsPrefabInHierarchy(sceneComp.gameObject)) return;
//             
//             EditorGUILayout.LabelField("Loaded Scene Asset Names", GetSceneNameString(sceneComp.GetLoadedSceneAssetNames()));
//             EditorGUILayout.LabelField("Loading Scene Asset Names", GetSceneNameString(sceneComp.GetLoadingSceneAssetNames()));
//             EditorGUILayout.LabelField("Unloading Scene Asset Names", GetSceneNameString(sceneComp.GetUnloadingSceneAssetNames()));
//             EditorGUILayout.ObjectField("Main Camera", sceneComp.MainCamera, typeof(Camera), true);
//
//             Repaint();
//         }
//
//         protected override void Enable()
//         {
//             m_EnableLoadSceneUpdateEvent = serializedObject.FindProperty("m_EnableLoadSceneUpdateEvent");
//             m_EnableLoadSceneDependencyAssetEvent = serializedObject.FindProperty("m_EnableLoadSceneDependencyAssetEvent");
//         }
//
//         protected override void RefreshTypeNames()
//         {
//             RefreshComponentTypeNames(typeof(IGameSceneManager));
//         }
//
//         private string GetSceneNameString(string[] sceneAssetNames)
//         {
//             if (sceneAssetNames is not { Length: > 0 }) return "<Empty>";
//
//             var sceneNameString = string.Empty;
//             foreach (var sceneAssetName in sceneAssetNames)
//             {
//                 if (!string.IsNullOrEmpty(sceneNameString)) sceneNameString += ", ";
//                 sceneNameString += SceneComponent.GetSceneName(sceneAssetName);
//             }
//
//             return sceneNameString;
//         }
//     }
// }