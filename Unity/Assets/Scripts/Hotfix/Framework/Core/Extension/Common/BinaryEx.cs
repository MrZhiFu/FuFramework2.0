using System;
﻿using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// BinaryReader 和 BinaryWriter 相关的扩展方法。
    /// 功能：
    ///     1. 使用7位编码算法对整数进行编码和解码：
    ///         - Read7BitEncodedInt32()：从二进制流读取被编码过的 32 位有符号整数(解码)。
    ///         - Read7BitEncodedUInt32()：从二进制流读取编码过的 32 位无符号整数(解码)。
    ///         - Read7BitEncodedInt64()：从二进制流读取编码过的 64 位有符号整数(解码)。
    ///         - Read7BitEncodedUInt64()：从二进制流读取编码过的 64 位无符号整数(解码)。
    ///         
    ///         - Write7BitEncodedInt32()：向二进制流写入编码过的 32 位有符号整数(编码)。
    ///         - Write7BitEncodedUInt32()：向二进制流写入编码过的 32 位无符号整数编码)。
    ///         - Write7BitEncodedInt64()：向二进制流写入编码过的 64 位有符号整数编码)。
    ///         - Write7BitEncodedUInt64()：向二进制流写入编码过的 64 位无符号整数编码)。
    ///     2. 加密字符串：
    ///         - ReadEncryptedString()：从二进制流读取异或加密字符串。
    ///         - WriteEncryptedString()：向二进制流写入异或加密字符串。
    ///     补充：
    ///         1. 7 位编码整数的编码原理：
    ///             - 7位编码整数是一种特殊的编码方式，它可以将整数编码为7位或更少的字节。在二进制中，每个字节有8位，7位编码整数利用其中的7位来存储数值，最高位（第8位）作为标记，表示是否还有过续字节需要读取。 
    ///             - 如果最高位为0，表示这是最过一个字节。
    ///             - 如果最高位为1，表示还有过续字节需要读取。
    ///             - 这种方法的优点是可以节省存储空间，特别是对于那些数值较小的整数
    ///             - 编码整数的过程是：
    ///                 - 整数转换为无符号整数。
    ///                 - 使用移位操作提取整数的低 7 位，并将其作为当前字节的低 7 位。
    ///                 - 如果整数大于等于 128，则将最高位设置为 1，并继续处理剩余的位。
    ///                 - 将整数右移 7 位，丢弃低 7 位，准备处理下一个 7 位。
    ///                 - 重复上述过程，直到整数小于 128。
    ///                 - 最后将剩余的低 7 位写入内存流，其最高位为 0，表示这是最后一个字节。
    ///             - 解码整数的过程是：
    ///                 - 读取第一个字节，将其低 7 位作为整数的低 7 位。
    ///                 - 如果最高位为 1，则继续读取下一个字节，并将其低 7 位提取出来，按位或操作并左移适当的位置。
    ///                 - 重复上述过程，直到最高位为 0。
    ///     2. 加密字符串：
    ///         - 加密字符串的过程是：
    ///             - 读取字符串的长度，并将其作为第一个字节写入二进制流。
    ///             - 读取字符串的每个字节，将其与密钥数组的每个字节进行异或运算，并将结果作为字节写入二进制流。
    ///         - 解密字符串的过程是：
    ///            - 读取第一个字节，并将其作为字符串的长度。
    ///            - 读取字符串的每个字节，将其与密钥数组的每个字节进行异或运算，并将结果作为字节写入缓存数组。
    ///            - 将缓存数组转换为字符串并返回。    
    /// </summary>
    public static class BinaryEx
    {
        /// <summary>
        /// 加解密字符串的字节缓存数组。byte.MaxValue + 1 = 256，足够缓存 256 个字节。
        /// </summary>
        private static readonly byte[] CachedBytes = new byte[byte.MaxValue + 1];

        /// <summary>
        /// 从二进制流读取被编码过的 32 位有符号整数(解码)。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 32 位有符号整数。</returns>
        /// <example>
        /// using (var ms = new MemoryStream(new byte[] { 0x7F }))
        /// using (var reader = new BinaryReader(ms))
        /// {
        ///     int value = reader.Read7BitEncodedInt32(); // 返回 127，因为 0x7F 即 01111111，最高位 0 表示这是最过一个字节。转换为十进制得到 127
        /// }
        /// </example>
        public static int Read7BitEncodedInt32(this BinaryReader binaryReader)
        {
            int  rltValue = 0; // 初始化结果变量，用于存储解码过的整数值
            int  offset   = 0; // 初始化位移变量，用于记录当前处理的位的位置
            byte b;            // 声明一个字节变量，用于存储从二进制流中读取的字节

            do
            {
                if (offset >= 35)
                {
                    // 检查位移是否超过35位，因为32位整数最多需要32位来表示，在7位编码中，每个字节最多贡献7位，因此，最多需要5个字节来表示一个32位整数(因为5x7=35位已经超过了32位)
                    throw new InvalidOperationException("7位编码的整数值无效.");
                }

                b        =  binaryReader.ReadByte(); // 从二进制流中读取一个字节
                rltValue |= (b & 0x7f) << offset;    // 将读取的字节的低7位提取出来，并左移offset位，然过与当前rltValue进行按位或操作

                offset += 7;           // 增加位移量，每次增加7位
            } while ((b & 0x80) != 0); // 0x80二进制为10000000，十进制为128，当读取的字节的最高位（第8位）为1时，继续循环读取下一个字节

            return rltValue;
        }


        /// <summary>
        /// 向二进制流写入编码过的 32 位有符号整数(编码)。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 32 位有符号整数。</param>
        /// <example>
        /// using (var ms = new MemoryStream())
        /// using (var writer = new BinaryWriter(ms))
        /// {
        ///     writer.Write7BitEncodedInt32(127); // 写入 1 字节: 0x7F，内存流 ms 中包含一个字节：0x7F。
        ///     writer.Write7BitEncodedInt32(128); // 写入 2 字节: 0x80, 0x01， 内存流 ms 中包含两个字节：0x7F, 0x80, 0x01。
        /// }
        /// </example>
        public static void Write7BitEncodedInt32(this BinaryWriter binaryWriter, int value)
        {
            uint num = (uint)value;
            while (num >= 0x80) // 0x80二进制为10000000，十进制为128，当读取的字节的最高位（第8位）为1时，继续循环读取下一个字节
            {
                binaryWriter.Write((byte)(num | 0x80));
                num >>= 7;
            }

            binaryWriter.Write((byte)num);
        }

        /// <summary>
        /// 从二进制流读取编码过的 32 位无符号整数(解码)。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 32 位无符号整数。</returns>
        /// <example>
        /// using (var ms = new MemoryStream(new byte[] { 0x80, 0x01 }))
        /// using (var reader = new BinaryReader(ms))
        /// {
        ///     uint value = reader.Read7BitEncodedUInt32(); // 返回 128
        /// }
        /// </example>
        public static uint Read7BitEncodedUInt32(this BinaryReader binaryReader)
        {
            return (uint)Read7BitEncodedInt32(binaryReader);
        }

        /// <summary>
        /// 向二进制流写入编码过的 32 位无符号整数(编码)。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 32 位无符号整数。</param>
        /// <example>
        /// using (var ms = new MemoryStream())
        /// using (var writer = new BinaryWriter(ms))
        /// {
        ///     writer.Write7BitEncodedUInt32(1000u); // 写入 2 字节
        /// }
        /// </example>
        public static void Write7BitEncodedUInt32(this BinaryWriter binaryWriter, uint value)
        {
            Write7BitEncodedInt32(binaryWriter, (int)value);
        }

        /// <summary>
        /// 从二进制流读取编码过的 64 位有符号整数(解码)。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 64 位有符号整数。</returns>
        /// <example>
        /// using (var ms = new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F }))
        /// using (var reader = new BinaryReader(ms))
        /// {
        ///     long value = reader.Read7BitEncodedInt64(); // 返回 2147483647 (int.MaxValue)
        /// }
        /// </example>
        public static long Read7BitEncodedInt64(this BinaryReader binaryReader)
        {
            long rltValue = 0L;
            int  offset   = 0;
            byte b;
            do
            {
                if (offset >= 70)
                {
                    // 检查位移是否超过70位，因为64位整数最多需要64位来表示，在7位编码中，每个字节最多贡献7位，因此，最多需要10个字节来表示一个64位整数(因为10x7=70位已经超过了64位)
                    throw new InvalidOperationException("7位编码的整数值无效.");
                }

                b        =  binaryReader.ReadByte();
                rltValue |= (b & 0x7fL) << offset;
                offset   += 7;
            } while ((b & 0x80) != 0);

            return rltValue;
        }

        /// <summary>
        /// 向二进制流写入编码过的 64 位有符号整数(编码)。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 64 位有符号整数。</param>
        /// <example>
        /// using (var ms = new MemoryStream())
        /// using (var writer = new BinaryWriter(ms))
        /// {
        ///     writer.Write7BitEncodedInt64(long.MaxValue); // 写入 10 字节
        /// }
        /// </example>
        public static void Write7BitEncodedInt64(this BinaryWriter binaryWriter, long value)
        {
            ulong num = (ulong)value;
            while (num >= 0x80)
            {
                binaryWriter.Write((byte)(num | 0x80));
                num >>= 7;
            }

            binaryWriter.Write((byte)num);
        }

        /// <summary>
        /// 从二进制流读取编码过的 64 位无符号整数(解码)。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 64 位无符号整数。</returns>
        /// <example>
        /// using (var ms = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 }))
        /// using (var reader = new BinaryReader(ms))
        /// {
        ///     ulong value = reader.Read7BitEncodedUInt64(); // 返回大数值
        /// }
        /// </example>
        public static ulong Read7BitEncodedUInt64(this BinaryReader binaryReader)
        {
            return (ulong)Read7BitEncodedInt64(binaryReader);
        }

        /// <summary>
        /// 向二进制流写入编码过的 64 位无符号整数(编码)。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 64 位无符号整数。</param>
        /// <example>
        /// using (var ms = new MemoryStream())
        /// using (var writer = new BinaryWriter(ms))
        /// {
        ///     writer.Write7BitEncodedUInt64(ulong.MaxValue); // 写入 10 字节
        /// }
        /// </example>
        public static void Write7BitEncodedUInt64(this BinaryWriter binaryWriter, ulong value)
        {
            Write7BitEncodedInt64(binaryWriter, (long)value);
        }

        /// <summary>
        /// 从二进制流读取解密过字符串(异或解密)。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <param name="encryptBytes">密钥数组。</param>
        /// <returns>读取的字符串。</returns>
        /// <example>
        /// byte[] key = new byte[] { 0xAB, 0xCD };
        /// using (var ms = new MemoryStream())
        /// {
        ///     // 先写入加密字符串
        ///     using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        ///     {
        ///         writer.WriteEncryptedString("Hello", key);
        ///     }
        ///     // 再读取加密字符串
        ///     ms.Position = 0;
        ///     using (var reader = new BinaryReader(ms))
        ///     {
        ///         string value = reader.ReadEncryptedString(key); // 返回 "Hello"
        ///     }
        /// }
        /// </example>
        public static string ReadEncryptedString(this BinaryReader binaryReader, byte[] encryptBytes)
        {
            byte length = binaryReader.ReadByte();
            if (length <= 0)
            {
                return null;
            }

            for (byte i = 0; i < length; i++)
            {
                CachedBytes[i] = binaryReader.ReadByte();
            }

            Utility.Encryption.Xor.GetSelfXorBytes(CachedBytes, 0, length, encryptBytes);
            var value = Utility.BitConverter.Bytes2String(CachedBytes, 0, length);
            Array.Clear(CachedBytes, 0, length);
            return value;
        }

        /// <summary>
        /// 向二进制流写入加密字符串(异或加密)。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的字符串。</param>
        /// <param name="encryptBytes">密钥数组。</param>
        /// <example>
        /// byte[] key = new byte[] { 0x12, 0x34, 0x56 };
        /// using (var ms = new MemoryStream())
        /// using (var writer = new BinaryWriter(ms))
        /// {
        ///     writer.WriteEncryptedString("Secret", key); // 写入加密过的字符串
        ///     writer.WriteEncryptedString(null, key); // 写入空标记 (1 字节 0)
        /// }
        /// </example>
        public static void WriteEncryptedString(this BinaryWriter binaryWriter, string value, byte[] encryptBytes)
        {
            if (string.IsNullOrEmpty(value))
            {
                binaryWriter.Write((byte)0);
                return;
            }

            int length = Utility.BitConverter.String2Bytes(value, CachedBytes);
            if (length > byte.MaxValue)
            {
                throw new InvalidOperationException($"字符串 '{value}' 太长，无法加密.");
            }

            Utility.Encryption.Xor.GetSelfXorBytes(CachedBytes, encryptBytes);
            binaryWriter.Write((byte)length);
            binaryWriter.Write(CachedBytes, 0, length);
        }
    }
}
