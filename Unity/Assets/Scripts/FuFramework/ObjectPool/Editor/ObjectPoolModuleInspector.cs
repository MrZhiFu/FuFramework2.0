using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Editor
{
    /// <summary>
    /// 对象池管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(ObjectPoolModule))]
    internal sealed class ObjectPoolModuleInspector : FuFrameworkInspector
    {
        // TODO: 后续考虑使用单独的调试界面去显示这些数据
        // 原始代码包含：对象池状态展示（按程序集分类、完整类名切换）、CSV导出功能
        /*
        private readonly Dictionary<string, List<ObjectPoolInfo>> m_ObjectPoolDict = new(StringComparer.Ordinal);
        private readonly HashSet<string> m_OpenedItems = new();
        private bool m_ShowFullClassName;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is not ObjectPoolModule module) return;
            ...
        }
        */
    }
}
