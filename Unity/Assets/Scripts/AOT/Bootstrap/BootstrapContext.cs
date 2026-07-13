// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 启动引导共享上下文。
    ///     功能：由 AOT 引导助手在初始化 YooAsset 后写入状态，供热更侧 AssetModule 判断是否复用，避免二次初始化。
    /// </summary>
    public static class BootstrapContext
    {
        /// <summary>
        /// YooAsset 是否已由引导助手完成初始化。
        /// </summary>
        public static bool YooAssetInitialized { get; set; }

        /// <summary>
        /// 默认资源包名称（引导期写入）。
        /// </summary>
        public static string DefaultPackageName { get; set; }
    }
}