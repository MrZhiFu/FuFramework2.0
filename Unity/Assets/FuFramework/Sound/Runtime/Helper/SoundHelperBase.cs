using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音辅助器基类。
    /// 用于实现声音资源的释放。
    /// </summary>
    public abstract class SoundHelperBase : MonoBehaviour, ISoundHelper
    {
        /// <summary>
        /// 释放声音资源。
        /// </summary>
        /// <param name="soundAsset">要释放的声音资源。</param>
        public abstract void ReleaseSoundAsset(object soundAsset);
    }
}
