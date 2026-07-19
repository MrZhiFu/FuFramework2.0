using System;
using System.Text;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    public static partial class Utility
    {
        /// <summary>
        /// 加密解密相关的实用函数。
        /// 功能：
        ///     1. AES对称加密解密算法。
        ///     2. 使用 code密钥 做异或运算的加密解密。
        ///     3. DSA非对称加密解密算法。
        ///     4. RSA非对称加密解密算法。
        /// </summary>
        public static partial class Encryption
        {
            /// <summary>
            /// AES 加密解密相关的实用函数-对称加密算法。
            /// AES-高级加密标准，是加密算法的一种标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法
            /// </summary>
            public static class Aes
            {
                /// <summary>
                /// 用于密钥派生的盐值(Salt)
                /// - 作用：增加密钥推导的复杂度，防止彩虹表攻击
                /// - 要求：固定值，长度为16字节(128位)
                /// </summary>
                private static readonly byte[] Salt = { 234, 231, 123, 100, 87, 254, 123, 17, 89, 18, 230, 13, 45, 65, 43, 32 };

                /// <summary>
                /// 初始化向量(Initialization Vector - IV)
                /// - 作用：确保即使相同的明文使用相同的密钥加密，也会产生不同的密文
                /// - 要求：固定值，长度为16字节(128位)，与AES块大小一致
                /// </summary>
                private static readonly byte[] BtIv = { 224, 131, 122, 101, 37, 254, 33, 17, 19, 28, 212, 130, 45, 65, 43, 32 };

                #region 加密

                /// <summary>
                /// AES 加密字符串
                /// </summary>
                /// <param name="encryptStr">待加密密文</param>
                /// <param name="encryptKey">加密密钥</param>
                public static string AesEncrypt(string encryptStr, string encryptKey)
                {
                    return Convert.ToBase64String(AesEncrypt(Encoding.UTF8.GetBytes(encryptStr), encryptKey));
                }

                /// <summary>
                /// AES 加密字节数组
                /// </summary>
                /// <param name="encryptByte">待加密的字节数组</param>
                /// <param name="encryptKey">加密密钥</param>
                public static byte[] AesEncrypt(byte[] encryptByte, string encryptKey)
                {
                    if (encryptByte == null || encryptByte.Length == 0) throw new ArgumentException("明文不得为空");
                    if (string.IsNullOrEmpty(encryptKey)) throw new ArgumentException("密钥不得为空");

                    using var aes          = System.Security.Cryptography.Aes.Create();
                    using var derivedBytes = new Rfc2898DeriveBytes(encryptKey, Salt, 10000, HashAlgorithmName.SHA256);

                    aes.Key     = derivedBytes.GetBytes(32);
                    aes.IV      = BtIv;
                    aes.Mode    = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var encryptor = aes.CreateEncryptor();
                    return encryptor.TransformFinalBlock(encryptByte, 0, encryptByte.Length);
                }

                #endregion

                #region 解密

                /// <summary>
                /// AES 解密字符串
                /// </summary>
                /// <param name="decryptStr">待解密密文</param>
                /// <param name="decryptKey">解密密钥</param>
                public static string AesDecrypt(string decryptStr, string decryptKey)
                {
                    return Encoding.UTF8.GetString((AesDecrypt(Convert.FromBase64String(decryptStr), decryptKey)));
                }

                /// <summary>
                /// AES 解密字节数组
                /// </summary>
                /// <param name="decryptByte">待解密的字节数组</param>
                /// <param name="decryptKey">解密密钥</param>
                public static byte[] AesDecrypt(byte[] decryptByte, string decryptKey)
                {
                    if (decryptByte == null || decryptByte.Length == 0) throw new ArgumentException("密文不得为空");
                    if (string.IsNullOrEmpty(decryptKey)) throw new ArgumentException("密钥不得为空");

                    using var aes          = System.Security.Cryptography.Aes.Create();
                    using var derivedBytes = new Rfc2898DeriveBytes(decryptKey, Salt, 10000, HashAlgorithmName.SHA256);

                    aes.Key     = derivedBytes.GetBytes(32);
                    aes.IV      = BtIv;
                    aes.Mode    = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var decryptor = aes.CreateDecryptor();
                    return decryptor.TransformFinalBlock(decryptByte, 0, decryptByte.Length);
                }

                #endregion
            }
        }
    }
}
