using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FuFramework.Foundation.Hash;

/// <summary>
/// SHA-256 哈希算法工具类
/// </summary>
public static class Sha256Helper
{
	/// <summary>
	/// 计算字符串的 SHA-256 哈希值
	/// </summary>
	/// <param name="input">要计算哈希值的字符串</param>
	/// <param name="encoding">字符串编码方式，默认为 UTF8</param>
	/// <returns>64个字符的十六进制字符串形式的哈希值</returns>
	public static string ComputeHash(string input, Encoding encoding = null)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		return ComputeHash(encoding.GetBytes(input));
	}

	/// <summary>
	/// 计算字节数组的 SHA-256 哈希值
	/// </summary>
	/// <param name="buffer">要计算哈希值的字节数组</param>
	/// <returns>64个字符的十六进制字符串形式的哈希值</returns>
	public static string ComputeHash(byte[] buffer)
	{
		if (buffer == null || buffer.Length == 0)
		{
			return string.Empty;
		}
		using SHA256 sHA = SHA256.Create();
		return BitConverter.ToString(sHA.ComputeHash(buffer)).Replace("-", "").ToLower();
	}

	/// <summary>
	/// 计算文件的 SHA-256 哈希值
	/// </summary>
	/// <param name="filePath">文件路径</param>
	/// <returns>64个字符的十六进制字符串形式的哈希值，如果文件不存在则返回空字符串</returns>
	public static string ComputeFileHash(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return string.Empty;
		}
		using SHA256 sHA = SHA256.Create();
		using FileStream inputStream = File.OpenRead(filePath);
		return BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", "").ToLower();
	}

	/// <summary>
	/// 验证字符串的 SHA-256 哈希值是否匹配
	/// </summary>
	/// <param name="input">原始字符串</param>
	/// <param name="hash">要验证的哈希值</param>
	/// <param name="encoding">字符串编码方式，默认为 UTF8</param>
	/// <returns>如果哈希值匹配则返回true，否则返回false</returns>
	public static bool VerifyHash(string input, string hash, Encoding encoding = null)
	{
		if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
		{
			return false;
		}
		return string.Equals(ComputeHash(input, encoding), hash, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 验证字节数组的 SHA-256 哈希值是否匹配
	/// </summary>
	/// <param name="buffer">原始字节数组</param>
	/// <param name="hash">要验证的哈希值</param>
	/// <returns>如果哈希值匹配则返回true，否则返回false</returns>
	public static bool VerifyHash(byte[] buffer, string hash)
	{
		if (buffer == null || buffer.Length == 0 || string.IsNullOrEmpty(hash))
		{
			return false;
		}
		return string.Equals(ComputeHash(buffer), hash, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 验证文件的 SHA-256 哈希值是否匹配
	/// </summary>
	/// <param name="filePath">文件路径</param>
	/// <param name="hash">要验证的哈希值</param>
	/// <returns>如果哈希值匹配则返回true，否则返回false</returns>
	public static bool VerifyFileHash(string filePath, string hash)
	{
		if (!File.Exists(filePath) || string.IsNullOrEmpty(hash))
		{
			return false;
		}
		return string.Equals(ComputeFileHash(filePath), hash, StringComparison.OrdinalIgnoreCase);
	}
}
