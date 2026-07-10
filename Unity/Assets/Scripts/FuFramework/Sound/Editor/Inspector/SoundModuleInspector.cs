using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Editor
{
    /// <summary>
    /// 声音管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(SoundModule))]
    internal sealed class SoundModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            if (target is not SoundModule module) return;
            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("声音组数量：", module.SoundGroupCount.ToString());
            EditorGUILayout.Space(10);
            foreach (var group in module.GetAllSoundGroups())
            {
                EditorGUILayout.LabelField($"名称：{group.Name}", $"静音：{group.Mute}\t音量：{group.Volume}");
            }
            */
        }
    }
}
