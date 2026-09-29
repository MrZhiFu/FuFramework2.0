using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络数据接收状态
	/// </summary>
	internal sealed class ReceiveState : IDisposable
	{
		public const int  DEFAULT_BUFFER_LENGTH = 1024 * 64;
		public const int  PACKET_HEADER_LENGTH  = 14;
		private      bool m_disposed            = false;

		public MemoryStream Stream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		/// <summary>
		/// 是否为空消息体
		/// </summary>
		public bool IsEmptyBody { get; private set; }

		public IPacketReceiveHeaderHandler PacketHeader { get; set; }

		public void PrepareForPacketHeader(int packetHeaderLength = PACKET_HEADER_LENGTH)
		{
			Reset(packetHeaderLength, null);
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

		public void Reset(int targetLength, IPacketReceiveHeaderHandler packetHeader)
		{
			if (targetLength < 0) throw new InvalidOperationException("Target length is invalid.");

			Stream.Position = 0L;
			Stream.SetLength(targetLength);

			// 发现内容长度为空.说明是个空消息或者内容是默认值.
			IsEmptyBody  = targetLength == 0;
			PacketHeader = packetHeader;
		}
	}
}