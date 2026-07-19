using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 字符串相关的扩展函数
    /// 功能：
    ///     1. 快速比较两个字符串内容是否一致。
    ///     2. 快速判断字符串是否以目标字符串结尾。
    ///     3. 快速判断字符串是否以目标字符串开始。
    ///     4. 字符串转字节数组。
    ///     5. 字符串相关判空。
    ///     6. 格式化字符串。
    ///     7. 空字符相关替换。
    ///     8. 中文字符串替换为空字符串。
    ///     9. 字符串分割为整数数组。
    ///     10. 字符串转换为蛇形命名。
    ///     11.根据目录类型字符串创建文件目录。
    ///     12. 从字符串指定位置读取一行。
    /// </summary>
    public static class StringEx
    {
        /// <summary>
        /// 快速比较两个字符串内容是否一致。
        /// 算法原理：两个字符串”从后往前“比较，如果所有字符都相等，则返回true，否则返回false。
        /// </summary>
        /// <param name="self">当前字符串</param>
        /// <param name="target">对比的目标字符串</param>
        /// <returns></returns>
        public static bool EqualsFast(this string self, string target)
        {
            if (self        == null) return target == null;
            if (target      == null) return false;
            if (self.Length != target.Length) return false;

            int aPos = self.Length   - 1;
            int bPos = target.Length - 1;

            while (aPos >= 0 && bPos >= 0 && self[aPos] == target[bPos])
            {
                aPos--;
                bPos--;
            }

            // 如果bPos小于0，说明全部比较完成且所有字符都相等，返回true，否则返回false。
            return bPos < 0;
        }

        /// <summary>
        /// 判断字符串是否以目标字符串结尾。
        /// 算法原理：两个字符串”从后往前“比较，如果所有字符都相等，则返回true，否则返回false。
        /// </summary>
        /// <param name="self">当前字符串</param>
        /// <param name="target">目标字符串</param>
        /// <returns></returns>
        public static bool EndsWithFast(this string self, string target)
        {
            if (self        == null) return target == null;
            if (target      == null) return false;
            if (self.Length < target.Length) return false;

            int ap = self.Length   - 1;
            int bp = target.Length - 1;

            while (ap >= 0 && bp >= 0 && self[ap] == target[bp])
            {
                ap--;
                bp--;
            }

            // 如果bp小于0，说明全部比较完成且所有字符都相等，返回true，否则返回false。
            return bp < 0;
        }

        /// <summary>
        /// 判断字符串是否以目标字符串开始。
        /// 算法原理：两个字符串”从前往后“比较，如果所有字符都相等，则返回true，否则返回false。
        /// </summary>
        /// <param name="self">当前字符串</param>   
        /// <param name="target">目标字符串</param>
        /// <returns></returns>
        public static bool StartsWithFast(this string self, string target)
        {
            if (self        == null) return target == null;
            if (target      == null) return false;
            if (self.Length < target.Length) return false;

            int aLen = self.Length;
            int bLen = target.Length;

            int ap = 0;
            int bp = 0;

            while (ap < aLen && bp < bLen && self[ap] == target[bp])
            {
                ap++;
                bp++;
            }

            return bp == bLen;
        }

        /// <summary>
        /// 字符串转字节数组
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this string self)
        {
            return Encoding.Default.GetBytes(self);
        }

        /// <summary>
        /// 字符串转UTF8字节数组
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static byte[] ToUtf8(this string self)
        {
            return Encoding.UTF8.GetBytes(self);
        }

        /// <summary>
        /// 16进制字符串转字节数组
        /// </summary>
        /// <param name="hexString">字符串</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">字符串长度不是偶数</exception>
        public static byte[] HexToBytes(this string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException($"16进制字符串长度必须是偶数: {hexString}");
            }

            var hexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < hexAsBytes.Length; index++)
            {
                string byteValue = "";
                byteValue         += hexString[index * 2];
                byteValue         += hexString[index * 2 + 1];
                hexAsBytes[index] =  byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return hexAsBytes;
        }

        /// <summary>
        /// 指定的字符串是 null、空还是仅由空白字符组成。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string self)
        {
            const string nullString = "null";
            return self.EqualsFast(nullString) || string.IsNullOrWhiteSpace(self);
        }

        /// <summary>
        /// 指定的字符串是 null 还是 Empty 字符串。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);

        /// <summary>
        /// 指定的字符串不是 null、空还是仅由空白字符组成。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNotNullOrWhiteSpace(this string self) => !self.IsNullOrWhiteSpace();

        /// <summary>
        /// 指定的字符串不是 null、空。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsNotNullOrEmpty(this string self) => !self.IsNullOrEmpty();

        /// <summary>
        /// 格式化字符串
        /// 算法原理：使用string.Format方法格式化字符串，将参数替换为实际值。
        /// <example>
        /// var str = "Hello, {0}!"; // "Hello, World!"
        /// var str2 = "Hello, {0}!".Format("World"); // "Hello, World!"
        /// </example>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(this string text, params object[] args) => string.Format(text, args);

        /// <summary>
        /// 将[\n、\t、\r、空格]替换为空字符串,并返回
        /// </summary>
        /// <param name="self">原始字符串</param>
        /// <returns></returns>
        public static string TrimEmpty(this string self)
        {
            self = self.Replace("\n", string.Empty).Replace(" ", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty);
            return self;
        }

        /// <summary>
        /// 将驼峰命名的字符串转换为下划线分隔的小写形式（蛇形命名）。
        /// </summary>
        /// <param name="input">要转换的字符串。</param>
        /// <returns>转换后的蛇形命名字符串。如果输入为null或空，则返回原字符串。</returns>
        public static string ConvertToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var startUnderscores = Regex.Match(input, @"^_+");
            return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        }

        /// <summary>
        /// 匹配中文正则表达式
        /// </summary>
        private static readonly Regex CnReg = new(@"[\u4e00-\u9fa5]");

        /// <summary>
        /// 替换中文为空字符串
        /// </summary>
        /// <param name="self">原始字符串</param>
        /// <returns></returns>
        public static string TrimZhCn(this string self)
        {
            self = CnReg.Replace(self, string.Empty);
            return self;
        }

        /// <summary>
        /// 将字符串转换为整数数组,分隔符默认为+
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static int[] SplitToIntArray(this string str, char sep = '+')
        {
            if (string.IsNullOrEmpty(str))
                return Array.Empty<int>();

            var   arr = str.Split(sep);
            int[] ret = new int[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
            {
                if (int.TryParse(arr[i], out var t))
                    ret[i] = t;
            }

            return ret;
        }

        /// <summary>
        /// 将字符串转换为二维整数数组,分隔符默认为 ; 与 +
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sep1"></param>
        /// <param name="sep2"></param>
        /// <returns></returns>
        public static int[][] SplitTo2IntArray(this string str, char sep1 = ';', char sep2 = '+')
        {
            if (string.IsNullOrEmpty(str))
                return Array.Empty<int[]>();

            var arr = str.Split(sep1);
            if (arr.Length <= 0)
                return Array.Empty<int[]>();

            int[][] ret = new int[arr.Length][];

            for (int i = 0; i < arr.Length; ++i)
                ret[i] = arr[i].SplitToIntArray(sep2);
            return ret;
        }

        /// <summary>
        /// 根据字符串创建目录,递归
        /// </summary>
        public static void CreateAsDirectory(this string path, bool isFile = false)
        {
            if (isFile)
                path = Path.GetDirectoryName(path);

            if (Directory.Exists(path)) return;
            CreateAsDirectory(path, true);

            if (path == null) return;
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 从指定字符串中的指定位置处开始读取一行。
        /// </summary>
        /// <param name="rawString">指定的字符串。</param>
        /// <param name="position">从指定位置处开始读取一行，读取后将返回下一行开始的位置。</param>
        /// <returns>读取的一行字符串。</returns>
        public static string ReadLine(this string rawString, ref int position)
        {
            if (position < 0) return null;

            var length = rawString.Length;
            var offset = position;

            while (offset < length)
            {
                char ch = rawString[offset];
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        if (offset > position)
                        {
                            string line = rawString.Substring(position, offset - position);
                            position = offset + 1;
                            if ((ch == '\r') && (position < length) && (rawString[position] == '\n'))
                            {
                                position++;
                            }

                            return line;
                        }

                        offset++;
                        position++;
                        break;

                    default:
                        offset++;
                        break;
                }
            }

            if (offset > position)
            {
                string line = rawString.Substring(position, offset - position);
                position = offset;
                return line;
            }

            return null;
        }
    }
}
