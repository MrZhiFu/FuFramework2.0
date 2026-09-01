using System.Threading;
using UnityEngine;
using YooAsset;

namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 实例化引用：句柄 + 引用计数。
    /// 同一路径多个实例共享一个句柄，引用计数跟踪活跃实例数，
    /// 计数归零时释放句柄（见 AssetModule.ReleaseInstantiate），让资源可被卸载。
    /// </summary>
    internal sealed class InstantiateRef
    {
        /// <summary>
        /// 资源句柄，持有 YooAsset 的资源引用。
        /// </summary>
        public AssetHandle Handle;

        /// <summary>
        /// 引用计数，即该路径当前活跃的实例化对象数。
        /// </summary>
        public int RefCount;
    }

    /// <summary>
    /// 实例化结果。携带实例对象、资源路径与创建时捕获的生命周期 Token。
    /// 实例销毁时调用 AssetModule.ReleaseInstantiate(result) 释放引用；
    /// 重启（OnDispose/重新初始化）后旧生命周期结果携带的 Token 与当前不同，会被识别并忽略，避免误释放新生命周期同路径引用。
    /// </summary>
    public sealed class InstantiateResult
    {
        /// <summary>
        /// 实例化出的 GameObject 对象。
        /// </summary>
        public GameObject Instance { get; internal set; }

        /// <summary>
        /// 实例来源的资源路径。
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// 创建本结果时捕获的生命周期 Token（旧生命周期结果重启后据此识别并忽略释放）。
        /// </summary>
        public CancellationToken Token { get; internal set; }
    }
}