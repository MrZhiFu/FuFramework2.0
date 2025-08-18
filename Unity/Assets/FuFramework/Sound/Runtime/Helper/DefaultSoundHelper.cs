using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 默认声音辅助器。
    /// 实现了声音资源的卸载功能。
    /// </summary>
    public class DefaultSoundHelper : SoundHelperBase
    {
        /// <summary>
        /// 资源管理器。
        /// </summary>
        private IAssetManager m_ResourceComponent;

        /// <summary>
        /// 释放声音资源。
        /// </summary>
        /// <param name="soundAsset">要释放的声音资源。</param>
        public override void ReleaseSoundAsset(object soundAsset)
        {
            // m_ResourceComponent.UnloadAsset(soundAsset);
        }

        private void Start()
        {
            m_ResourceComponent = FuEntry.GetModule<IAssetManager>();
            if (m_ResourceComponent == null) Log.Fatal("Resource component is invalid.");
        }
    }
}