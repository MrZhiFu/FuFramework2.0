using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;

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
        public static class Compression
        {
            /// 解压缩辅助器
            private static readonly CompressionHelper m_CompressionHelper = new();

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
                if (m_CompressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (bytes == null) throw new FuException("要压缩的二进制数据为空.");

                if (offset < 0 || length < 0 || offset + length > bytes.Length)
                    throw new FuException("偏移或长度超出范围.");

                if (compressedStream == null)
                    throw new FuException("压缩后数据的字节流为空.");

                try
                {
                    return m_CompressionHelper.Compress(bytes, offset, length, compressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException($"无法压缩，出现异常 '{exception}'.", exception);
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
                if (m_CompressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (stream == null) throw new FuException("要压缩的字节流为空.");
                if (compressedStream == null) throw new FuException("压缩后的字节流为空.");

                try
                {
                    return m_CompressionHelper.Compress(stream, compressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException($"无法压缩，出现异常 '{exception}'.", exception);
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
                if (m_CompressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (bytes == null) throw new FuException("要压缩的二进制数据为空.");

                if (offset < 0 || length < 0 || offset + length > bytes.Length)
                    throw new FuException("偏移或长度超出范围.");

                if (decompressedStream == null)
                    throw new FuException("解压缩后数据的字节流为空.");

                try
                {
                    return m_CompressionHelper.Decompress(bytes, offset, length, decompressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException($"无法压缩，出现异常 '{exception}'.", exception);
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
                if (m_CompressionHelper == null) throw new FuException("压缩辅助器为空.");
                if (stream == null) throw new FuException("要解压的字节流为空.");
                if (decompressedStream == null) throw new FuException("解压后的字节流为空.");

                try
                {
                    return m_CompressionHelper.Decompress(stream, decompressedStream);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException($"无法压缩，出现异常 '{exception}'.", exception);
                }
            }
        }

        /// <summary>
        /// 默认压缩解压缩辅助器。
        /// </summary>
        public class CompressionHelper
        {
            private const int CachedBytesLength = 0x1000;
            private readonly byte[] m_CachedBytes = new byte[CachedBytesLength];

            /// <summary>
            /// 压缩数据。
            /// </summary>
            /// <param name="bytes">要压缩的数据的二进制流。</param>
            /// <param name="offset">要压缩的数据的二进制流的偏移。</param>
            /// <param name="length">要压缩的数据的二进制流的长度。</param>
            /// <param name="compressedStream">压缩后的数据的二进制流。</param>
            /// <returns>是否压缩数据成功。</returns>
            public bool Compress(byte[] bytes, int offset, int length, Stream compressedStream)
            {
                if (bytes == null) return false;
                if (offset < 0 || length < 0 || offset + length > bytes.Length) return false;
                if (compressedStream == null) return false;

                try
                {
                    var gZipOutputStream = new GZipOutputStream(compressedStream);
                    gZipOutputStream.Write(bytes, offset, length);
                    gZipOutputStream.Finish();
                    _ProcessHeader(compressedStream);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// 压缩数据。
            /// </summary>
            /// <param name="stream">要压缩的数据的二进制流。</param>
            /// <param name="compressedStream">压缩后的数据的二进制流。</param>
            /// <returns>是否压缩数据成功。</returns>
            public bool Compress(Stream stream, Stream compressedStream)
            {
                if (stream == null) return false;
                if (compressedStream == null) return false;

                try
                {
                    var gZipOutputStream = new GZipOutputStream(compressedStream);
                    int bytesRead;
                    while ((bytesRead = stream.Read(m_CachedBytes, 0, CachedBytesLength)) > 0)
                    {
                        gZipOutputStream.Write(m_CachedBytes, 0, bytesRead);
                    }

                    gZipOutputStream.Finish();
                    _ProcessHeader(compressedStream);
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Array.Clear(m_CachedBytes, 0, CachedBytesLength);
                }
            }

            /// <summary>
            /// 解压缩数据。
            /// </summary>
            /// <param name="bytes">要解压缩的数据的二进制流。</param>
            /// <param name="offset">要解压缩的数据的二进制流的偏移。</param>
            /// <param name="length">要解压缩的数据的二进制流的长度。</param>
            /// <param name="decompressedStream">解压缩后的数据的二进制流。</param>
            /// <returns>是否解压缩数据成功。</returns>
            public bool Decompress(byte[] bytes, int offset, int length, Stream decompressedStream)
            {
                if (bytes == null) return false;
                if (offset < 0 || length < 0 || offset + length > bytes.Length) return false;
                if (decompressedStream == null) return false;

                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, offset, length, false);
                    using var gZipInputStream = new GZipInputStream(memoryStream);
                    int bytesRead;
                    while ((bytesRead = gZipInputStream.Read(m_CachedBytes, 0, CachedBytesLength)) > 0)
                    {
                        decompressedStream.Write(m_CachedBytes, 0, bytesRead);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    memoryStream?.Dispose();
                    Array.Clear(m_CachedBytes, 0, CachedBytesLength);
                }
            }

            /// <summary>
            /// 解压缩数据。
            /// </summary>
            /// <param name="stream">要解压缩的数据的二进制流。</param>
            /// <param name="decompressedStream">解压缩后的数据的二进制流。</param>
            /// <returns>是否解压缩数据成功。</returns>
            public bool Decompress(Stream stream, Stream decompressedStream)
            {
                if (stream == null) return false;
                if (decompressedStream == null) return false;

                try
                {
                    var gZipInputStream = new GZipInputStream(stream);
                    int bytesRead;
                    while ((bytesRead = gZipInputStream.Read(m_CachedBytes, 0, CachedBytesLength)) > 0)
                    {
                        decompressedStream.Write(m_CachedBytes, 0, bytesRead);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Array.Clear(m_CachedBytes, 0, CachedBytesLength);
                }
            }

            /// <summary>
            /// 处理头部
            /// </summary>
            private static void _ProcessHeader(Stream compressedStream)
            {
                if (compressedStream.Length < 8L) return;
                var current = compressedStream.Position;
                compressedStream.Position = 4L;
                compressedStream.WriteByte(25);
                compressedStream.WriteByte(134);
                compressedStream.WriteByte(2);
                compressedStream.WriteByte(32);
                compressedStream.Position = current;
            }
        }
    }
}