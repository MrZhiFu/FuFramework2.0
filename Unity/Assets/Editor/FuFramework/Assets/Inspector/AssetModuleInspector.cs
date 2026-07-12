// TODO: 后续考虑使用单独的调试界面去显示模块数据

using FuFramework.Core.Editor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Editor
{
    /// <summary>
    /// 自定义资源管理模块组件的Inspector
    /// </summary>
    // [CustomEditor(typeof(AssetModule))]
    internal sealed class AssetModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            serializedObject.Update();

            if (target is not AssetModule module) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("资源运行模式：",     module.PlayMode.ToString());
                EditorGUILayout.LabelField("默认资源包名称：",    module.DefaultPackageName);
                EditorGUILayout.LabelField("资源下载最大并发数量：", module.DownloadingMaxNum.ToString());
                EditorGUILayout.LabelField("资源下载失败重试次数：", module.FailedTryAgainNum.ToString());
            }

            Repaint();
            */
        }
    }
}