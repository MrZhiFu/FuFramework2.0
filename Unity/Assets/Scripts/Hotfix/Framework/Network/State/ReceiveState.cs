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
		/// <summary>
		/// 默认缓冲区长度
		/// </summary>
		public const int DEFAULT_BUFFER_LENGTH = 1024 * 64;

		/// <summary>
		/// 默认包头长度
		/// </summary>
		public const int PACKET_HEADER_LENGTH = 14;

		/// <summary>
		/// 是否已释放
		/// </summary>
		private bool m_disposed = false;

		/// <summary>
		/// 接收数据流
		/// </summary>
		public MemoryStream Stream { get; private set; } = new(DEFAULT_BUFFER_LENGTH);

		/// <summary>
		/// 是否为空消息体
		/// </summary>
		public bool IsEmptyBody { get; private set; }

		/// <summary>
		/// 接收包头处理器
		/// </summary>
		public IPacketReceiveHeaderHandler PacketHeader { get; set; }

		/// <summary>
		/// 准备接收包头
		/// </summary>
		/// <param name="packetHeaderLength">包头长度</param>
		public void PrepareForPacketHeader(int packetHeaderLength = PACKET_HEADER_LENGTH)
		{
			Reset(packetHeaderLength, null);
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

		/// <summary>
		/// 重置接收状态
		/// </summary>
		/// <param name="targetLength">目标长度</param>
		/// <param name="packetHeader">接收包头处理器</param>
		public void Reset(int targetLength, IPacketReceiveHeaderHandler packetHeader)
		{
			if (targetLength < 0) throw new InvalidOperationException("目标长度无效。");

			Stream.Position = 0L;
			Stream.SetLength(targetLength);

			// 发现内容长度为空.说明是个空消息或者内容是默认值.
			IsEmptyBody  = targetLength == 0;
			PacketHeader = packetHeader;
		}
	}
}