using System.IO;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架序列化器基类。
    /// 功能：
    /// 1.定义序列化和反序列化的回调函数。
    /// 2.提供注册序列化和反序列化的回调函数的接口。
    /// 3.提供序列化和反序列化的接口。
    /// 4.提供尝试从指定流获取指定键的值的接口。
    /// 5.提供数据头标识的接口，子类必须实现，用于区分不同的数据。
    /// </summary>
    /// <typeparam name="T">要序列化的数据类型。</typeparam>
    public abstract class FuSerializer<T>
    {
        /// 最新的序列化回调函数的版本
        private byte m_LatestSerializeCbVersion;


        /// <summary>
        /// 序列化回调函数。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="data">要序列化的数据。</param>
        /// <returns>是否序列化数据成功。</returns>
        public delegate bool SerializeCallback(Stream stream, T data);
        
        /// 序列化回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, SerializeCallback> m_SerializeCbDict;
        

        /// <summary>
        /// 反序列化回调函数。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <returns>反序列化的数据。</returns>
        public delegate T DeserializeCallback(Stream stream);

        /// 反序列化回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, DeserializeCallback> m_DeserializeCbDict;


        /// <summary>
        /// 尝试从指定流获取指定键的值回调函数。
        /// </summary>
        /// <param name="stream">指定流。</param>
        /// <param name="key">指定键。</param>
        /// <param name="value">指定键的值。</param>
        /// <returns>是否从指定流获取指定键的值成功。</returns>
        public delegate bool TryGetValueCallback(Stream stream, string key, out object value);

        /// 取值回调函数的字典, key:回调函数的版本--Value:回调函数
        private readonly Dictionary<byte, TryGetValueCallback> m_TryGetValueCbDict;


        /// <summary>
        /// 初始化游戏框架序列化器基类的新实例。
        /// </summary>
        protected FuSerializer()
        {
            m_LatestSerializeCbVersion = 0;

            m_SerializeCbDict   = new Dictionary<byte, SerializeCallback>();
            m_DeserializeCbDict = new Dictionary<byte, DeserializeCallback>();
            m_TryGetValueCbDict = new Dictionary<byte, TryGetValueCallback>();
        }


        /// <summary>
        /// 注册序列化时采用的回调函数。
        /// </summary>
        /// <param name="version">序列化回调函数的版本。</param>
        /// <param name="callback">序列化回调函数。</param>
        public void RegisterSerializeCallback(byte version, SerializeCallback callback)
        {
            m_SerializeCbDict[version] = callback ?? throw new FuException("传入的序列化回调函数为空.");
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
            m_DeserializeCbDict[version] = callback ?? throw new FuException("传入的反序列化回调函数为空.");
        }

        /// <summary>
        /// 注册尝试从指定流获取指定键的值时采用的回调函数。
        /// </summary>
        /// <param name="version">尝试从指定流获取指定键的值回调函数的版本。</param>
        /// <param name="callback">尝试从指定流获取指定键的值回调函数。</param>
        public void RegisterTryGetValueCallback(byte version, TryGetValueCallback callback)
        {
            m_TryGetValueCbDict[version] = callback ?? throw new FuException("传入的取值回调函数为空.");
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
                ? throw new FuException("未注册任何序列化回调函数.")
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
                throw new FuException($"序列化回调函数版本 '{version}' 不存在.");

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
                throw new FuException($"标头无效, 需要 '{(char)header[0]}{(char)header[1]}{(char)header[2]}', 文件中为 '{ (char)header0}{(char)header1}{(char)header2}'.");

            var version = (byte)stream.ReadByte();
            if (!m_DeserializeCbDict.TryGetValue(version, out var callback))
                throw new FuException($"反序列化回调函数版本 '{version}' 不存在.");

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

            if (header0 != header[0] || header1 != header[1] || header2 != header[2]) return false;

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