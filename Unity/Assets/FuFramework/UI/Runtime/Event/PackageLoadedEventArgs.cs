using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 包加载完成事件。
    /// </summary>
    public sealed class PackageLoadedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取包加载完成事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 包加载完成事件编号。
        /// </summary>
        public static readonly string EventId = typeof(PackageLoadedEventArgs).FullName;

        /// <summary>
        /// 获取加载完成的包名。
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 初始化包加载完成事件的新实例。
        /// </summary>
        public PackageLoadedEventArgs()
        {
            PackageName = null;
        }

        /// <summary>
        /// 创建包加载完成事件。
        /// </summary>
        /// <param name="packageName">加载完成的包名。</param>
        /// <returns>创建的包加载完成事件。</returns>
        public static PackageLoadedEventArgs Create(string packageName)
        {
            var packageLoadedEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<PackageLoadedEventArgs>();
            packageLoadedEventArgs.PackageName = packageName;
            return packageLoadedEventArgs;
        }

        /// <summary>
        /// 清理包加载完成事件。
        /// </summary>
        public override void Clear()
        {
            PackageName = null;
        }
    }
}
