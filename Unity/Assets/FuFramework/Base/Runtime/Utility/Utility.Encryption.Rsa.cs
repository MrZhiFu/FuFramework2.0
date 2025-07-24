using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GameFrameX.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 加密解密相关的实用函数。
        /// </summary>
        public static partial class Encryption
        {
            /// <summary>
            /// RSA 加密解密类-非对称加密。
            /// 使用此类可以实现 RSA 加密解密、签名验证等功能。
            /// </summary>
            public sealed class Rsa
            {
                private readonly RSACryptoServiceProvider _rsa = null;

                /// <summary>
                /// 使用提供的 RSACryptoServiceProvider 实例初始化 Rsa 类。
                /// </summary>
                /// <param name="rsa">RSACryptoServiceProvider 实例</param>
                public Rsa(RSACryptoServiceProvider rsa)
                {
                    _rsa = rsa;
                }

                /// <summary>
                /// 使用提供的密钥字符串初始化 Rsa 类。
                /// </summary>
                /// <param name="key">密钥字符串</param>
                public Rsa(string key)
                {
                    var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(key);
                    _rsa = rsa;
                }

                /// <summary>
                /// 创建 RSA 密钥对。
                /// </summary>
                /// <returns>包含私钥和公钥的字典</returns>
                public static Dictionary<string, string> Make()
                {
                    var dic = new Dictionary<string, string>();
                    var dsa = new RSACryptoServiceProvider();
                    dic["privateKey"] = dsa.ToXmlString(true);
                    dic["publicKey"]  = dsa.ToXmlString(false);
                    return dic;
                }

                /// <summary>
                /// 使用公钥加密内容。
                /// </summary>
                /// <param name="publicKey">公钥</param>
                /// <param name="content">所加密的内容</param>
                /// <returns>加密后的内容</returns>
                public static string RSAEncrypt(string publicKey, string content)
                {
                    var res = RSAEncrypt(publicKey, Encoding.UTF8.GetBytes(content));
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 不使用公钥加密内容。
                /// </summary>
                /// <param name="content"></param>
                /// <returns></returns>
                public string Encrypt(string content)
                {
                    var res = Encrypt(Encoding.UTF8.GetBytes(content));
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 使用公钥加密内容。
                /// </summary>
                /// <param name="publicKey"></param>
                /// <param name="content"></param>
                /// <returns></returns>
                public static byte[] RSAEncrypt(string publicKey, byte[] content)
                {
                    var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(publicKey);
                    var cipherBytes = rsa.Encrypt(content, false);
                    return cipherBytes;
                }

                /// <summary>
                /// 不使用公钥加密内容。
                /// </summary>
                /// <param name="content"></param>
                /// <returns></returns>
                public byte[] Encrypt(byte[] content)
                {
                    var cipherBytes = _rsa.Encrypt(content, false);
                    return cipherBytes;
                }

                /// <summary>
                /// 使用私钥解密字符串。
                /// </summary>
                /// <param name="privateKey">私钥</param>
                /// <param name="content">加密后的内容</param>
                /// <returns>解密后的内容</returns>
                public static string RSADecrypt(string privateKey, string content)
                {
                    var res = RSADecrypt(privateKey, Convert.FromBase64String(content));
                    return Encoding.UTF8.GetString(res);
                }

                /// <summary>
                /// 使用私钥解密字节数组。
                /// </summary>
                /// <param name="privateKey"></param>
                /// <param name="content"></param>
                /// <returns></returns>
                public static byte[] RSADecrypt(string privateKey, byte[] content)
                {
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(privateKey);
                    byte[] cipherBytes = rsa.Decrypt(content, false);
                    return cipherBytes;
                }

                /// <summary>
                /// 不使用私钥解密字符串。
                /// </summary>
                /// <param name="content"></param>
                /// <returns></returns>
                public string Decrypt(string content)
                {
                    var res = Decrypt(Convert.FromBase64String(content));
                    return Encoding.UTF8.GetString(res);
                }

                /// <summary>
                /// 不使用私钥解密字节数组。
                /// </summary>
                /// <param name="content"></param>
                /// <returns></returns>
                public byte[] Decrypt(byte[] content)
                {
                    var bytes = _rsa.Decrypt(content, false);
                    return bytes;
                }

                /// <summary>
                /// 使用私钥对字节数据进行签名。
                /// </summary>
                /// <param name="dataToSign">要签名的数据</param>
                /// <param name="privateKey">私钥字符串</param>
                /// <returns>签名后的字节数组</returns>
                public static byte[] RSASignData(byte[] dataToSign, string privateKey)
                {
                    try
                    {
                        var rsa = new RSACryptoServiceProvider();
                        rsa.FromXmlString(privateKey);
                        return rsa.SignData(dataToSign, new SHA1CryptoServiceProvider());
                    }
                    catch
                    {
                        return null;
                    }
                }

                /// <summary>
                /// 使用私钥对字符串进行签名。
                /// </summary>
                /// <param name="dataToSign"></param>
                /// <param name="privateKey"></param>
                /// <returns></returns>
                public static string RSASignData(string dataToSign, string privateKey)
                {
                    byte[] res = RSASignData(Encoding.UTF8.GetBytes(dataToSign), privateKey);
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 不使用公钥验证签名字节数组。
                /// </summary>
                /// <param name="dataToSign"></param>
                /// <returns></returns>
                public byte[] SignData(byte[] dataToSign)
                {
                    try
                    {
                        return _rsa.SignData(dataToSign, new SHA1CryptoServiceProvider());
                    }
                    catch
                    {
                        return null;
                    }
                }

                /// <summary>
                /// 不使用公钥验证签名字符串。
                /// </summary>
                /// <param name="dataToSign"></param>
                /// <returns></returns>
                public string SignData(string dataToSign)
                {
                    var res = SignData(Encoding.UTF8.GetBytes(dataToSign));
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 使用公钥验证字节数组的签名。
                /// </summary>
                /// <param name="dataToVerify">要验证的数据</param>
                /// <param name="signedData">签名数据</param>
                /// <param name="publicKey">公钥字符串</param>
                /// <returns>验证结果</returns>
                public static bool RSAVerifyData(byte[] dataToVerify, byte[] signedData, string publicKey)
                {
                    try
                    {
                        var rsa = new RSACryptoServiceProvider();
                        rsa.FromXmlString(publicKey);
                        return rsa.VerifyData(dataToVerify, new SHA1CryptoServiceProvider(), signedData);
                    }
                    catch
                    {
                        return false;
                    }
                }

                /// <summary>
                /// 使用公钥验证字符串的签名。
                /// </summary>
                /// <param name="dataToVerify"></param>
                /// <param name="signedData"></param>
                /// <param name="publicKey"></param>
                /// <returns></returns>
                public static bool RSAVerifyData(string dataToVerify, string signedData, string publicKey)
                {
                    return RSAVerifyData(Encoding.UTF8.GetBytes(dataToVerify), Convert.FromBase64String(signedData), publicKey);
                }

                /// <summary>
                /// 不使用公钥验证字节数组的签名。
                /// </summary>
                /// <param name="dataToVerify"></param>
                /// <param name="signedData"></param>
                /// <returns></returns>
                public bool VerifyData(byte[] dataToVerify, byte[] signedData)
                {
                    try
                    {
                        return _rsa.VerifyData(dataToVerify, new SHA1CryptoServiceProvider(), signedData);
                    }
                    catch
                    {
                        return false;
                    }
                }

                /// <summary>
                /// 不使用公钥验证字符串的签名。
                /// </summary>
                /// <param name="dataToVerify"></param>
                /// <param name="signedData"></param>
                /// <returns></returns>
                public bool VerifyData(string dataToVerify, string signedData)
                {
                    try
                    {
                        return VerifyData(Encoding.UTF8.GetBytes(dataToVerify), Convert.FromBase64String(signedData));
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }
    }
}