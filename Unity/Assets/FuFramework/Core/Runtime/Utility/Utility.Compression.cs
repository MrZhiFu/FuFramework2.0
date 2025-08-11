using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 压缩与解压缩相关的实用函数。
        /// 功能：
        /// 1. 使用压缩辅助器压缩二进制数据为字节流，或压缩字节流为二进制数据。
        /// 2. 使用压缩辅助器解压二进制数据为字节流，或解压字节流为二进制数据。
        /// </summary>
        public static partial class Compression
        {
            /// 解压缩辅助器
            private static ICompressionHelper _compressionHelper;

            /// <summary>
            /// 设置解压缩辅助器。
            /// </summary>
            /// <param name="compressionHelper">要设置的压缩解压缩辅助器。</param>
            public static void SetCompressionHelper(ICompressionHelper compressionHelper)
                => _compressionHelper = compressionHelper;

            /// <summary>
            /// 压缩二进制数据。
            /// </summary>
            /// <param name="bytes">要压缩的二进制数据。</param>
            /// <returns>压缩后数据的字节流。</returns>
            public static byte[] Compress(byte[] bytes)
                => bytes != null ? Compress(bytes, 0, bytes.Length) : throw new FuException("要压缩的二进制数据为空.");

            /// <summary>
            /// 压缩二进制数据。
            /// </summary>
            /// <param name="bytes">要压缩的二进制数据。</param>
            /// <param name="compressedStream">压缩后数据的字节流。</param>
            /// <returns>是否压缩成功。</returns>
            public static bool Compress(byte[] bytes, Stream compressedStream)
                => bytes != null ? Compress(bytes, 0, bytes.Length, compressedStream) : throw new FuException("要压缩的二进制数据为空.");

            /// <summary>
            /// 压缩二进制数据。
            /// </summary>
            /// <param name="bytes">要压缩的二进制数据。</param>
            /// <param name="offset">要压缩的二进制的偏移。</param>
            /// <param name="length">要压缩的二进制的长度。</param>
            /// <returns>压缩后数据的字节流。</returns>
            public static byte[] Compress(byte[] bytes, int offset, int length)
            {
                using var compressedStream = new MemoryStream();
                return Compress(bytes, offset, length, compressedStream) ? compressedStream.ToArray() : null;
            }

            /// <summary>
            /// 压缩二进制数据。
            /// </summary>
            /// <param name="bytes">要压缩的二进制数据。</param>
            /// <param name="offset">要压缩的二进制的偏移。</param>
            /// <param name="length">要压缩的二进制的长度。</param>
            /// <param name="compressedStream">压缩后数据的字节流。</param>
            /// <returns>是否压缩成功。</returns>
            public static bool Compress(byte[] bytes, int offset, int length, Stream compressedStream)
            {
                if (_compressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (bytes               == null) throw new FuException("要压缩的二进制数据为空.");

                if (offset < 0 || length < 0 || offset + length > bytes.Length)
                    throw new FuException("偏移或长度超出范围.");

                if (compressedStream == null)
                    throw new FuException("压缩后数据的字节流为空.");

                try
                {
                    return _compressionHelper.Compress(bytes, offset, length, compressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法压缩，出现异常 '{0}'.", exception), exception);
                }
            }

            /// <summary>
            /// 压缩字节流数据。
            /// </summary>
            /// <param name="stream">要压缩的字节流。</param>
            /// <returns>压缩后的字节流。</returns>
            public static byte[] Compress(Stream stream)
            {
                using var compressedStream = new MemoryStream();
                return Compress(stream, compressedStream) ? compressedStream.ToArray() : null;
            }

            /// <summary>
            /// 压缩字节流数据。
            /// </summary>
            /// <param name="stream">要压缩的字节流。</param>
            /// <param name="compressedStream">压缩后的字节流。</param>
            /// <returns>是否压缩成功。</returns>
            public static bool Compress(Stream stream, Stream compressedStream)
            {
                if (_compressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (stream              == null) throw new FuException("要压缩的字节流为空.");
                if (compressedStream    == null) throw new FuException("压缩后的字节流为空.");

                try
                {
                    return _compressionHelper.Compress(stream, compressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法压缩，出现异常 '{0}'.", exception), exception);
                }
            }

            /// <summary>
            /// 解压二进制数据。
            /// </summary>
            /// <param name="bytes">要解压的二进制数据。</param>
            /// <returns>解压后的二进制数据。</returns>
            public static byte[] Decompress(byte[] bytes)
                => bytes != null ? Decompress(bytes, 0, bytes.Length) : throw new FuException("要压缩的二进制数据为空.");

            /// <summary>
            /// 解压二进制数据。
            /// </summary>
            /// <param name="bytes">要解压的二进制数据。</param>
            /// <param name="decompressedStream">解压后的字节流。</param>
            /// <returns>是否解压成功。</returns>
            public static bool Decompress(byte[] bytes, Stream decompressedStream)
                => bytes != null ? Decompress(bytes, 0, bytes.Length, decompressedStream) : throw new FuException("要压缩的二进制数据为空.");

            /// <summary>
            /// 解压二进制数据。
            /// </summary>
            /// <param name="bytes">要解压的二进制数据。</param>
            /// <param name="offset">要解压缩的二进制数据的偏移。</param>
            /// <param name="length">要解压缩的二进制数据的长度。</param>
            /// <returns>解压后的二进制数据。</returns>
            public static byte[] Decompress(byte[] bytes, int offset, int length)
            {
                using var decompressedStream = new MemoryStream();
                return Decompress(bytes, offset, length, decompressedStream) ? decompressedStream.ToArray() : null;
            }

            /// <summary>
            /// 解压二进制数据。
            /// </summary>
            /// <param name="bytes">要解压的二进制数据。</param>
            /// <param name="offset">要解压缩的二进制数据的偏移。</param>
            /// <param name="length">要解压缩的二进制数据的长度。</param>
            /// <param name="decompressedStream">解压后的字节流。</param>
            /// <returns>是否解压成功。</returns>
            public static bool Decompress(byte[] bytes, int offset, int length, Stream decompressedStream)
            {
                if (_compressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (bytes               == null) throw new FuException("要压缩的二进制数据为空.");

                if (offset < 0 || length < 0 || offset + length > bytes.Length)
                    throw new FuException("偏移或长度超出范围.");

                if (decompressedStream == null)
                    throw new FuException("解压缩后数据的字节流为空.");

                try
                {
                    return _compressionHelper.Decompress(bytes, offset, length, decompressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法解压，出现异常 '{0}'.", exception), exception);
                }
            }

            /// <summary>
            /// 解压字节流数据。
            /// </summary>
            /// <param name="stream">要解压的字节流。</param>
            /// <returns>是否解压成功。</returns>
            public static byte[] Decompress(Stream stream)
            {
                using var decompressedStream = new MemoryStream();
                return Decompress(stream, decompressedStream) ? decompressedStream.ToArray() : null;
            }

            /// <summary>
            /// 解压字节流数据。
            /// </summary>
            /// <param name="stream">要解压的字节流。</param>
            /// <param name="decompressedStream">解压后的字节流。</param>
            /// <returns>是否解压成功。</returns>
            public static bool Decompress(Stream stream, Stream decompressedStream)
            {
                if (_compressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (stream              == null) throw new FuException("要解压的字节流为空.");
                if (decompressedStream  == null) throw new FuException("解压后的字节流为空.");

                try
                {
                    return _compressionHelper.Decompress(stream, decompressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法解压，出现异常 '{0}'.", exception), exception);
                }
            }
        }
    }
}