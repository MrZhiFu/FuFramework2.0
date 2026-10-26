using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 发送状态：待发消息的序列化字节流载体，由发送管线写入、SendCallback 从 Position 增量续发，发完 Reset 清空复用。
	/// </summary>
	internal sealed class SendState : IDisposable
	{
		/// <summary>
		/// 默认缓冲区长度
		/// </summary>
		private const int DEFAULT_BUFFER_LENGTH = 1024 * 64;

		/// <summary>
		/// 是否已释放
		/// </summary>
		private bool m_disposed;

		/// <summary>
		/// 发送数据流
		/// </summary>
		public MemoryStream CacheStream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		/// <summary>
		/// 重置发送数据流
		/// </summary>
		public void Reset()
		{
			CacheStream.Position = 0L;
			CacheStream.SetLength(0L);
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Dispose()
		{
			if (m_disposed) return;

			if (CacheStream != null)
			{
				CacheStream.Dispose();
				CacheStream = null;
			}

			m_disposed = true;
		}
	}
}