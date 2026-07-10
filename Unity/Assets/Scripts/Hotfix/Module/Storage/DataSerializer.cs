using FuFramework.Core.Runtime;

namespace Hotfix.Storage
{
    /// <summary>
    /// 数据序列化器。
    /// </summary>
    public sealed class DataSerializer : FuSerializer<Data>
    {
        /// <summary>
        /// 默认游戏数据头标识。
        /// G M D : GameData 表示游戏数据文件夹
        /// </summary>
        private static readonly byte[] Header = { (byte)'G', (byte)'M', (byte)'D' };

        /// <summary>
        /// 获取默认游戏数据头标识。
        /// </summary>
        /// <returns>默认游戏数据头标识。</returns>
        protected override byte[] GetHeader() => Header;
    }
}
