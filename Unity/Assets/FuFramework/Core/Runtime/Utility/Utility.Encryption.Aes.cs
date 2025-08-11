using System;
using System.IO;
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
            /// AES 加密解密相关的实用函数-对称加密算法。
            /// AES-高级加密标准，是加密算法的一种标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法
            /// </summary>
            public static class Aes
            {
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
                    if (encryptByte.Length == 0)
                    {
                        throw (new Exception("明文不得为空"));
                    }

                    if (string.IsNullOrEmpty(encryptKey))
                    {
                        throw (new Exception("密钥不得为空"));
                    }

                    byte[]   strEncrypt;
                    byte[]   btIv        = { 224, 131, 122, 101, 37, 254, 33, 17, 19, 28, 212, 130, 45, 65, 43, 32 };
                    byte[]   salt        = { 234, 231, 123, 100, 87, 254, 123, 17, 89, 18, 230, 13, 45, 65, 43, 32 };
                    Rijndael aesProvider = Rijndael.Create();
                    try
                    {
                        MemoryStream        mStream   = new MemoryStream();
                        PasswordDeriveBytes pdb       = new PasswordDeriveBytes(encryptKey, salt);
                        ICryptoTransform    transform = aesProvider.CreateEncryptor(pdb.GetBytes(32), btIv);
                        CryptoStream        cStream   = new CryptoStream(mStream, transform, CryptoStreamMode.Write);
                        cStream.Write(encryptByte, 0, encryptByte.Length);
                        cStream.FlushFinalBlock();
                        strEncrypt = mStream.ToArray();
                        mStream.Close();
                        mStream.Dispose();
                        cStream.Close();
                        cStream.Dispose();
                    }
                    catch (IOException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (CryptographicException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (ArgumentException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    finally
                    {
                        aesProvider.Clear();
                    }

                    return strEncrypt;
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
                    if (decryptByte.Length == 0) throw (new Exception("密文不得为空"));
                    if (string.IsNullOrEmpty(decryptKey)) throw (new Exception("密钥不得为空"));

                    byte[]   strDecrypt;
                    byte[]   btIv        = { 224, 131, 122, 101, 37, 254, 33, 17, 19, 28, 212, 130, 45, 65, 43, 32 };
                    byte[]   salt        = { 234, 231, 123, 100, 87, 254, 123, 17, 89, 18, 230, 13, 45, 65, 43, 32 };
                    Rijndael aesProvider = Rijndael.Create();
                    try
                    {
                        MemoryStream        mStream   = new MemoryStream();
                        PasswordDeriveBytes pdb       = new PasswordDeriveBytes(decryptKey, salt);
                        ICryptoTransform    transform = aesProvider.CreateDecryptor(pdb.GetBytes(32), btIv);
                        CryptoStream        cStream   = new CryptoStream(mStream, transform, CryptoStreamMode.Write);
                        cStream.Write(decryptByte, 0, decryptByte.Length);
                        cStream.FlushFinalBlock();
                        strDecrypt = mStream.ToArray();
                        mStream.Close();
                        mStream.Dispose();
                        cStream.Close();
                        cStream.Dispose();
                    }
                    catch (IOException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (CryptographicException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (ArgumentException ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        // ReSharper disable once PossibleIntendedRethrow
                        throw ex;
                    }
                    finally
                    {
                        aesProvider.Clear();
                    }

                    return strDecrypt;
                }

                #endregion
            }
        }
    }
}