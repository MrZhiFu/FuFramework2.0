using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
    /// <summary>
    /// 网络管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(NetworkModule))]
    internal sealed class NetworkModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            if (target is not NetworkModule module) return;
            ...
            */
        }
    }
}
