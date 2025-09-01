using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 补丁流程步骤改变
    /// </summary>
    public sealed class AssetPatchStatesChangeEventArgs : GameEventArgs
    {
        public static readonly string EventId = typeof(AssetPatchStatesChangeEventArgs).FullName;

        public override string Id => EventId;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 当前步骤
        /// </summary>
        public EPatchStates CurrentStates { get; private set; }

        public override void Clear()
        {
            PackageName   = null;
            CurrentStates = EPatchStates.CreateDownloader;
        }

        /// <summary>
        /// 创建补丁流程步骤改变
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="currentStates">当前步骤</param>
        /// <returns></returns>
        public static AssetPatchStatesChangeEventArgs Create(string packageName, EPatchStates currentStates)
        {
            var assetPatchStatesChange = ReferencePool.Runtime.ReferencePool.Acquire<AssetPatchStatesChangeEventArgs>();
            assetPatchStatesChange.PackageName   = packageName;
            assetPatchStatesChange.CurrentStates = currentStates;
            return assetPatchStatesChange;
        }
    }
}