using System.Text;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 类型转换相关的实用函数。
        /// 功能：
        ///     1. 字符与字节数组相互转换。
        ///     2. 整型与字节数组相互转换。
        ///     3. 浮点型与字节数组相互转换。
        ///     4. 布尔值与字节数组相互转换。
        ///     5. 字符串与字节数组相互转换。
        /// </summary>
        public static class BitConverter
        {
            #region 字节数组转字符

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 Unicode 字符。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的字符。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x41, 0x00 }; // 'A' 的 Unicode 编码
            /// char c = Utility.BitConverter.Bytes2Char(bytes); // 返回 'A'
            /// </example>
            public static char Bytes2Char(byte[] value) => System.BitConverter.ToChar(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 Unicode 字符。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的字符。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x41, 0x00 }; // 从索引1开始读取
            /// char c = Utility.BitConverter.Bytes2Char(bytes, 1); // 返回 'A'
            /// </example>
            public static char Bytes2Char(byte[] value, int startIndex) => System.BitConverter.ToChar(value, startIndex);

            #endregion

            #region 字符转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Char2Bytes('A'); // 返回 new byte[] { 0x41, 0x00 }
            /// </example>
            public static byte[] Char2Bytes(char value)
            {
                var buffer = new byte[2];
                Char2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Char2Bytes('A', buffer); // buffer[0]=0x41, buffer[1]=0x00
            /// </example>
            public static void Char2Bytes(char value, byte[] buffer) => Char2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Char2Bytes('A', buffer, 2); // 从索引2开始写入
            /// </example>
            public static void Char2Bytes(char value, byte[] buffer, int startIndex) => Short2Bytes((short)value, buffer, startIndex);

            #endregion

            #region 字节数组转Int16

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 16 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的 16 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0x7F }; // 小端序 32767
            /// short num = Utility.BitConverter.Bytes2Int16(bytes); // 返回 32767
            /// </example>
            public static short Bytes2Int16(byte[] value) => System.BitConverter.ToInt16(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 16 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的 16 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0x7F };
            /// short num = Utility.BitConverter.Bytes2Int16(bytes, 1); // 从索引1开始，返回 32767
            /// </example>
            public static short Bytes2Int16(byte[] value, int startIndex) => System.BitConverter.ToInt16(value, startIndex);

            #endregion

            #region Short转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Short2Bytes(12345); // 返回 2 字节数组
            /// </example>
            public static byte[] Short2Bytes(short value)
            {
                var buffer = new byte[2];
                Short2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Short2Bytes(12345, buffer); // 写入前2字节
            /// </example>
            public static void Short2Bytes(short value, byte[] buffer) => Short2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Short2Bytes(12345, buffer, 3); // 从索引3开始写入
            /// </example>
            public static unsafe void Short2Bytes(short value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new FuException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 2 > buffer.Length) throw new FuException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(short*)(valueRef + startIndex) = value;
                }
            }

            #endregion

            #region 字节数组转UInt16

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 16 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的 16 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0xFF }; // 小端序 65535
            /// ushort num = Utility.BitConverter.Bytes2UInt16(bytes); // 返回 65535
            /// </example>
            public static ushort Bytes2UInt16(byte[] value) => System.BitConverter.ToUInt16(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 16 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的 16 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0xFF };
            /// ushort num = Utility.BitConverter.Bytes2UInt16(bytes, 1); // 从索引1开始，返回 65535
            /// </example>
            public static ushort Bytes2UInt16(byte[] value, int startIndex) => System.BitConverter.ToUInt16(value, startIndex);

            #endregion

            #region UShort转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.UShort2Bytes(60000); // 返回 2 字节数组
            /// </example>
            public static byte[] UShort2Bytes(ushort value)
            {
                var buffer = new byte[2];
                UShort2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.UShort2Bytes(60000, buffer);
            /// </example>
            public static void UShort2Bytes(ushort value, byte[] buffer) => UShort2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.UShort2Bytes(60000, buffer, 2);
            /// </example>
            public static void UShort2Bytes(ushort value, byte[] buffer, int startIndex) => Short2Bytes((short)value, buffer, startIndex);

            #endregion

            #region 字节数组转Int32

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的 32 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的 32 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; // 小端序 int.MaxValue
            /// int num = Utility.BitConverter.Bytes2Int32(bytes); // 返回 2147483647
            /// </example>
            public static int Bytes2Int32(byte[] value) => System.BitConverter.ToInt32(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的 32 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的 32 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0x7F };
            /// int num = Utility.BitConverter.Bytes2Int32(bytes, 1); // 从索引1开始，返回 2147483647
            /// </example>
            public static int Bytes2Int32(byte[] value, int startIndex) => System.BitConverter.ToInt32(value, startIndex);

            #endregion

            #region Int32转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Int2Bytes(123456789); // 返回 4 字节数组
            /// </example>
            public static byte[] Int2Bytes(int value)
            {
                var buffer = new byte[4];
                Int2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Int2Bytes(123456789, buffer);
            /// </example>
            public static void Int2Bytes(int value, byte[] buffer) => Int2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Int2Bytes(123456789, buffer, 2); // 从索引2开始写入
            /// </example>
            public static unsafe void Int2Bytes(int value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new FuException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 4 > buffer.Length) throw new FuException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(int*)(valueRef + startIndex) = value;
                }
            }

            #endregion

            #region 字节数组转UInt32

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的 32 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的 32 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // 小端序 uint.MaxValue
            /// uint num = Utility.BitConverter.Bytes2UInt32(bytes); // 返回 4294967295
            /// </example>
            public static uint Bytes2UInt32(byte[] value) => System.BitConverter.ToUInt32(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的 32 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的 32 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
            /// uint num = Utility.BitConverter.Bytes2UInt32(bytes, 1); // 从索引1开始，返回 4294967295
            /// </example>
            public static uint Bytes2UInt32(byte[] value, int startIndex) => System.BitConverter.ToUInt32(value, startIndex);

            #endregion

            #region UInt32转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.UInt2Bytes(4000000000u); // 返回 4 字节数组
            /// </example>
            public static byte[] UInt2Bytes(uint value)
            {
                var buffer = new byte[4];
                UInt2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.UInt2Bytes(4000000000u, buffer);
            /// </example>
            public static void UInt2Bytes(uint value, byte[] buffer) => UInt2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.UInt2Bytes(4000000000u, buffer, 2);
            /// </example>
            public static void UInt2Bytes(uint value, byte[] buffer, int startIndex) => Int2Bytes((int)value, buffer, startIndex);

            #endregion

            #region 字节数组转Long

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的 64 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的 64 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }; // long.MaxValue
            /// long num = Utility.BitConverter.Bytes2Long(bytes); // 返回 9223372036854775807
            /// </example>
            public static long Bytes2Long(byte[] value) => System.BitConverter.ToInt64(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的 64 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的 64 位有符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F };
            /// long num = Utility.BitConverter.Bytes2Long(bytes, 1); // 从索引1开始
            /// </example>
            public static long Bytes2Long(byte[] value, int startIndex) => System.BitConverter.ToInt64(value, startIndex);

            #endregion

            #region Long转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Long2Bytes(1234567890123L); // 返回 8 字节数组
            /// </example>
            public static byte[] Long2Bytes(long value)
            {
                var buffer = new byte[8];
                Long2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Long2Bytes(1234567890123L, buffer);
            /// </example>
            public static void Long2Bytes(long value, byte[] buffer) => Long2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[16];
            /// Utility.BitConverter.Long2Bytes(1234567890123L, buffer, 4); // 从索引4开始写入
            /// </example>
            public static unsafe void Long2Bytes(long value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new FuException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 8 > buffer.Length) throw new FuException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(long*)(valueRef + startIndex) = value;
                }
            }

            #endregion

            #region 字节数组转ULong

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的 64 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的 64 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; // ulong.MaxValue
            /// ulong num = Utility.BitConverter.Bytes2ULong(bytes); // 返回 18446744073709551615
            /// </example>
            public static ulong Bytes2ULong(byte[] value) => System.BitConverter.ToUInt64(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的 64 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的 64 位无符号整数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            /// ulong num = Utility.BitConverter.Bytes2ULong(bytes, 1); // 从索引1开始
            /// </example>
            public static ulong Bytes2ULong(byte[] value, int startIndex) => System.BitConverter.ToUInt64(value, startIndex);

            #endregion

            #region ULong转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.ULong2Bytes(18446744073709551615UL); // 返回 8 字节数组
            /// </example>
            public static byte[] ULong2Bytes(ulong value)
            {
                var buffer = new byte[8];
                ULong2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[16];
            /// Utility.BitConverter.ULong2Bytes(18446744073709551615UL, buffer);
            /// </example>
            public static void ULong2Bytes(ulong value, byte[] buffer) => ULong2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[16];
            /// Utility.BitConverter.ULong2Bytes(18446744073709551615UL, buffer, 4); // 从索引4开始写入
            /// </example>
            public static void ULong2Bytes(ulong value, byte[] buffer, int startIndex) => Long2Bytes((long)value, buffer, startIndex);

            #endregion

            #region 字节数组转Float

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的单精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的单精度浮点数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x00, 0x80, 0x41 }; // 小端序 16.0f
            /// float num = Utility.BitConverter.Bytes2Float(bytes); // 返回 16.0f
            /// </example>
            public static float Bytes2Float(byte[] value) => System.BitConverter.ToSingle(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的单精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的单精度浮点数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x41 };
            /// float num = Utility.BitConverter.Bytes2Float(bytes, 1); // 从索引1开始
            /// </example>
            public static float Bytes2Float(byte[] value, int startIndex) => System.BitConverter.ToSingle(value, startIndex);

            #endregion

            #region Float转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Float2Bytes(3.14f); // 返回 4 字节数组
            /// </example>
            public static unsafe byte[] Float2Bytes(float value)
            {
                var buffer = new byte[4];
                Int2Bytes(*(int*)&value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Float2Bytes(3.14f, buffer);
            /// </example>
            public static unsafe void Float2Bytes(float value, byte[] buffer) => Float2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Float2Bytes(3.14f, buffer, 2); // 从索引2开始写入
            /// </example>
            public static unsafe void Float2Bytes(float value, byte[] buffer, int startIndex) => Int2Bytes(*(int*)&value, buffer, startIndex);

            #endregion

            #region 字节数组转Double

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的双精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的双精度浮点数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x30, 0x40 }; // 16.0
            /// double num = Utility.BitConverter.Bytes2Double(bytes); // 返回 16.0
            /// </example>
            public static double Bytes2Double(byte[] value) => System.BitConverter.ToDouble(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的双精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的双精度浮点数。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x30, 0x40, 0x00 };
            /// double num = Utility.BitConverter.Bytes2Double(bytes, 1); // 从索引1开始，返回 16.0
            /// </example>
            public static double Bytes2Double(byte[] value, int startIndex) => System.BitConverter.ToDouble(value, startIndex);

            #endregion

            #region Double转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Double2Bytes(3.14159); // 返回 8 字节数组
            /// </example>
            public static unsafe byte[] Double2Bytes(double value)
            {
                var buffer = new byte[8];
                Long2Bytes(*(long*)&value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[16];
            /// Utility.BitConverter.Double2Bytes(3.14159, buffer);
            /// </example>
            public static unsafe void Double2Bytes(double value, byte[] buffer) => Double2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[16];
            /// Utility.BitConverter.Double2Bytes(3.14159, buffer, 4); // 从索引4开始写入
            /// </example>
            public static unsafe void Double2Bytes(double value, byte[] buffer, int startIndex) => Long2Bytes(*(long*)&value, buffer, startIndex);

            #endregion

            #region 字节数组转字符串

            /// <summary>
            /// 返回由字节数组使用 UTF-8 编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>转换后的字符串。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" 的 UTF-8 编码
            /// string str = Utility.BitConverter.Bytes2String(bytes); // 返回 "Hello"
            /// </example>
            public static string Bytes2String(byte[] value) => Bytes2String(value, Encoding.UTF8);

            /// <summary>
            /// 返回由字节数组使用指定编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>转换后的字符串。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            /// string str = Utility.BitConverter.Bytes2String(bytes, Encoding.ASCII); // 返回 "Hello"
            /// </example>
            public static string Bytes2String(byte[] value, Encoding encoding) => encoding.GetString(value);

            /// <summary>
            /// 返回由字节数组使用 UTF-8 编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <param name="length">长度。</param>
            /// <returns>转换后的字符串。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            /// string str = Utility.BitConverter.Bytes2String(bytes, 1, 5); // 从索引1开始取5字节，返回 "Hello"
            /// </example>
            public static string Bytes2String(byte[] value, int startIndex, int length) => Bytes2String(value, startIndex, length, Encoding.UTF8);

            /// <summary>
            /// 返回由字节数组使用指定编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <param name="length">长度。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>转换后的字符串。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            /// string str = Utility.BitConverter.Bytes2String(bytes, 1, 5, Encoding.ASCII); // 返回 "Hello"
            /// </example>
            public static string Bytes2String(byte[] value, int startIndex, int length, Encoding encoding) => encoding.GetString(value, startIndex, length);

            #endregion

            #region 字符串转字节数组

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.String2Bytes("Hello"); // 返回 UTF-8 编码的字节数组
            /// </example>
            public static byte[] String2Bytes(string value) => String2Bytes(value, Encoding.UTF8);

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            /// <example>
            /// byte[] buffer = new byte[100];
            /// int count = Utility.BitConverter.String2Bytes("Hello", buffer); // 返回填充的字节数，这里为 5
            /// </example>
            public static int String2Bytes(string value, byte[] buffer) => String2Bytes(value, Encoding.UTF8, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            /// <example>
            /// byte[] buffer = new byte[100];
            /// int count = Utility.BitConverter.String2Bytes("Hello", buffer, 10); // 从索引10开始写入，返回填充的字节数，这里为 5
            /// </example>
            public static int String2Bytes(string value, byte[] buffer, int startIndex) => String2Bytes(value, Encoding.UTF8, buffer, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.String2Bytes("Hello", Encoding.ASCII); // 返回 ASCII 编码的字节数组
            /// </example>
            public static byte[] String2Bytes(string value, Encoding encoding)
            {
                if (value    == null) throw new FuException("传入的字符串为空.");
                if (encoding == null) throw new FuException("传入的编码为空.");

                return encoding.GetBytes(value);
            }

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            /// <example>
            /// byte[] buffer = new byte[100];
            /// int count = Utility.BitConverter.String2Bytes("Hello", Encoding.ASCII, buffer);
            /// </example>
            public static int String2Bytes(string value, Encoding encoding, byte[] buffer) => String2Bytes(value, encoding, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            /// <example>
            /// byte[] buffer = new byte[100];
            /// int count = Utility.BitConverter.String2Bytes("Hello", Encoding.ASCII, buffer, 10); // 从索引10开始写入
            /// </example>
            public static int String2Bytes(string value, Encoding encoding, byte[] buffer, int startIndex)
            {
                if (value    == null) throw new FuException("传入的字符串为空.");
                if (encoding == null) throw new FuException("传入的编码为空.");

                return encoding.GetBytes(value, 0, value.Length, buffer, startIndex);
            }

            #endregion

            #region 字节数组转布尔值

            /// <summary>
            /// 返回由字节数组中首字节转换来的布尔值。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>如果 value 中的首字节非零，则为 true，否则为 false。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 1, 0, 1 };
            /// bool flag = Utility.BitConverter.Bytes2Bool(bytes); // 返回 true
            /// </example>
            public static bool Bytes2Bool(byte[] value) => System.BitConverter.ToBoolean(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的一个字节转换来的布尔值。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>如果 value 中指定位置的字节非零，则为 true，否则为 false。</returns>
            /// <example>
            /// byte[] bytes = new byte[] { 0, 0, 1 };
            /// bool flag = Utility.BitConverter.Bytes2Bool(bytes, 2); // 返回 true
            /// </example>
            public static bool Bytes2Bool(byte[] value, int startIndex) => System.BitConverter.ToBoolean(value, startIndex);

            #endregion

            #region Bool转字节数组

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            /// <example>
            /// byte[] bytes = Utility.BitConverter.Bool2Bytes(true); // 返回 new byte[] { 1 }
            /// byte[] bytes2 = Utility.BitConverter.Bool2Bytes(false); // 返回 new byte[] { 0 }
            /// </example>
            public static byte[] Bool2Bytes(bool value)
            {
                var buffer = new byte[1];
                Bool2Bytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Bool2Bytes(true, buffer); // buffer[0] = 1
            /// </example>
            public static void Bool2Bytes(bool value, byte[] buffer) => Bool2Bytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <example>
            /// byte[] buffer = new byte[10];
            /// Utility.BitConverter.Bool2Bytes(true, buffer, 5); // buffer[5] = 1
            /// </example>
            public static void Bool2Bytes(bool value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new FuException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 1 > buffer.Length) throw new FuException("开始索引超出范围.");
                buffer[startIndex] = value ? (byte)1 : (byte)0;
            }

            #endregion
        }
    }
}