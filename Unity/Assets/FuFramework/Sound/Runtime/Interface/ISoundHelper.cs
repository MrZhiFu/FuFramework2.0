// ReSharper disable once CheckNamespace

namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音辅助器接口。
    /// 定义了声音资源的释放接口。
    /// </summary>
    public interface ISoundHelper
    {
        /// <summary>
        /// 释放声音资源。
        /// </summary>
        /// <param name="soundAsset">要释放的声音资源。</param>
        void ReleaseSoundAsset(object soundAsset);
    }
}