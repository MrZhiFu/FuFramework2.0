using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源更新状态改变事件
    /// </summary>
    public sealed class AssetUpdateStateChangeEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(AssetUpdateStateChangeEventArgs).FullName;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 当前步骤
        /// </summary>
        public EUpdateStates CurrentStates { get; private set; }

        public override void Clear()
        {
            PackageName   = null;
            CurrentStates = EUpdateStates.CreateDownloader;
        }

        /// <summary>
        /// 创建补丁流程步骤改变
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="currentStates">当前步骤</param>
        /// <returns></returns>
        public static AssetUpdateStateChangeEventArgs Create(string packageName, EUpdateStates currentStates)
        {
            var assetPatchStatesChange = ReferencePool.Runtime.ReferencePool.Acquire<AssetUpdateStateChangeEventArgs>();
            assetPatchStatesChange.PackageName   = packageName;
            assetPatchStatesChange.CurrentStates = currentStates;
            return assetPatchStatesChange;
        }
    }
}