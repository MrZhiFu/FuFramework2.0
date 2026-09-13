using System;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    public static partial class Utility
    {
        /// <summary>
        /// 哈希计算相关的实用函数。
        /// </summary>
        public static partial class Hash
        {
            /// <summary>
            /// Md5哈希编码
            /// </summary>
            public static class MD5
            {
                /// <summary>
                /// Md5加密算法
                /// </summary>
                private static readonly System.Security.Cryptography.MD5 Md5 = System.Security.Cryptography.MD5.Create();

                /// <summary>
                /// 获取字符串的Md5值
                /// </summary>
                /// <param name="input"></param>
                /// <returns></returns>
                // ReSharper disable once MemberHidesStaticFromOuterClass
                public static string Hash(string input)
                {
                    var data = Md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                    return ToHash(data);
                }

                /// <summary>
                /// 获取流的Md5值
                /// </summary>
                /// <param name="input"></param>
                /// <returns></returns>
                // ReSharper disable once MemberHidesStaticFromOuterClass
                public static string Hash(Stream input)
                {
                    var data = Md5.ComputeHash(input);
                    return ToHash(data);
                }

                /// <summary>
                /// 验证Md5值是否一致
                /// </summary>
                /// <param name="input"></param>
                /// <param name="hash"></param>
                /// <returns></returns>
                public static bool IsVerify(string input, string hash)
                {
                    if (input == null || string.IsNullOrEmpty(hash)) return false;

                    // 必须先用同一算法计算 input 的 MD5，再与传入的 hash 比对（原实现直接比对明文与哈希，恒为 false）
                    var actualHash = Hash(input);
                    return StringComparer.OrdinalIgnoreCase.Equals(actualHash, hash);
                }

                /// <summary>
                /// 获取指定文件路径的Md5值
                /// </summary>
                /// <param name="filePath"></param>
                /// <returns></returns>
                public static string FileHash(string filePath)
                {
                    using var file = new FileStream(filePath, FileMode.Open);
                    return Hash(file);
                }

                /// <summary>
                /// 将字节数组转换为16进制字符串
                /// </summary>
                /// <param name="data"></param>
                /// <returns></returns>
                private static string ToHash(byte[] data)
                {
                    var sb = new StringBuilder();
                    foreach (var t in data)
                    {
                        sb.Append(t.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
        }
    }
}
