using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Runtime
{
    /// <summary>
    /// 默认游戏配置序列化器。
    /// </summary>
    public sealed class DefaultSettingSerializer : FuSerializer<DefaultSetting>
    {
        /// <summary>
        /// 默认游戏配置头标识。
        /// </summary>
        private static readonly byte[] Header = { (byte)'G', (byte)'F', (byte)'S' };

        /// <summary>
        /// 获取默认游戏配置头标识。
        /// </summary>
        /// <returns>默认游戏配置头标识。</returns>
        protected override byte[] GetHeader() => Header;
    }
}