using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 消息发送状态
	/// </summary>
	internal sealed class SendState : IDisposable
	{
		/// <summary>
		/// 默认缓冲区长度
		/// </summary>
		private const int  DEFAULT_BUFFER_LENGTH = 1024 * 64;

		/// <summary>
		/// 是否已释放
		/// </summary>
		private       bool m_disposed;

		/// <summary>
		/// 发送数据流
		/// </summary>
		public MemoryStream Stream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		/// <summary>
		/// 重置发送数据流
		/// </summary>
		public void Reset()
		{
			Stream.Position = 0L;
			Stream.SetLength(0L);
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		/// <param name="disposing">是否释放托管资源</param>
		private void Dispose(bool disposing)
		{
			if (m_disposed) return;

			if (disposing && Stream != null)
			{
				Stream.Dispose();
				Stream = null;
			}

			m_disposed = true;
		}
	}
}