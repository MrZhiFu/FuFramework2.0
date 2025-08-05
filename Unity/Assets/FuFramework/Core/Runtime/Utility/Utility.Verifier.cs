//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.IO;
using FuFramework.Core.Runtime;
using UnityEngine.Scripting;

namespace GameFrameX.Runtime
{
    public static partial class Utility
    {
      /// <summary>
        /// 校验数据完整性相关的实用函数。
        /// 功能：
        /// 1. 计算二进制流的 CRC32。
        /// 2. 获取 CRC32 数值的二进制数组。
        /// </summary>
        public static partial class Verifier
        {
            private const int CachedBytesLength = 0x1000; // 缓存字节数组的长度，0x1000表示缓存16KB数据

            private static readonly byte[] s_CachedBytes = new byte[CachedBytesLength]; // 缓存字节数组
            private static readonly Crc32  s_Algorithm   = new();                       // Crc32算法对象

            /// <summary>
            /// 获取二进制流的使用CRC32算法后的哈希值
            /// </summary>
            /// <param name="bytes">指定的二进制流。</param>
            /// <returns>计算后的 CRC32。</returns>
            public static int GetCrc32(byte[] bytes)
            {
                if (bytes == null) throw new GameFrameworkException("要计算 CRC32 的二进制流为空.");
                return GetCrc32(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// 获取二进制流的使用CRC32算法后的哈希值。
            /// </summary>
            /// <param name="bytes">指定的二进制流。</param>
            /// <param name="offset">二进制流的偏移。</param>
            /// <param name="length">二进制流的长度。</param>
            /// <returns>计算后的 CRC32。</returns>
            public static int GetCrc32(byte[] bytes, int offset, int length)
            {
                if (bytes == null)
                    throw new GameFrameworkException("要计算 CRC32 的二进制流为空.");

                if (offset < 0 || length < 0 || offset + length > bytes.Length)
                    throw new GameFrameworkException("二进制流的偏移或长度不正确.");

                s_Algorithm.HashCore(bytes, offset, length);
                var result = (int)s_Algorithm.HashFinal();
                s_Algorithm.Initialize();
                return result;
            }

            /// <summary>
            /// 获取字节流的使用CRC32算法后的哈希值。
            /// </summary>
            /// <param name="stream">指定的字节流。</param>
            /// <returns>计算后的 CRC32。</returns>
            public static int GetCrc32(Stream stream)
            {
                if (stream == null) throw new GameFrameworkException("要计算 CRC32 的字节流为空.");

                while (true)
                {
                    var bytesRead = stream.Read(s_CachedBytes, 0, CachedBytesLength);
                    if (bytesRead > 0)
                        s_Algorithm.HashCore(s_CachedBytes, 0, bytesRead);
                    else
                        break;
                }

                var result = (int)s_Algorithm.HashFinal();
                s_Algorithm.Initialize();
                Array.Clear(s_CachedBytes, 0, CachedBytesLength);
                return result;
            }

            /// <summary>
            /// 获取使用CRC32算法后的哈希值的二进制数组形式。如：0x12345678 转换为 [0x78, 0x56, 0x34, 0x12]。
            /// </summary>
            /// <param name="crc32">CRC32 哈希值。</param>
            /// <returns>CRC32 哈希值的二进制数组。</returns>
            public static byte[] GetCrc32Bytes(int crc32)
                => new byte[] { (byte)((crc32 >> 24) & 0xff), (byte)((crc32 >> 16) & 0xff), (byte)((crc32 >> 8) & 0xff), (byte)(crc32 & 0xff) };

            /// <summary>
            /// 获取使用CRC32算法后的哈希值的二进制数组形式。如：0x12345678 转换为 [0x78, 0x56, 0x34, 0x12]
            /// </summary>
            /// <param name="crc32">CRC32 哈希值。</param>
            /// <param name="bytes">要存放结果的数组。</param>
            public static void GetCrc32Bytes(int crc32, byte[] bytes) => GetCrc32Bytes(crc32, bytes, 0);

            /// <summary>
            /// 获取使用CRC32算法后的哈希值的二进制数组。如：0x12345678 转换为 [0x78, 0x56, 0x34, 0x12]。
            /// </summary>
            /// <param name="crc32">CRC32 哈希值。</param>
            /// <param name="bytes">要存放结果的数组。</param>
            /// <param name="offset">CRC32 哈希值的二进制数组在结果数组内的起始位置。</param>
            public static void GetCrc32Bytes(int crc32, byte[] bytes, int offset)
            {
                if (bytes == null) throw new GameFrameworkException("传入的结果数组为空.");
                if (offset < 0 || offset + 4 > bytes.Length) throw new GameFrameworkException("结果数组的偏移或长度不正确.");

                bytes[offset]     = (byte)((crc32 >> 24) & 0xff);
                bytes[offset + 1] = (byte)((crc32 >> 16) & 0xff);
                bytes[offset + 2] = (byte)((crc32 >> 8)  & 0xff);
                bytes[offset + 3] = (byte)(crc32         & 0xff);
            }

            /// <summary>
            /// 获取字节流的使用指定密钥的CRC32算法后的哈希值。
            /// </summary>
            /// <param name="stream">指定的字节流。</param>
            /// <param name="code">指定的密钥。</param>
            /// <param name="length">密钥的长度。</param>
            /// <returns></returns>
            /// <exception cref="GameFrameworkException"></exception>
            internal static int GetCrc32(Stream stream, byte[] code, int length)
            {
                if (stream == null) throw new GameFrameworkException("指定的字节流为空.");
                if (code   == null) throw new GameFrameworkException("指定的密钥为空.");

                var codeLength = code.Length;
                if (codeLength <= 0) throw new GameFrameworkException("指定的密钥长度不正确.");

                var bytesLength = (int)stream.Length;
                if (length < 0 || length > bytesLength)
                    length = bytesLength;

                var codeIndex = 0;
                while (true)
                {
                    var bytesRead = stream.Read(s_CachedBytes, 0, CachedBytesLength);
                    if (bytesRead > 0)
                    {
                        if (length > 0)
                        {
                            for (var i = 0; i < bytesRead && i < length; i++)
                            {
                                s_CachedBytes[i] ^= code[codeIndex++];
                                codeIndex        %= codeLength;
                            }

                            length -= bytesRead;
                        }

                        s_Algorithm.HashCore(s_CachedBytes, 0, bytesRead);
                    }
                    else
                        break;
                }

                var result = (int)s_Algorithm.HashFinal();
                s_Algorithm.Initialize();
                Array.Clear(s_CachedBytes, 0, CachedBytesLength);
                return result;
            }
        }
    }
}