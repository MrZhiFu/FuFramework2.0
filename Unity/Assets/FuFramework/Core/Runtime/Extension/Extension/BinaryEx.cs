using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 对 BinaryReader 和 BinaryWriter 的扩展方法。
    /// 功能：
    /// 1. 7 位编码整数：
    ///    - Read7BitEncodedInt32()：从二进制流读取编码后的 32 位有符号整数。
    ///    - Write7BitEncodedInt32()：向二进制流写入编码后的 32 位有符号整数。
    ///    - Read7BitEncodedUInt32()：从二进制流读取编码后的 32 位无符号整数。
    ///    - Write7BitEncodedUInt32()：向二进制流写入编码后的 32 位无符号整数。
    ///    - Read7BitEncodedInt64()：从二进制流读取编码后的 64 位有符号整数。
    ///    - Write7BitEncodedInt64()：向二进制流写入编码后的 64 位有符号整数。
    ///    - Read7BitEncodedUInt64()：从二进制流读取编码后的 64 位无符号整数。
    ///    - Write7BitEncodedUInt64()：向二进制流写入编码后的 64 位无符号整数。
    /// 2. 加密字符串：
    ///    - ReadEncryptedString()：从二进制流读取加密字符串。
    ///    - WriteEncryptedString()：向二进制流写入加密字符串。
    /// 注：
    /// 1. 7 位编码整数：
    ///    - 7 位编码整数是一种特殊的编码方式，它可以将整数编码为 7 位或更少的字节。
    ///    - 编码整数的过程是：
    ///      - 整数除以 128，取余数作为当前字节的低 7 位。
    ///      - 除以 128 取余数的结果作为下一个字节的高 7 位，并将当前字节的低 7 位设置为 1。
    ///      - 重复上述过程，直到整数为 0。
    ///    - 解码整数的过程是：
    ///      - 读取第一个字节，将其低 7 位作为整数的低 7 位。
    ///      - 重复上述过程，直到整数为 0。
    /// 2. 加密字符串：
    ///    - 加密字符串的过程是：
    ///      - 读取字符串的长度，并将其作为第一个字节写入二进制流。
    ///      - 读取字符串的每个字节，将其与密钥数组的每个字节进行异或运算，并将结果作为字节写入二进制流。
    ///    - 解密字符串的过程是：
    ///      - 读取第一个字节，并将其作为字符串的长度。
    ///      - 读取字符串的每个字节，将其与密钥数组的每个字节进行异或运算，并将结果作为字节写入缓存数组。
    ///      - 将缓存数组转换为字符串并返回。
    /// </summary>
    public static class BinaryEx
    {
        private static readonly byte[] s_CachedBytes = new byte[byte.MaxValue + 1];

        /// <summary>
        /// 从二进制流读取编码后的 32 位有符号整数。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 32 位有符号整数。</returns>
        public static int Read7BitEncodedInt32(this BinaryReader binaryReader)
        {
            int  value = 0;
            int  shift = 0;
            byte b;
            do
            {
                if (shift >= 35)
                {
                    throw new FuException("7 bit encoded int is invalid.");
                }

                b     =  binaryReader.ReadByte();
                value |= (b & 0x7f) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return value;
        }

        /// <summary>
        /// 向二进制流写入编码后的 32 位有符号整数。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 32 位有符号整数。</param>
        public static void Write7BitEncodedInt32(this BinaryWriter binaryWriter, int value)
        {
            uint num = (uint)value;
            while (num >= 0x80)
            {
                binaryWriter.Write((byte)(num | 0x80));
                num >>= 7;
            }

            binaryWriter.Write((byte)num);
        }

        /// <summary>
        /// 从二进制流读取编码后的 32 位无符号整数。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 32 位无符号整数。</returns>
        public static uint Read7BitEncodedUInt32(this BinaryReader binaryReader)
        {
            return (uint)Read7BitEncodedInt32(binaryReader);
        }

        /// <summary>
        /// 向二进制流写入编码后的 32 位无符号整数。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 32 位无符号整数。</param>
        public static void Write7BitEncodedUInt32(this BinaryWriter binaryWriter, uint value)
        {
            Write7BitEncodedInt32(binaryWriter, (int)value);
        }

        /// <summary>
        /// 从二进制流读取编码后的 64 位有符号整数。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 64 位有符号整数。</returns>
        public static long Read7BitEncodedInt64(this BinaryReader binaryReader)
        {
            long value = 0L;
            int  shift = 0;
            byte b;
            do
            {
                if (shift >= 70)
                {
                    throw new FuException("7 bit encoded int is invalid.");
                }

                b     =  binaryReader.ReadByte();
                value |= (b & 0x7fL) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return value;
        }

        /// <summary>
        /// 向二进制流写入编码后的 64 位有符号整数。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 64 位有符号整数。</param>
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
        /// 从二进制流读取编码后的 64 位无符号整数。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <returns>读取的 64 位无符号整数。</returns>
        public static ulong Read7BitEncodedUInt64(this BinaryReader binaryReader)
        {
            return (ulong)Read7BitEncodedInt64(binaryReader);
        }

        /// <summary>
        /// 向二进制流写入编码后的 64 位无符号整数。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的 64 位无符号整数。</param>
        public static void Write7BitEncodedUInt64(this BinaryWriter binaryWriter, ulong value)
        {
            Write7BitEncodedInt64(binaryWriter, (long)value);
        }

        /// <summary>
        /// 从二进制流读取加密字符串。
        /// </summary>
        /// <param name="binaryReader">要读取的二进制流。</param>
        /// <param name="encryptBytes">密钥数组。</param>
        /// <returns>读取的字符串。</returns>
        public static string ReadEncryptedString(this BinaryReader binaryReader, byte[] encryptBytes)
        {
            byte length = binaryReader.ReadByte();
            if (length <= 0)
            {
                return null;
            }

            for (byte i = 0; i < length; i++)
            {
                s_CachedBytes[i] = binaryReader.ReadByte();
            }

            Utility.Encryption.GetSelfXorBytes(s_CachedBytes, 0, length, encryptBytes);
            var value = Utility.Converter.GetString(s_CachedBytes, 0, length);
            Array.Clear(s_CachedBytes, 0, length);
            return value;
        }

        /// <summary>
        /// 向二进制流写入加密字符串。
        /// </summary>
        /// <param name="binaryWriter">要写入的二进制流。</param>
        /// <param name="value">要写入的字符串。</param>
        /// <param name="encryptBytes">密钥数组。</param>
        public static void WriteEncryptedString(this BinaryWriter binaryWriter, string value, byte[] encryptBytes)
        {
            if (string.IsNullOrEmpty(value))
            {
                binaryWriter.Write((byte)0);
                return;
            }

            int length = Utility.Converter.GetBytes(value, s_CachedBytes);
            if (length > byte.MaxValue)
            {
                throw new FuException(Utility.Text.Format("String '{0}' is too long.", value));
            }

            Utility.Encryption.GetSelfXorBytes(s_CachedBytes, encryptBytes);
            binaryWriter.Write((byte)length);
            binaryWriter.Write(s_CachedBytes, 0, length);
        }
    }
}