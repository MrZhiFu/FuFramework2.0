using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 加密解密相关的实用函数。
        /// </summary>
        public static partial class Encryption
        {
            /// <summary>
            /// DSA（数字签名算法）加密解密相关的实用函数-非对称加密
            /// 使用此类可以对数据进行签名和验证签名，也可以生成密钥对。
            /// </summary>
            public sealed class Dsa
            {
                private readonly DSACryptoServiceProvider _dsa;

                /// <summary>
                /// Dsa 构造函数，使用提供的 DSACryptoServiceProvider 实例初始化。
                /// </summary>
                /// <param name="dsa">DSACryptoServiceProvider 实例</param>
                public Dsa(DSACryptoServiceProvider dsa)
                {
                    _dsa = dsa;
                }

                /// <summary>
                /// Dsa 构造函数，使用提供的密钥字符串初始化。
                /// </summary>
                /// <param name="key">密钥字符串</param>
                public Dsa(string key)
                {
                    var rsa = new DSACryptoServiceProvider();
                    rsa.FromXmlString(key);
                    _dsa = rsa;
                }

                /// <summary>
                /// 创建 DSA 密钥对。
                /// </summary>
                /// <returns>包含私钥和公钥的字典</returns>
                public static Dictionary<string, string> Make()
                {
                    var dic = new Dictionary<string, string>();
                    var dsa = new DSACryptoServiceProvider();
                    dic["privatekey"] = dsa.ToXmlString(true);
                    dic["publickey"] = dsa.ToXmlString(false);
                    return dic;
                }

                /// <summary>
                /// 使用私钥对字节数据进行签名。
                /// </summary>
                /// <param name="dataToSign">要签名的数据</param>
                /// <param name="privateKey">私钥字符串</param>
                /// <returns>签名后的字节数组</returns>
                public static byte[] DsaSignData(byte[] dataToSign, string privateKey)
                {
                    try
                    {
                        var dsa = new DSACryptoServiceProvider();
                        dsa.FromXmlString(privateKey);
                        return dsa.SignData(dataToSign);
                    }
                    catch
                    {
                        return null;
                    }
                }

                /// <summary>
                /// 使用私钥对字符串数据进行签名。
                /// </summary>
                /// <param name="dataToSign">要签名的字符串数据</param>
                /// <param name="privateKey">私钥字符串</param>
                /// <returns>签名后的 Base64 字符串</returns>
                public static string DsaSignData(string dataToSign, string privateKey)
                {
                    var res = DsaSignData(Encoding.UTF8.GetBytes(dataToSign), privateKey);
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 使用 DSA 对字节数据进行签名。
                /// </summary>
                /// <param name="dataToSign">要签名的数据</param>
                /// <returns>签名后的字节数组</returns>
                public byte[] SignData(byte[] dataToSign)
                {
                    try
                    {
                        return _dsa.SignData(dataToSign);
                    }
                    catch
                    {
                        return null;
                    }
                }

                /// <summary>
                /// 使用 DSA 对字符串数据进行签名。
                /// </summary>
                /// <param name="dataToSign">要签名的字符串数据</param>
                /// <returns>签名后的 Base64 字符串</returns>
                public string SignData(string dataToSign)
                {
                    var res = SignData(Encoding.UTF8.GetBytes(dataToSign));
                    return Convert.ToBase64String(res);
                }

                /// <summary>
                /// 使用私钥对字节数据验证签名。
                /// </summary>
                /// <param name="dataToVerify">要验证的数据</param>
                /// <param name="signedData">签名数据</param>
                /// <param name="privateKey">私钥字符串</param>
                /// <returns>验证结果</returns>
                public static bool DsaVerifyData(byte[] dataToVerify, byte[] signedData, string privateKey)
                {
                    try
                    {
                        var dsa = new DSACryptoServiceProvider();
                        dsa.FromXmlString(privateKey);
                        return dsa.VerifyData(dataToVerify, signedData);
                    }
                    catch
                    {
                        return false;
                    }
                }

                /// <summary>
                /// 使用私钥对字符串数据验证签名。
                /// </summary>
                /// <param name="dataToVerify">要验证的字符串数据</param>
                /// <param name="signedData">签名的 Base64 字符串</param>
                /// <param name="privateKey">私钥字符串</param>
                /// <returns>验证结果</returns>
                public static bool DsaVerifyData(string dataToVerify, string signedData, string privateKey)
                {
                    return DsaVerifyData(Encoding.UTF8.GetBytes(dataToVerify), Convert.FromBase64String(signedData), privateKey);
                }

                /// <summary>
                /// 不使用私钥对字节数据验证签名。
                /// </summary>
                /// <param name="dataToVerify">要验证的数据</param>
                /// <param name="signedData">签名数据</param>
                /// <returns>验证结果</returns>
                public bool VerifyData(byte[] dataToVerify, byte[] signedData)
                {
                    try
                    {
                        return _dsa.VerifyData(dataToVerify, signedData);
                    }
                    catch
                    {
                        return false;
                    }
                }

                /// <summary>
                /// 不使用私钥对字符串数据验证签名。
                /// </summary>
                /// <param name="dataToVerify">要验证的字符串数据</param>
                /// <param name="signedData">签名的 Base64 字符串</param>
                /// <returns>验证结果</returns>
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