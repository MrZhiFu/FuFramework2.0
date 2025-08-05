using System.IO;
using System.Collections.Generic;
using GameFrameX.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架序列化器基类。
    /// </summary>
    /// <typeparam name="T">要序列化的数据类型。</typeparam>
    public abstract class GameFrameworkSerializer<T>
    {
        /// 最新的序列化回调函数的版本
        private byte m_LatestSerializeCbVersion;


        /// 序列化回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, SerializeCallback> m_SerializeCbDict;

        /// 反序列化回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, DeserializeCallback> m_DeserializeCbDict;

        /// 取值回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, TryGetValueCallback> m_TryGetValueCbDict;


        /// <summary>
        /// 初始化游戏框架序列化器基类的新实例。
        /// </summary>
        protected GameFrameworkSerializer()
        {
            m_LatestSerializeCbVersion = 0;

            m_SerializeCbDict   = new Dictionary<byte, SerializeCallback>();
            m_DeserializeCbDict = new Dictionary<byte, DeserializeCallback>();
            m_TryGetValueCbDict = new Dictionary<byte, TryGetValueCallback>();
        }

        /// <summary>
        /// 序列化回调函数。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="data">要序列化的数据。</param>
        /// <returns>是否序列化数据成功。</returns>
        public delegate bool SerializeCallback(Stream stream, T data);

        /// <summary>
        /// 反序列化回调函数。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <returns>反序列化的数据。</returns>
        public delegate T DeserializeCallback(Stream stream);

        /// <summary>
        /// 尝试从指定流获取指定键的值回调函数。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <param name="key">指定键。</param>
        /// <param name="value">指定键的值。</param>
        /// <returns>是否从指定流获取指定键的值成功。</returns>
        public delegate bool TryGetValueCallback(Stream stream, string key, out object value);

        /// <summary>
        /// 注册序列化时采用的回调函数。
        /// </summary>
        /// <param name="version">序列化回调函数的版本。</param>
        /// <param name="callback">序列化回调函数。</param>
        public void RegisterSerializeCallback(byte version, SerializeCallback callback)
        {
            m_SerializeCbDict[version] = callback ?? throw new GameFrameworkException("传入的序列化回调函数为空.");
            if (version <= m_LatestSerializeCbVersion) return;
            m_LatestSerializeCbVersion = version;
        }

        /// <summary>
        /// 注册反序列化时采用的回调函数。
        /// </summary>
        /// <param name="version">反序列化回调函数的版本。</param>
        /// <param name="callback">反序列化回调函数。</param>
        public void RegisterDeserializeCallback(byte version, DeserializeCallback callback)
        {
            m_DeserializeCbDict[version] = callback ?? throw new GameFrameworkException("传入的反序列化回调函数为空.");
        }

        /// <summary>
        /// 注册尝试从指定流获取指定键的值时采用的回调函数。
        /// </summary>
        /// <param name="version">尝试从指定流获取指定键的值回调函数的版本。</param>
        /// <param name="callback">尝试从指定流获取指定键的值回调函数。</param>
        public void RegisterTryGetValueCallback(byte version, TryGetValueCallback callback)
        {
            m_TryGetValueCbDict[version] = callback ?? throw new GameFrameworkException("传入的取值回调函数为空.");
        }

        /// <summary>
        /// 序列化数据到目标流中。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="data">要序列化的数据。</param>
        /// <returns>是否序列化数据成功。</returns>
        public bool Serialize(Stream stream, T data)
        {
            return m_SerializeCbDict.Count <= 0
                ? throw new GameFrameworkException("未注册任何序列化回调函数.")
                : Serialize(stream, data, m_LatestSerializeCbVersion);
        }

        /// <summary>
        /// 序列化数据到目标流中。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="data">要序列化的数据。</param>
        /// <param name="version">序列化回调函数的版本。</param>
        /// <returns>是否序列化数据成功。</returns>
        public bool Serialize(Stream stream, T data, byte version)
        {
            var header = GetHeader();

            stream.WriteByte(header[0]);
            stream.WriteByte(header[1]);
            stream.WriteByte(header[2]);
            stream.WriteByte(version);

            if (!m_SerializeCbDict.TryGetValue(version, out var callback))
                throw new GameFrameworkException(Utility.Text.Format("序列化回调函数版本 '{0}' 不存在.", version));

            return callback(stream, data);
        }

        /// <summary>
        /// 从指定流反序列化数据。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <returns>反序列化的数据。</returns>
        public T Deserialize(Stream stream)
        {
            var header = GetHeader();

            var header0 = (byte)stream.ReadByte();
            var header1 = (byte)stream.ReadByte();
            var header2 = (byte)stream.ReadByte();

            if (header0 != header[0] || header1 != header[1] || header2 != header[2])
                throw new GameFrameworkException(Utility.Text.Format("标头无效, 需要 '{0}{1}{2}', 当前为 '{3}{4}{5}'.", (char)header[0], (char)header[1], (char)header[2], (char)header0,
                                                                     (char)header1, (char)header2));

            var version = (byte)stream.ReadByte();
            if (!m_DeserializeCbDict.TryGetValue(version, out var callback))
                throw new GameFrameworkException(Utility.Text.Format("反序列化回调函数版本 '{0}' 不存在.", version));

            return callback(stream);
        }

        /// <summary>
        /// 尝试从指定流获取指定键的值。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <param name="key">指定键。</param>
        /// <param name="value">回调函数处理之后的值。</param>
        /// <returns>从指定流获取指定回调函数处理之后数据成功。</returns>
        public bool TryGetValue(Stream stream, string key, out object value)
        {
            value = null;
            var header = GetHeader();

            var header0 = (byte)stream.ReadByte();
            var header1 = (byte)stream.ReadByte();
            var header2 = (byte)stream.ReadByte();

            if (header0 != header[0] || header1 != header[1] || header2 != header[2])
                return false;

            var version = (byte)stream.ReadByte(); // 版本号
            return m_TryGetValueCbDict.TryGetValue(version, out var callback) && callback(stream, key, out value);
        }

        /// <summary>
        /// 获取数据头标识。
        /// </summary>
        /// <returns>数据头标识。</returns>
        protected abstract byte[] GetHeader();
    }
}