using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.ReferencePool.Editor
{
    /// <summary>
    /// 引用池管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(ReferencePoolModule))]
    internal sealed class ReferencePoolModuleInspector : FuFrameworkInspector
    {
        // TODO: 后续考虑使用单独的调试界面去显示这些数据
        // 原始代码包含：引用池状态展示（按程序集分类、完整类名切换）、CSV导出功能
        /*
        private readonly Dictionary<string, List<ReferencePoolInfo>> m_ReferencePoolDict = new(StringComparer.Ordinal);
        private readonly HashSet<string> m_OpenedItems = new();
        private bool m_ShowFullClassName;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying) return;

            var enableStrictCheck = EditorGUILayout.Toggle("激活严格检查(打开可能会影响性能)", ReferencePoolModule.EnableStrictCheck);
            if (enableStrictCheck != ReferencePoolModule.EnableStrictCheck)
                ReferencePoolModule.EnableStrictCheck = enableStrictCheck;

            EditorGUILayout.LabelField("引用池个数", ReferencePool.Runtime.ReferencePool.Count.ToString());
            m_ShowFullClassName = EditorGUILayout.Toggle("是否显示完整类名", m_ShowFullClassName);
            ...
        }
        */
    }
}
