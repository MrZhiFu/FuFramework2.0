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
		private const int  DEFAULT_BUFFER_LENGTH = 1024 * 64;
		private       bool m_disposed;

		public MemoryStream Stream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		public void Reset()
		{
			Stream.Position = 0L;
			Stream.SetLength(0L);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

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
