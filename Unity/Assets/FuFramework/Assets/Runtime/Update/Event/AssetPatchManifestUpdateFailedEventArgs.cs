﻿using GameFrameX.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 补丁清单更新失败
    /// </summary>
    public sealed class AssetPatchManifestUpdateFailedEventArgs : GameEventArgs
    {
        public static readonly string EventId = typeof(AssetPatchManifestUpdateFailedEventArgs).FullName;

        public override string Id => EventId;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; private set; }

        public override void Clear()
        {
            PackageName = null;
            Error       = null;
        }

        /// <summary>
        /// 创建补丁清单更新失败
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="error">错误信息</param>
        /// <returns></returns>
        public static AssetPatchManifestUpdateFailedEventArgs Create(string packageName, string error)
        {
            var assetPatchManifestUpdateFailed = ReferencePool.Acquire<AssetPatchManifestUpdateFailedEventArgs>();
            assetPatchManifestUpdateFailed.PackageName = packageName;
            assetPatchManifestUpdateFailed.Error       = error;
            return assetPatchManifestUpdateFailed;
        }
    }
}