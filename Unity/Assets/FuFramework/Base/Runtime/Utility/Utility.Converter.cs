//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Text;
using UnityEngine.Scripting;

namespace GameFrameX.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 类型转换相关的实用函数。
        /// 功能：
        /// 1. 屏幕像素和厘米与英寸的转换。
        /// 2. 布尔值和字节数组的转换。
        /// 3. Unicode字符和字节数组的转换。
        /// 4. 字节数组和其他类型值的转换。
        /// 5. 其他类型值和字节数组的转换。
        /// </summary>
        public static class Converter
        {
            /// 英寸到厘米(1英寸 = 2.54厘米)
            private const float InchesToCentimeters = 2.54f;

            /// 厘米到英寸(1厘米 = 0.3937英寸)
            private const float CentimetersToInches = 1f / InchesToCentimeters;

            /// <summary>
            /// 获取数据在此计算机结构中存储时的字节顺序。
            /// </summary>
            public static bool IsLittleEndian => BitConverter.IsLittleEndian;

            /// <summary>
            /// 获取或设置屏幕每英寸点数。
            /// </summary>
            public static float ScreenDpi { get; set; }

            /// <summary>
            /// 将像素转换为厘米。
            /// </summary>
            /// <param name="pixels">像素。</param>
            /// <returns>厘米。</returns>
            public static float GetCentimetersFromPixels(float pixels)
            {
                if (ScreenDpi <= 0) throw new GameFrameworkException("您必须先设置屏幕 DPI.");
                return InchesToCentimeters * pixels / ScreenDpi;
            }

            /// <summary>
            /// 将厘米转换为像素。
            /// </summary>
            /// <param name="centimeters">厘米。</param>
            /// <returns>像素。</returns>
            public static float GetPixelsFromCentimeters(float centimeters)
            {
                if (ScreenDpi <= 0) throw new GameFrameworkException("您必须先设置屏幕 DPI.");
                return CentimetersToInches * centimeters * ScreenDpi;
            }

            /// <summary>
            /// 将像素转换为英寸。
            /// </summary>
            /// <param name="pixels">像素。</param>
            /// <returns>英寸。</returns>
            public static float GetInchesFromPixels(float pixels)
            {
                if (ScreenDpi <= 0) throw new GameFrameworkException("您必须先设置屏幕 DPI.");
                return pixels / ScreenDpi;
            }

            /// <summary>
            /// 将英寸转换为像素。
            /// </summary>
            /// <param name="inches">英寸。</param>
            /// <returns>像素。</returns>
            public static float GetPixelsFromInches(float inches)
            {
                if (ScreenDpi <= 0) throw new GameFrameworkException("您必须先设置屏幕 DPI.");
                return inches * ScreenDpi;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(bool value)
            {
                var buffer = new byte[1];
                GetBytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(bool value, byte[] buffer) => GetBytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的布尔值。
            /// </summary>
            /// <param name="value">要转换的布尔值。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static void GetBytes(bool value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new GameFrameworkException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 1 > buffer.Length) throw new GameFrameworkException("开始索引超出范围.");
                buffer[startIndex] = value ? (byte)1 : (byte)0;
            }

            /// <summary>
            /// 返回由字节数组中首字节转换来的布尔值。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>如果 value 中的首字节非零，则为 true，否则为 false。</returns>
            public static bool GetBoolean(byte[] value) => BitConverter.ToBoolean(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的一个字节转换来的布尔值。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>如果 value 中指定位置的字节非零，则为 true，否则为 false。</returns>
            public static bool GetBoolean(byte[] value, int startIndex) => BitConverter.ToBoolean(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(char value)
            {
                var buffer = new byte[2];
                GetBytes((short)value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(char value, byte[] buffer) => GetBytes((short)value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 Unicode 字符值。
            /// </summary>
            /// <param name="value">要转换的字符。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static void GetBytes(char value, byte[] buffer, int startIndex) => GetBytes((short)value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 Unicode 字符。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的字符。</returns>
            public static char GetChar(byte[] value) => BitConverter.ToChar(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 Unicode 字符。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的字符。</returns>
            public static char GetChar(byte[] value, int startIndex) => BitConverter.ToChar(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(short value)
            {
                var buffer = new byte[2];
                GetBytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(short value, byte[] buffer) => GetBytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static unsafe void GetBytes(short value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new GameFrameworkException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 2 > buffer.Length) throw new GameFrameworkException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(short*)(valueRef + startIndex) = value;
                }
            }

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 16 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的 16 位有符号整数。</returns>
            public static short GetInt16(byte[] value) => BitConverter.ToInt16(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 16 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的 16 位有符号整数。</returns>
            public static short GetInt16(byte[] value, int startIndex) => BitConverter.ToInt16(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(ushort value)
            {
                var buffer = new byte[2];
                GetBytes((short)value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(ushort value, byte[] buffer) => GetBytes((short)value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 16 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static void GetBytes(ushort value, byte[] buffer, int startIndex) => GetBytes((short)value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前两个字节转换来的 16 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由两个字节构成的 16 位无符号整数。</returns>
            public static ushort GetUInt16(byte[] value) => BitConverter.ToUInt16(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的两个字节转换来的 16 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由两个字节构成的 16 位无符号整数。</returns>
            public static ushort GetUInt16(byte[] value, int startIndex) => BitConverter.ToUInt16(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(int value)
            {
                var buffer = new byte[4];
                GetBytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(int value, byte[] buffer) => GetBytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static unsafe void GetBytes(int value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new GameFrameworkException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 4 > buffer.Length) throw new GameFrameworkException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(int*)(valueRef + startIndex) = value;
                }
            }

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的 32 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的 32 位有符号整数。</returns>
            public static int GetInt32(byte[] value) => BitConverter.ToInt32(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的 32 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的 32 位有符号整数。</returns>
            public static int GetInt32(byte[] value, int startIndex) => BitConverter.ToInt32(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(uint value)
            {
                var buffer = new byte[4];
                GetBytes((int)value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(uint value, byte[] buffer) => GetBytes((int)value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 32 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static void GetBytes(uint value, byte[] buffer, int startIndex) => GetBytes((int)value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的 32 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的 32 位无符号整数。</returns>
            public static uint GetUInt32(byte[] value) => BitConverter.ToUInt32(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的 32 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的 32 位无符号整数。</returns>
            public static uint GetUInt32(byte[] value, int startIndex) => BitConverter.ToUInt32(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(long value)
            {
                var buffer = new byte[8];
                GetBytes(value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(long value, byte[] buffer) => GetBytes(value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位有符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static unsafe void GetBytes(long value, byte[] buffer, int startIndex)
            {
                if (buffer == null) throw new GameFrameworkException("传入的结果Buffer为空.");
                if (startIndex < 0 || startIndex + 8 > buffer.Length) throw new GameFrameworkException("开始索引超出范围.");

                fixed (byte* valueRef = buffer)
                {
                    *(long*)(valueRef + startIndex) = value;
                }
            }

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的 64 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的 64 位有符号整数。</returns>
            public static long GetInt64(byte[] value) => BitConverter.ToInt64(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的 64 位有符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的 64 位有符号整数。</returns>
            public static long GetInt64(byte[] value, int startIndex) => BitConverter.ToInt64(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(ulong value)
            {
                var buffer = new byte[8];
                GetBytes((long)value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static void GetBytes(ulong value, byte[] buffer) => GetBytes((long)value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的 64 位无符号整数值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static void GetBytes(ulong value, byte[] buffer, int startIndex) => GetBytes((long)value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的 64 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的 64 位无符号整数。</returns>
            public static ulong GetUInt64(byte[] value) => BitConverter.ToUInt64(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的 64 位无符号整数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的 64 位无符号整数。</returns>
            public static ulong GetUInt64(byte[] value, int startIndex) => BitConverter.ToUInt64(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static unsafe byte[] GetBytes(float value)
            {
                var buffer = new byte[4];
                GetBytes(*(int*)&value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static unsafe void GetBytes(float value, byte[] buffer) => GetBytes(*(int*)&value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的单精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static unsafe void GetBytes(float value, byte[] buffer, int startIndex) => GetBytes(*(int*)&value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前四个字节转换来的单精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由四个字节构成的单精度浮点数。</returns>
            public static float GetSingle(byte[] value) => BitConverter.ToSingle(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的四个字节转换来的单精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由四个字节构成的单精度浮点数。</returns>
            public static float GetSingle(byte[] value, int startIndex) => BitConverter.ToSingle(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static unsafe byte[] GetBytes(double value)
            {
                var buffer = new byte[8];
                GetBytes(*(long*)&value, buffer, 0);
                return buffer;
            }

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            public static unsafe void GetBytes(double value, byte[] buffer) => GetBytes(*(long*)&value, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定的双精度浮点值。
            /// </summary>
            /// <param name="value">要转换的数字。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            public static unsafe void GetBytes(double value, byte[] buffer, int startIndex) => GetBytes(*(long*)&value, buffer, startIndex);

            /// <summary>
            /// 返回由字节数组中前八个字节转换来的双精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>由八个字节构成的双精度浮点数。</returns>
            public static double GetDouble(byte[] value) => BitConverter.ToDouble(value, 0);

            /// <summary>
            /// 返回由字节数组中指定位置的八个字节转换来的双精度浮点数。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <returns>由八个字节构成的双精度浮点数。</returns>
            public static double GetDouble(byte[] value, int startIndex) => BitConverter.ToDouble(value, startIndex);

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(string value) => GetBytes(value, Encoding.UTF8);

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            public static int GetBytes(string value, byte[] buffer) => GetBytes(value, Encoding.UTF8, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取 UTF-8 编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            public static int GetBytes(string value, byte[] buffer, int startIndex) => GetBytes(value, Encoding.UTF8, buffer, startIndex);

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>用于存放结果的字节数组。</returns>
            public static byte[] GetBytes(string value, Encoding encoding)
            {
                if (value    == null) throw new GameFrameworkException("传入的字符串为空.");
                if (encoding == null) throw new GameFrameworkException("传入的编码为空.");

                return encoding.GetBytes(value);
            }

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            public static int GetBytes(string value, Encoding encoding, byte[] buffer) => GetBytes(value, encoding, buffer, 0);

            /// <summary>
            /// 以字节数组的形式获取指定编码的字符串。
            /// </summary>
            /// <param name="value">要转换的字符串。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <param name="buffer">用于存放结果的字节数组。</param>
            /// <param name="startIndex">buffer 内的起始位置。</param>
            /// <returns>buffer 内实际填充了多少字节。</returns>
            public static int GetBytes(string value, Encoding encoding, byte[] buffer, int startIndex)
            {
                if (value    == null) throw new GameFrameworkException("传入的字符串为空.");
                if (encoding == null) throw new GameFrameworkException("传入的编码为空.");

                return encoding.GetBytes(value, 0, value.Length, buffer, startIndex);
            }

            /// <summary>
            /// 返回由字节数组使用 UTF-8 编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <returns>转换后的字符串。</returns>
            public static string GetString(byte[] value) => GetString(value, Encoding.UTF8);

            /// <summary>
            /// 返回由字节数组使用指定编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>转换后的字符串。</returns>
            public static string GetString(byte[] value, Encoding encoding)
            {
                if (value    == null) throw new GameFrameworkException("传入的字节数组为空.");
                if (encoding == null) throw new GameFrameworkException("传入的编码为空.");

                return encoding.GetString(value);
            }

            /// <summary>
            /// 返回由字节数组使用 UTF-8 编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <param name="length">长度。</param>
            /// <returns>转换后的字符串。</returns>
            public static string GetString(byte[] value, int startIndex, int length) => GetString(value, startIndex, length, Encoding.UTF8);

            /// <summary>
            /// 返回由字节数组使用指定编码转换成的字符串。
            /// </summary>
            /// <param name="value">字节数组。</param>
            /// <param name="startIndex">value 内的起始位置。</param>
            /// <param name="length">长度。</param>
            /// <param name="encoding">要使用的编码。</param>
            /// <returns>转换后的字符串。</returns>
            public static string GetString(byte[] value, int startIndex, int length, Encoding encoding)
            {
                if (value    == null) throw new GameFrameworkException("传入的字节数组为空.");
                if (encoding == null) throw new GameFrameworkException("传入的编码为空.");

                return encoding.GetString(value, startIndex, length);
            }
        }
    }
}