using FuFramework.Core.Editor;
using FuFramework.Sound.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Editor
{
    [CustomEditor(typeof(SoundManager))]
    internal sealed class SoundManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var soundManager = target as SoundManager;
            if (soundManager == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("声音组数量：", soundManager.SoundGroupCount.ToString());
            EditorGUILayout.Space(10);
            foreach (var group in soundManager.GetAllSoundGroups())
            {
                EditorGUILayout.LabelField($"名称：{group.Name}", $"静音：{group.Mute}\t音量：{group.Volume}");
            }
        }
    }
}